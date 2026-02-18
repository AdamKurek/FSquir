using Fillsquir.Controls;
using Fillsquir.Domain;
using Fillsquir.Services;
using Fillsquir.Visuals;
using Microsoft.Maui.ApplicationModel;
using SkiaSharp;

namespace Fillsquir;


public partial class GamePage : ContentPage, IQueryAttributable
{
    private const float WallSnapAngleDotThreshold = 0.998f;
    private const float WallSnapDistanceThreshold = 24f;
    private const float WallSnapAlongAxisThreshold = 18f;
    private const float WallSnapMaxTranslation = 40f;
    private const float WallSnapTranslationAgreement = 3f;

    private readonly struct WallSegment
    {
        internal WallSegment(SKPoint start, SKPoint end, SKPoint direction)
        {
            Start = start;
            End = end;
            Direction = direction;
        }

        internal SKPoint Start { get; }
        internal SKPoint End { get; }
        internal SKPoint Direction { get; }
    }

    private readonly struct WallSnapCandidate
    {
        internal WallSnapCandidate(SKPoint translation, float score)
        {
            Translation = translation;
            Score = score;
        }

        internal SKPoint Translation { get; }
        internal float Score { get; }
    }

    private sealed class WallSnapCluster
    {
        private SKPoint translationSum;
        private float scoreSum;

        internal int SupportCount { get; private set; }

        internal WallSnapCluster(WallSnapCandidate candidate)
        {
            Add(candidate);
        }

        internal void Add(WallSnapCandidate candidate)
        {
            translationSum = new SKPoint(
                translationSum.X + candidate.Translation.X,
                translationSum.Y + candidate.Translation.Y);
            scoreSum += candidate.Score;
            SupportCount++;
        }

        internal SKPoint Center => new(
            translationSum.X / SupportCount,
            translationSum.Y / SupportCount);

        internal float AverageScore => scoreSum / SupportCount;
    }

    float absolute0x = 0f;
    float absolute0y = 0f;
    float absolutemaxx = 1000f;
    float absolutemaxy = 1000f;
    enum moveStatus
    {
        none = 0,
        map,
        fragment,
        bottomStrip,
        undecided,
    }
    moveStatus movingStatus = moveStatus.none;

    private readonly IProgressStore progressStore;
    private readonly ILeaderboardClient leaderboardClient;
    private readonly IRecordSyncService recordSyncService;
    private readonly IScoreEvaluator scoreEvaluator;
    private readonly ICoordinateTransformer coordinateTransformer;
    private readonly VisualSettingsState visualSettingsState;
    private readonly IPuzzleMaterialService puzzleMaterialService;

    private readonly GameSessionState sessionState = new();
    private LevelProgress? levelProgress;
    private PuzzleKey puzzleKey;
    private string installId = string.Empty;
    private const decimal CoverageComparisonTolerance = 0.0001m;
    private VisualSettings currentVisualSettings = new();
    private bool subscribedToVisualSettings;

    GameSettings settings;
    public GamePage()
    {
        BindingContext = new GamePageViewModel();

        IServiceProvider? services = App.Services;
        progressStore = services?.GetService(typeof(IProgressStore)) as IProgressStore ?? new JsonFileProgressStore();
        leaderboardClient = services?.GetService(typeof(ILeaderboardClient)) as ILeaderboardClient
            ?? new HttpLeaderboardClient(new HttpClient { BaseAddress = new Uri("http://localhost:5180/"), Timeout = TimeSpan.FromSeconds(2) });
        recordSyncService = services?.GetService(typeof(IRecordSyncService)) as IRecordSyncService
            ?? new RecordSyncService(leaderboardClient, progressStore, new JsonFileSyncQueue());
        scoreEvaluator = services?.GetService(typeof(IScoreEvaluator)) as IScoreEvaluator ?? new ScoreEvaluator();
        coordinateTransformer = services?.GetService(typeof(ICoordinateTransformer)) as ICoordinateTransformer ?? new CoordinateTransformer();
        visualSettingsState = services?.GetService(typeof(VisualSettingsState)) as VisualSettingsState
            ?? new VisualSettingsState(new VisualSettingsStore());
        puzzleMaterialService = services?.GetService(typeof(IPuzzleMaterialService)) as IPuzzleMaterialService
            ?? new PuzzleMaterialService(new WorldTextureProvider());

        Shell.SetNavBarIsVisible(this, false);
        InitializeComponent();
        snapToggle.IsToggled = true;
        UpdateStatusLabel();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (!subscribedToVisualSettings)
        {
            visualSettingsState.Changed += VisualSettingsState_Changed;
            subscribedToVisualSettings = true;
        }

        _ = recordSyncService.TriggerSyncAsync();
        _ = LoadAndApplyVisualSettingsAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (!subscribedToVisualSettings)
        {
            return;
        }

        visualSettingsState.Changed -= VisualSettingsState_Changed;
        subscribedToVisualSettings = false;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        int level = int.Parse(query["Level"].ToString());
        const int seed = 0;

        settings = new(seed, level);
        puzzleKey = new PuzzleKey(level, seed, GameRules.RulesVersion);
        ApplyVisualSettingsToSettings(settings, currentVisualSettings, invalidateTextureCache: false);
        InitializeSquir(settings);
        _ = LoadProgressAndRecordsAsync();
        _ = LoadAndApplyVisualSettingsAsync();
    }

    Squir drawa;
    DrawableStack drawables;
    double SquirArea;
    SKPoint startingPoint = new();
    SKPoint TapPosition = new();
    Microsoft.Maui.Graphics.Point mousePosition = new();
    Fragment moved;
    GameSettings gameSettings;
    CommonArea commonArea;

    SKPoint dlocation;
    SKPoint d;

    int isZooming = 0;
    float zoomSum = 0f;
    bool was2FingerTouched = false;
    SKPoint fingersMove;
    bool fingersLocked = false;

    SKPoint fingersMoveOnZooming { get { return new(); } }
    SKPoint currMoveWhenZooming;
    SKPoint currOffsetOnZooming;
    SKPoint removeFromMoveWhenZooming;
#if DebugClickingLines
#endif
    private void InitializeSquir(GameSettings settings)
    {
        gameSettings = settings;
        gameSettings.SnapEnabled = sessionState.SnapEnabled;
        drawa = new Squir(1000, 1000, gameSettings);
        commonArea = new(gameSettings,drawa);
        gameSettings.MaxArea = FSMath.CalculateArea(drawa.PointsP);

        var fragmentpoints = drawa.SplitSquir();
        drawables = new DrawableStack(gameSettings);
        drawables.AddDrawable(drawa);

        for (int r = 0, i = 0; i < gameSettings.fragments; r++) 
        {
            for(int c = 0; c < gameSettings.Cols; c++)
            {
                try { 
                var fragment = new Fragment(fragmentpoints[i++], c,r, gameSettings);
                gameSettings.untouchedFragments[c,r] = fragment;
                drawables.AddDrawable(fragment);
                }catch (Exception e)
                {
                    continue;
                }
            }
        }
        //for (int i = 0; i < fragmentpoints.Count; i++)
        {
         //   var fragment = new Fragment(fragmentpoints[i], i, gameSettings);
        //    drawables.AddDrawable(fragment);
        }
        drawables.AddCover(commonArea); 
        drawables.Gui = new PercentageDisplay(gameSettings);


        squir.PaintSurface += (s, e) =>
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear();
            canvas.Scale(gameSettings.zoomFactor);
            drawables.DrawPreZoom(canvas);
            canvas.Scale(1/gameSettings.zoomFactor);
            drawables.DrawPastZoom(canvas);
            canvas.Scale(gameSettings.zoomFactor);
            drawables.DrawFragmentsoutlines(canvas);
        };

        var panGesture = new PanGestureRecognizer();
        var pointGesture = new PointerGestureRecognizer();
        var zoom = new PinchGestureRecognizer();
        /*
        var slide2FingersGesture = new PanGestureRecognizer();
        slide2FingersGesture.TouchPoints = 2;
        slide2FingersGesture.PanUpdated += (s, e) =>
        {
            was2FingerTouched = true;
            //gameSettings.xoffset = (float)e.TotalX;
            //gameSettings.yoffset = (float)e.TotalY;
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    {
                        if (!fingersLocked)
                        {
                            prevXOffset = gameSettings.xoffset;
                            prevYOffset = gameSettings.yoffset;
                        }
                    break;
                    }
                case GestureStatus.Running: {
                        drawables.AddDot(new(d.X + (float)e.TotalX, d.Y + (float)e.TotalY),true);
                        //if (!fingersLocked)
                        {
                            fingersMove.X = (float)e.TotalX;
                            fingersMove.Y = (float)e.TotalY;
                        }
                        SetCameraToZoomAndMove(new SKPoint((float)e.TotalX - removeFromMoveWhenZooming.X, (float)e.TotalY - removeFromMoveWhenZooming.Y));
                        break; }
                case GestureStatus.Completed: {
                        if (--isZooming <= 0) { 
                            //StartMovingMap();
                        }
                        fingersLocked = false;
                        break;
                    }
            }
            Invalidate();
        };
        */
        zoom.PinchUpdated += (s, e) =>
        {
            var xd = drawables.Gui as PercentageDisplay;
            was2FingerTouched = true;
            switch (e.Status)
            {
                case GestureStatus.Started:
                    isZooming++;
                    if (!fingersLocked)
                    {
                        prevXOffset = gameSettings.xoffset;
                        prevYOffset = gameSettings.yoffset;
                        removeFromMoveWhenZooming.X = fingersMove.X;
                        removeFromMoveWhenZooming.Y = fingersMove.Y;

                    fingersLocked = true;
                    }

                    dlocation = new SKPoint((float)(e.ScaleOrigin.X * squir.Width), (float)(e.ScaleOrigin.Y * squir.Height));
                    dlocation.X /= gameSettings.zoomFactor;
                    dlocation.Y /= gameSettings.zoomFactor;
                    d = new SKPoint((float)(e.ScaleOrigin.X * squir.Width), (float)(e.ScaleOrigin.Y * squir.Height));
                    //dlocation.X -= gameSettings.xoffset;
                    //dlocation.Y -= gameSettings.yoffset;
                    zoomPrev = gameSettings.zoomFactor;

                    break;

                case GestureStatus.Running:

                    zoomSum += 1f - (float)e.Scale;
                    gameSettings.zoomFactor *= (float)e.Scale;

                    // SKPoint MoveInZoom = new(-d.X+(((float)(e.ScaleOrigin.X * squir.Width)) ), -d.Y+( ((float)(e.ScaleOrigin.Y * squir.Height)) ));
                    currMoveWhenZooming = new SKPoint((float)(e.ScaleOrigin.X * squir.Width), (float)(e.ScaleOrigin.Y * squir.Height));
                    currMoveWhenZooming.X -= d.X;
                    currMoveWhenZooming.Y -= d.Y;
                    currMoveWhenZooming.X /= gameSettings.zoomFactor;
                    currMoveWhenZooming.Y /= gameSettings.zoomFactor;
                    SetCameraToZoomAndMove(currMoveWhenZooming);

                    //drawables.AddDot(currmove,false);

                    //gameSettings.xoffset = -location.X + gameSettings.xoffset + (e.Location.X / gameSettings.zoomFactor);

                    // floats.Add((-d.X , gameSettings.xoffset , dlocation.X , gameSettings.zoomFactor));
                    //gameSettings.yoffset = -d.Y + gameSettings.yoffset + (dlocation.Y / gameSettings.zoomFactor);

                    ((PercentageDisplay)(drawables.Gui)).debugString = e.Scale.ToString();

                    //drawables.AddDot(dlocation);
                    break;

                case GestureStatus.Completed:
                    if (--isZooming <= 0)
                    {
                        //   StartMovingMap();
                        removeFromMoveWhenZooming = new();
                        zoomSum = 0f;
                        fingersLocked = false;

                        fingersMove = new();
                    }
                    break;

            }
            Invalidate();
        };
        grid.GestureRecognizers.Add(panGesture);
        grid.GestureRecognizers.Add(zoom);
        grid.GestureRecognizers.Add(pointGesture);
      //  grid.GestureRecognizers.Add(slide2FingersGesture);
        panGesture.PanUpdated += PanGesture_PanUpdated;
        squir.EnableTouchEvents = true;

        pointGesture.PointerEntered += (_, e) =>
        {
            Microsoft.Maui.Graphics.Point? pointerPosition = e.GetPosition(squir);
            if (pointerPosition.HasValue)
            {
                UpdateHoveredFragmentFromPointer(new SKPoint((float)pointerPosition.Value.X, (float)pointerPosition.Value.Y));
            }
        };

        pointGesture.PointerMoved += (_, e) =>
        {
            Microsoft.Maui.Graphics.Point? pointerPosition = e.GetPosition(squir);
            if (pointerPosition.HasValue)
            {
                UpdateHoveredFragmentFromPointer(new SKPoint((float)pointerPosition.Value.X, (float)pointerPosition.Value.Y));
            }
            else
            {
                SetHoveredFragment(null);
            }
        };

        pointGesture.PointerExited += (_, _) =>
        {
            SetHoveredFragment(null);
        };

#if DebugClickingLines
                switch (e.StatusType)
                {
                    case GestureStatus.Started:
                        drawables.testLine = new();
                        drawables.testLine.p.X = mousePosition.X;
                        drawables.testLine.p.Y = mousePosition.Y;
                        break;
                    case GestureStatus.Running:

                        drawables.testLine.q.X = mousePosition.X;
                        drawables.testLine.q.Y = mousePosition.Y;
                        bool crossing = FSMath.DoSegmentsIntersect(drawables.testLine.q, drawables.testLine.p, ((Squir)drawables[0]).VisiblePoints[0], ((Squir)drawables[0]).VisiblePoints[1]);


                        drawables.isCrossing = crossing;
                        break;

                    case GestureStatus.Completed:
                        drawables.testLine = null;
                        break;
                }
                Invalidate();

                return;
#endif
        }

    private void SetCameraToZoomAndMove(SKPoint mov)
    {
        gameSettings.xoffset = -dlocation.X + prevXOffset + (d.X / gameSettings.zoomFactor) + fingersMoveOnZooming.X + mov.X;
        gameSettings.yoffset = -dlocation.Y + prevYOffset + (d.Y / gameSettings.zoomFactor) + fingersMoveOnZooming.Y + mov.Y;
    }

    void UpdateCover()
        {
            List<Fragment> FiguresAsPointlists = new List<Fragment>();
            foreach (Fragment a in drawables.drawables.Skip(1))
            {
                if (a.wasTouched) { 
                    FiguresAsPointlists.Add((a));
                }
            }
            //var u1 = ((Fragment)drawables.drawables[1]).VisiblePointsP;
            //var u2 = ((Squir)drawables[0]).PointsP;
           
            commonArea.FragmentsInside = FiguresAsPointlists;

        //commonArea.FiguresP = FSMath.CommonArea(
            ///   ((Squir)drawables[0]).PointsP,
             //  FiguresAsPointlists);
        UpdateGui(((CommonArea)drawables.cover).Area);
        }

        void UpdateGui(double area)
        {
            gameSettings.AreaFilled = area;
            decimal coveragePercent = scoreEvaluator.ComputeCoveragePercent(gameSettings.AreaFilled, gameSettings.MaxArea);
            sessionState.CoveragePercent = coveragePercent;
            gameSettings.CurrentStars = scoreEvaluator.ComputeStars(
                coveragePercent,
                gameSettings.WorldRecordCoveragePercent,
                gameSettings.BestCoveragePercent > 0m ? gameSettings.BestCoveragePercent : null);
            UpdateStatusLabel();
        }


        void Invalidate()
        {
            squir.InvalidateSurface();
        }

        private async Task LoadAndApplyVisualSettingsAsync()
        {
            try
            {
                VisualSettings loaded = await visualSettingsState.LoadAsync();
                currentVisualSettings = loaded.Normalize();
            }
            catch
            {
                currentVisualSettings = new VisualSettings();
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (gameSettings is not null)
                {
                    ApplyVisualSettingsToSettings(gameSettings, currentVisualSettings, invalidateTextureCache: true);
                    Invalidate();
                    return;
                }

                if (settings is not null)
                {
                    ApplyVisualSettingsToSettings(settings, currentVisualSettings, invalidateTextureCache: false);
                }
            });
        }

        private void VisualSettingsState_Changed(object? sender, VisualSettings updated)
        {
            currentVisualSettings = updated.Normalize();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (gameSettings is null)
                {
                    return;
                }

                ApplyVisualSettingsToSettings(gameSettings, currentVisualSettings, invalidateTextureCache: true);
                Invalidate();
            });
        }

        private void ApplyVisualSettingsToSettings(GameSettings targetSettings, VisualSettings visualSettings, bool invalidateTextureCache)
        {
            VisualSettings normalized = visualSettings.Normalize();
            string previousSkinId = targetSettings.SkinId;
            GraphicsQualityTier previousQuality = targetSettings.QualityTier;

            targetSettings.SkinId = normalized.SelectedSkinId;
            targetSettings.QualityTier = normalized.QualityTier;
            targetSettings.MappingMode = normalized.MappingMode;
            targetSettings.ShowStrongOutlines = normalized.ShowStrongOutlines;
            targetSettings.DepthIntensity = normalized.DepthIntensity;
            targetSettings.StripOpacity = normalized.StripOpacity;
            targetSettings.StripFrostAmount = normalized.StripFrostAmount;

            bool cacheKeyChanged =
                !string.Equals(previousSkinId, targetSettings.SkinId, StringComparison.OrdinalIgnoreCase)
                || previousQuality != targetSettings.QualityTier;

            if (!invalidateTextureCache || !cacheKeyChanged)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(previousSkinId))
            {
                puzzleMaterialService.InvalidateCacheForSkinOrSeed(puzzleKey, previousSkinId);
            }

            puzzleMaterialService.InvalidateCacheForSkinOrSeed(puzzleKey, targetSettings.SkinId);
        }

        private void squir_SizeChanged(object sender, EventArgs e)
        {
            if (drawa != null && drawables != null)
            {
                //drawa.Resize(squir.Width, squir.Height);
                drawables.Resize((float)squir.Width, (float)squir.Height);
                drawables.cover.Resize((float)squir.Width, (float)squir.Height);
                drawables.Gui.Resize((float)squir.Width, (float)squir.Height);
            }
        //(sender as SKCanvasView).ScaleX.ToString();
    }

    float wtfstrip;
    private float bottomStripMovePre
    {
        get
        {
            return wtfstrip;
        }
        set
        {
            wtfstrip = value;
        }
    }

    SKPoint currentMove;
    private void PanGesture_PanUpdated(object sender, PanUpdatedEventArgs e)
    {
        if (e.TotalX == 0) { return; }

        //location.Offset(+gameSettings.xoffset);
        if (was2FingerTouched){ return; }
       
        SKPoint location =new((float)e.TotalX, (float)e.TotalY);
        location.X /= gameSettings.zoomFactor;
        location.Y /= gameSettings.zoomFactor;



        switch (movingStatus)
        {
            case moveStatus.undecided:
                {
                    if (Math.Abs(e.TotalX) > Math.Abs(e.TotalY) + 5)
                    {
                        movingStatus = moveStatus.bottomStrip;
                        gameSettings.ActiveDraggedFragment = null;
                        goto case moveStatus.bottomStrip;
                    }
                    if (Math.Abs(e.TotalY) > Math.Abs(e.TotalX) + 5)
                    {
                        movingStatus = moveStatus.fragment;
                        if (moved.wasTouched)
                        {
                            startingPoint = moved.PositionS;
                        }
                        TouchFragment(moved);
                        goto case moveStatus.fragment;
                    }
                    break;
                }
            case moveStatus.map:
                {
                    gameSettings.ActiveDraggedFragment = null;
                    
                    gameSettings.xoffset = location.X + offsetMoveLocation.X;
                    gameSettings.yoffset = location.Y + offsetMoveLocation.Y;


                    var xMoveTotal = location.X - currentMove.X;
                    var yMoveTotal = location.Y - currentMove.Y;
                  
                    currentMove.X = location.X;
                    currentMove.Y = location.Y;
                    
                    if (xMoveTotal < 0)
                    {
                        GameSettings.MoveFragmentsBetweenLists(gameSettings.CenterFragments, gameSettings.TooLeftFragments,
                            drawable => (((drawable.PositionP.X  + drawable.sizeP.X )* (squir.Width /1000)) + gameSettings.xoffset < absolute0x));
                        GameSettings.MoveFragmentsBetweenLists(gameSettings.TooRightFragments, gameSettings.CenterFragments,
                            drawable => (((drawable.PositionP.X * (squir.Width / 1000))) + (gameSettings.xoffset ) ) < (squir.Width/gameSettings.zoomFactor));
                    }
                    if (xMoveTotal > 0)
                    {
                        GameSettings.MoveFragmentsBetweenLists(gameSettings.TooLeftFragments, gameSettings.CenterFragments,
                            drawable => (((drawable.PositionP.X + drawable.sizeP.X) * (squir.Width / 1000)) + gameSettings.xoffset > absolute0x));
                        GameSettings.MoveFragmentsBetweenLists(gameSettings.CenterFragments, gameSettings.TooRightFragments,
                            drawable => (((drawable.PositionP.X * (squir.Width / 1000))) + (gameSettings.xoffset ) ) > (squir.Width / gameSettings.zoomFactor));
                    }
                    if (yMoveTotal < 0)
                    {
                        GameSettings.MoveFragmentsBetweenLists(gameSettings.CenterFragments, gameSettings.TooTopFragments,
                            drawable => (((drawable.PositionP.Y + drawable.sizeP.Y) * (squir.Height / 1000)) + gameSettings.yoffset < absolute0y));
                        GameSettings.MoveFragmentsBetweenLists(gameSettings.TooBottomFragments, gameSettings.CenterFragments,
                            drawable => (((drawable.PositionP.Y * (squir.Height / 1000))) + (gameSettings.yoffset)) < (squir.Height / gameSettings.zoomFactor));
                    }
                    if (yMoveTotal > 0)
                    {
                        GameSettings.MoveFragmentsBetweenLists(gameSettings.TooTopFragments, gameSettings.CenterFragments,
                            drawable => (((drawable.PositionP.Y + drawable.sizeP.Y) * (squir.Height / 1000)) + gameSettings.yoffset > absolute0y));
                        GameSettings.MoveFragmentsBetweenLists(gameSettings.CenterFragments, gameSettings.TooBottomFragments,
                            drawable => (((drawable.PositionP.Y * (squir.Height / 1000))) + (gameSettings.yoffset)) > (squir.Height / gameSettings.zoomFactor));
                    }

                    ((PercentageDisplay)drawables.Gui).debugString = gameSettings.CenterFragments.Count.ToString();
                    //List<int> indexesToMove = new List<int>();

                    //for (int i = 0; i < gameSettings.CenterFragments.Count; i++)
                    //{
                    //    var drawable = gameSettings.CenterFragments[i];
                    //    if (((drawable.PositionP.X + drawable.sizeP.X) * gameSettings.zoomFactor) + gameSettings.xoffset < absolute0x)
                    //    {
                    //        indexesToMove.Add(i);
                    //    }
                    //}
                    //foreach (var index in indexesToMove)
                    //{
                    //    var item = gameSettings.CenterFragments[index];
                    //    gameSettings.TooLeftFragments.Add(item);
                    //    gameSettings.CenterFragments.RemoveAt(index);
                    //}
                    if (xMoveTotal > 0)
                    {
                        //List<Fragment> itemsToMove = new List<Fragment>();
                        //foreach (var drawable in gameSettings.TooLeftFragments)
                        //{
                        //    if (((drawable.PositionP.X + drawable.sizeP.X) * gameSettings.zoomFactor) + gameSettings.xoffset > absolute0x)
                        //    {
                        //        itemsToMove.Add(drawable);
                        //    }
                        //}

                        //foreach (var item in itemsToMove)
                        //{
                        //    gameSettings.TooLeftFragments.Remove(item);
                        //    gameSettings.CenterFragments.Add(item);
                        //}
                    }


                   
                    break;
                }
            case moveStatus.fragment:
                {
                    if(moved == null)
                    {
                        movingStatus = moveStatus.none;
                        gameSettings.ActiveDraggedFragment = null;
                        return;
                    }
                    moved.PositionS.X = startingPoint.X + location.X;
                    moved.PositionS.Y = startingPoint.Y + location.Y;
                    UpdateCover();
                    break;
                }
            case moveStatus.bottomStrip: 
                {
                    gameSettings.ActiveDraggedFragment = null;
                    var pos = bottomStripMovePre - (float)e.TotalX;
                    if (pos <= 0)
                    {
                        pos = 0;
                    }
                    else
                    {
                        var TotalStripLenth = ((float)gameSettings.Cols / (float)gameSettings.VisibleRows) * (float)squir.Width - (float)squir.Width;
                        if (TotalStripLenth <= pos)
                        {
                            pos = TotalStripLenth;
                        }
                    }
                    gameSettings.bottomStripMove = pos;
                    break;
                }
        }
        Invalidate();
    }

    SKPoint offsetMoveLocation;

    float zoomPrev = 1;
    SKPoint zoomPos;
    float prevXOffset;
    float prevYOffset;

    Fragment theonlypuzzleRemoveitlater;

    private void squir_Touch(object sender, SkiaSharp.Views.Maui.SKTouchEventArgs e)
        {
        was2FingerTouched = false;
        TapPosition = e.Location;
        var location = e.Location;
        if (location.Y > squir.Height * gameSettings.prop1 / gameSettings.prop2&& e.ActionType == SkiaSharp.Views.Maui.SKTouchAction.Pressed)
        {
            if(e.MouseButton == SkiaSharp.Views.Maui.SKMouseButton.Left)
            {
                SetHoveredFragment(null);
                if (TryGetStripCell(e.Location, out int selectedCol, out int selectedRow))
                {
                    moved = gameSettings.untouchedFragments[selectedCol, selectedRow];
                    movingStatus = moveStatus.fragment;
                    gameSettings.untouchedFragments[selectedCol, selectedRow] = null;
                }
                else
                {
                    moved = null;
                    movingStatus = moveStatus.bottomStrip;
                    gameSettings.ActiveDraggedFragment = null;
                    bottomStripMovePre = gameSettings.bottomStripMove;
                    return;
                }
#if DebugString
                //((PercentageDisplay)(drawables.Gui)).debugString = selectedCell.ToString();
#endif
                if (moved == null)
                {
                    location.X /= gameSettings.zoomFactor;
                    location.Y /= gameSettings.zoomFactor;
                    
                    drawables.AddDot(location); Invalidate();
                    moved = drawables.SelectFragmentOnClick(location);
                    if (moved == null)
                    {
                        movingStatus = moveStatus.bottomStrip;
                        gameSettings.ActiveDraggedFragment = null;
                        bottomStripMovePre = gameSettings.bottomStripMove;
                        return;
                    }
                    gameSettings.ActiveDraggedFragment = moved.wasTouched ? moved : null;
                    location.X -= gameSettings.xoffset;
                    location.Y -= gameSettings.yoffset;
                    startingPoint = location;
                    movingStatus = moveStatus.undecided;
                    bottomStripMovePre = gameSettings.bottomStripMove;
                    return;
                }
                else
                {
#if WINDOWS
                    TouchFragment(moved);

                    movingStatus = moveStatus.fragment;
#else
                    movingStatus = moveStatus.undecided;
#endif
                    gameSettings.ActiveDraggedFragment = moved.wasTouched ? moved : null;
                    bottomStripMovePre = gameSettings.bottomStripMove;
                    location.X /= gameSettings.zoomFactor;
                    location.Y /= gameSettings.zoomFactor;
                    location.X -= gameSettings.xoffset;
                    location.Y -= gameSettings.yoffset;
                    startingPoint = location;
                    return;
                }
            }
            else if (e.MouseButton == SkiaSharp.Views.Maui.SKMouseButton.Middle)
            {
                SetHoveredFragment(null);
                movingStatus = moveStatus.bottomStrip;
                gameSettings.ActiveDraggedFragment = null;
                bottomStripMovePre = gameSettings.bottomStripMove;
                Invalidate();
                return;
            }
            else if(e.ActionType == SkiaSharp.Views.Maui.SKTouchAction.Pressed) {; }
        }
       // location.X -= gameSettings.xoffset;
       // location.Y -= gameSettings.yoffset;
        location.X /= gameSettings.zoomFactor;
        location.Y /= gameSettings.zoomFactor;
        var diff = e.Location;

#if DebugClicking
        bool inisde = false;
        SKPoint mp = new SKPoint() { X = location.X, Y = location.Y };
        if (FSMath.IsPointInShape(mp, ((Squir)drawables[0]).VisiblePoints))
        {
            inisde = true;
        }
        //drawables.AddDot(mp, inisde);
        Invalidate();
#endif
        zoomPos = location;
        switch (e.ActionType)
        {
            case SkiaSharp.Views.Maui.SKTouchAction.Pressed:
                {
                    SetHoveredFragment(null);
                    if (e.MouseButton == SkiaSharp.Views.Maui.SKMouseButton.Middle)
                    {
                        StartMovingMap();
                        break;
                    }

                    moved = drawables.SelectFragmentOnClick(location);
#if WINDOWS
                    if (moved == null)
                    {
                        moved = drawables.getNearestFragment(location);
                    }
#endif
                    if (moved == null) {
#if WINDOWS
                        bottomStripMovePre = gameSettings.bottomStripMove;
                        gameSettings.ActiveDraggedFragment = null;

#else
                        StartMovingMap();
                     
#endif
                        return;
                    }//probably will be needed one day
                    if(moved.wasTouched) {
                        startingPoint = moved.PositionS;
                        gameSettings.ActiveDraggedFragment = moved;
                    }
                    else
                    {
                        startingPoint = location;
                        gameSettings.ActiveDraggedFragment = null;
                                        //here add offset
                    }
                    //TouchFragment(moved);
                    movingStatus = moveStatus.fragment;
                    break;
                }
            case SkiaSharp.Views.Maui.SKTouchAction.Released:
                {
                    if (moved == null)
                    {
                        gameSettings.ActiveDraggedFragment = null;
                        SetHoveredFragment(null);
                        return;
                    }

                    if (!moved.wasTouched)
                    {
                        gameSettings.ActiveDraggedFragment = null;
                        SetHoveredFragment(null);
                        return;
                    }

                    if (gameSettings.SnapEnabled && TryGetWallSnapTranslation(moved, out var snapTranslation))
                    {
                        moved.PositionS = new SKPoint(
                            moved.PositionS.X + snapTranslation.X,
                            moved.PositionS.Y + snapTranslation.Y);
                    }
                    moved.TriggerReleaseSettle();
                    moved = null;
                    movingStatus = moveStatus.none;
                    gameSettings.ActiveDraggedFragment = null;
                    SetHoveredFragment(null);
                    UpdateCover();
                    _ = SaveBestIfImprovedAsync();
                    //UpdateGui();
                    Invalidate();
                    break;
                }
            case SkiaSharp.Views.Maui.SKTouchAction.WheelChanged:
                {

                    if (e.WheelDelta > 0)
                    {
                        gameSettings.zoomFactor += 0.5f;
                    }
                    else
                    {
                        if (gameSettings.zoomFactor <= 0.5f)
                        {
                            return;
                        }
                        gameSettings.zoomFactor -= 0.5f;
                    }
                   // var zoomprop = gameSettings.zoomFactor / zoomPrev;
                   // var xfromhere = -gameSettings.xoffset + (e.Location.X / zoomPrev);
                   // var yfromhere = -gameSettings.yoffset + (e.Location.Y / zoomPrev);
                    gameSettings.xoffset = -location.X + gameSettings.xoffset + (e.Location.X / gameSettings.zoomFactor);
                    gameSettings.yoffset = -location.Y + gameSettings.yoffset + (e.Location.Y / gameSettings.zoomFactor);
                    

                    


                    //var xd = location.X;
                    //var difference = xd - xfromhere;

                    Invalidate();

                    //var 


                    //but it can't be constant 1 pixel it has to be something else, maybe librarys have different approach to this



                    //but notice that by default it zooms to the left top corner
                    //so to adjust xoffset you need to add difference that was made by that




                    break;
                }
            case SkiaSharp.Views.Maui.SKTouchAction.Moved:
                {
#if WINDOWS
                    if (e.MouseButton == SkiaSharp.Views.Maui.SKMouseButton.Middle)
#endif
                    {
                        if (movingStatus == moveStatus.map) { 
                          
                            Invalidate();
                            break;
                        }
                    }
                    if (movingStatus == moveStatus.bottomStrip)
                    {
                        ;
                    }
                    break;
                }
            }
        
        Invalidate();
    }

    private bool TryGetWallSnapTranslation(Fragment movedFragment, out SKPoint snapTranslation)
    {
        snapTranslation = default;

        var movedWalls = BuildWallSegments(movedFragment.VisiblePointsS);
        if (movedWalls.Count == 0)
        {
            return false;
        }

        var targetWalls = GetAllSnapTargetWalls(movedFragment);
        if (targetWalls.Count == 0)
        {
            return false;
        }

        List<WallSnapCandidate> candidates = new();
        foreach (var movedWall in movedWalls)
        {
            foreach (var targetWall in targetWalls)
            {
                if (!AreWallsParallel(movedWall.Direction, targetWall.Direction))
                {
                    continue;
                }

                if (!TryGetWallTranslation(
                    movedWall,
                    targetWall,
                    out SKPoint translation,
                    out float perpendicularDistance,
                    out float alongAxisGap))
                {
                    continue;
                }

                float translationLength = VectorLength(translation);
                if (translationLength > WallSnapMaxTranslation)
                {
                    continue;
                }

                if (perpendicularDistance > WallSnapDistanceThreshold)
                {
                    continue;
                }

                if (alongAxisGap > WallSnapAlongAxisThreshold)
                {
                    continue;
                }

                float score = perpendicularDistance + (0.35f * alongAxisGap);
                candidates.Add(new WallSnapCandidate(translation, score));
            }
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        return TrySelectBestWallSnap(candidates, out snapTranslation);
    }

    private List<WallSegment> GetAllSnapTargetWalls(Fragment movedFragment)
    {
        List<WallSegment> walls = new();
        walls.AddRange(BuildWallSegments(drawa.VisiblePoints));

        foreach (var fragment in gameSettings.CenterFragments)
        {
            if (!fragment.wasTouched || Object.ReferenceEquals(fragment, movedFragment))
            {
                continue;
            }

            walls.AddRange(BuildWallSegments(fragment.VisiblePointsS));
        }

        return walls;
    }

    private static List<WallSegment> BuildWallSegments(SKPoint[] polygon)
    {
        List<WallSegment> walls = new();
        if (polygon is null || polygon.Length < 3)
        {
            return walls;
        }

        for (int i = 0; i < polygon.Length; i++)
        {
            SKPoint start = polygon[i];
            SKPoint end = polygon[(i + 1) % polygon.Length];

            SKPoint direction = Normalize(Subtract(end, start));
            if (direction.X == 0f && direction.Y == 0f)
            {
                continue;
            }

            walls.Add(new WallSegment(start, end, direction));
        }

        return walls;
    }

    private static bool TryGetWallTranslation(
        WallSegment movedWall,
        WallSegment targetWall,
        out SKPoint translation,
        out float perpendicularDistance,
        out float alongAxisGap)
    {
        translation = default;
        perpendicularDistance = float.PositiveInfinity;
        alongAxisGap = float.PositiveInfinity;

        SKPoint axis = targetWall.Direction;
        SKPoint normal = Perpendicular(axis);
        if (normal.X == 0f && normal.Y == 0f)
        {
            return false;
        }

        SKPoint delta = Subtract(targetWall.Start, movedWall.Start);
        float signedPerpendicularDistance = Dot(delta, normal);
        perpendicularDistance = MathF.Abs(signedPerpendicularDistance);

        translation = Multiply(normal, signedPerpendicularDistance);
        alongAxisGap = ParallelAxisGap(movedWall, targetWall, axis);

        return float.IsFinite(translation.X)
            && float.IsFinite(translation.Y)
            && float.IsFinite(perpendicularDistance)
            && float.IsFinite(alongAxisGap);
    }

    private static bool TrySelectBestWallSnap(List<WallSnapCandidate> candidates, out SKPoint translation)
    {
        translation = default;
        if (candidates.Count == 0)
        {
            return false;
        }

        List<WallSnapCluster> clusters = new();
        foreach (var candidate in candidates)
        {
            bool addedToExistingCluster = false;
            for (int i = 0; i < clusters.Count; i++)
            {
                if (PointDistance(candidate.Translation, clusters[i].Center) <= WallSnapTranslationAgreement)
                {
                    clusters[i].Add(candidate);
                    addedToExistingCluster = true;
                    break;
                }
            }

            if (!addedToExistingCluster)
            {
                clusters.Add(new WallSnapCluster(candidate));
            }
        }

        WallSnapCluster bestCluster = clusters[0];
        for (int i = 1; i < clusters.Count; i++)
        {
            WallSnapCluster candidateCluster = clusters[i];
            if (candidateCluster.SupportCount > bestCluster.SupportCount)
            {
                bestCluster = candidateCluster;
                continue;
            }

            if (candidateCluster.SupportCount == bestCluster.SupportCount
                && candidateCluster.AverageScore < bestCluster.AverageScore)
            {
                bestCluster = candidateCluster;
                continue;
            }

            if (candidateCluster.SupportCount == bestCluster.SupportCount
                && Math.Abs(candidateCluster.AverageScore - bestCluster.AverageScore) < 0.001f
                && VectorLength(candidateCluster.Center) < VectorLength(bestCluster.Center))
            {
                bestCluster = candidateCluster;
            }
        }

        translation = bestCluster.Center;
        return float.IsFinite(translation.X) && float.IsFinite(translation.Y);
    }

    private static bool AreWallsParallel(SKPoint firstDirection, SKPoint secondDirection)
    {
        float dot = (firstDirection.X * secondDirection.X) + (firstDirection.Y * secondDirection.Y);
        return MathF.Abs(dot) >= WallSnapAngleDotThreshold;
    }

    private static SKPoint Subtract(SKPoint left, SKPoint right)
    {
        return new SKPoint(left.X - right.X, left.Y - right.Y);
    }

    private static SKPoint Multiply(SKPoint point, float scalar)
    {
        return new SKPoint(point.X * scalar, point.Y * scalar);
    }

    private static float Dot(SKPoint left, SKPoint right)
    {
        return (left.X * right.X) + (left.Y * right.Y);
    }

    private static SKPoint Perpendicular(SKPoint vector)
    {
        return new SKPoint(-vector.Y, vector.X);
    }

    private static float ParallelAxisGap(WallSegment movedWall, WallSegment targetWall, SKPoint axis)
    {
        float movedA = Dot(movedWall.Start, axis);
        float movedB = Dot(movedWall.End, axis);
        float targetA = Dot(targetWall.Start, axis);
        float targetB = Dot(targetWall.End, axis);

        float movedMin = MathF.Min(movedA, movedB);
        float movedMax = MathF.Max(movedA, movedB);
        float targetMin = MathF.Min(targetA, targetB);
        float targetMax = MathF.Max(targetA, targetB);

        if (movedMax < targetMin)
        {
            return targetMin - movedMax;
        }

        if (targetMax < movedMin)
        {
            return movedMin - targetMax;
        }

        return 0f;
    }

    private static SKPoint Normalize(SKPoint vector)
    {
        float length = VectorLength(vector);
        if (length <= 1e-6f || !float.IsFinite(length))
        {
            return new SKPoint(0f, 0f);
        }

        return new SKPoint(vector.X / length, vector.Y / length);
    }

    private static float PointDistance(SKPoint a, SKPoint b)
    {
        return VectorLength(Subtract(a, b));
    }

    private static float VectorLength(SKPoint vector)
    {
        return MathF.Sqrt((vector.X * vector.X) + (vector.Y * vector.Y));
    }

    private void StartMovingMap()
    {
        offsetMoveLocation.X =  gameSettings.xoffset;
        offsetMoveLocation.Y =  gameSettings.yoffset;
        gameSettings.ActiveDraggedFragment = null;
        gameSettings.HoveredFragment = null;
        movingStatus = moveStatus.map;
        currentMove = new();
    }

    private void zoomTo(float zoomVal, SKPoint OnMapLocation, SKPoint OnScreenLocation, float previousZoom)
    {
            gameSettings.zoomFactor *= zoomVal;
        //if (gameSettings.zoomFactor > 1.5) { gameSettings.zoomFactor = 1.5f; }
        //if (gameSettings.zoomFactor < 0.5) { gameSettings.zoomFactor = 0.5f; }

        var zoomprop = gameSettings.zoomFactor / zoomPrev;
        var xfromhere = -gameSettings.xoffset + (OnScreenLocation.X / zoomPrev);
        var yfromhere = -gameSettings.yoffset + (OnScreenLocation.Y / zoomPrev);
        gameSettings.xoffset = -OnMapLocation.X + gameSettings.xoffset + (OnScreenLocation.X / gameSettings.zoomFactor);
            //gameSettings.yoffset = -OnMapLocation.Y + gameSettings.yoffset + (OnScreenLocation.Y / gameSettings.zoomFactor);
    }

    private void UpdateHoveredFragmentFromPointer(SKPoint screenLocation)
    {
        if (gameSettings is null || drawables is null)
        {
            return;
        }

        if (movingStatus == moveStatus.fragment || movingStatus == moveStatus.map || gameSettings.ActiveDraggedFragment is not null)
        {
            SetHoveredFragment(null);
            return;
        }

        Fragment? hovered = ResolveFragmentForScreenLocation(screenLocation);
        SetHoveredFragment(hovered);
    }

    private Fragment? ResolveFragmentForScreenLocation(SKPoint screenLocation)
    {
        float stripTop = (float)squir.Height * gameSettings.prop1 / gameSettings.prop2;
        if (screenLocation.Y > stripTop)
        {
            if (TryGetStripCell(screenLocation, out int stripCol, out int stripRow))
            {
                return gameSettings.untouchedFragments[stripCol, stripRow];
            }

            return null;
        }

        SKPoint normalized = new(screenLocation.X / gameSettings.zoomFactor, screenLocation.Y / gameSettings.zoomFactor);
        Fragment? selected = drawables.SelectFragmentOnClick(normalized);

#if WINDOWS
        if (selected is null)
        {
            selected = drawables.getNearestFragment(normalized);
        }
#endif

        return selected;
    }

    private bool TryGetStripCell(SKPoint location, out int col, out int row)
    {
        col = 0;
        row = 0;

        if (squir is null || gameSettings is null)
        {
            return false;
        }

        float stripTop = (float)squir.Height * gameSettings.prop1 / gameSettings.prop2;
        if (location.Y <= stripTop)
        {
            return false;
        }

        (int candidateCol, int candidateRow) = FindSlotOnBottomStrip(location);
        int maxCols = gameSettings.untouchedFragments.GetLength(0);
        int maxRows = gameSettings.untouchedFragments.GetLength(1);

        if (candidateCol < 0 || candidateCol >= maxCols || candidateRow < 0 || candidateRow >= maxRows)
        {
            return false;
        }

        col = candidateCol;
        row = candidateRow;
        return true;
    }

    private void SetHoveredFragment(Fragment? hovered)
    {
        if (gameSettings is null || ReferenceEquals(gameSettings.HoveredFragment, hovered))
        {
            return;
        }

        gameSettings.HoveredFragment = hovered;
        Invalidate();
    }

    private (int, int) FindSlotOnBottomStrip(SKPoint location)
    {
        var bottomStripHeight = squir.Height - ((float)squir.Height * gameSettings.prop1 / gameSettings.prop2);
        var onStripLocation = location;
        onStripLocation.Y -= ((float)squir.Height * gameSettings.prop1 / gameSettings.prop2);
        (int, int) selectedCell;
        selectedCell.Item2 = (int)(onStripLocation.Y / bottomStripHeight * gameSettings.Rows);
        selectedCell.Item1 = (int)((onStripLocation.X + gameSettings.bottomStripMove) / ((float)squir.Width / gameSettings.VisibleRows));
        return selectedCell;
    }

    private void TouchFragment(Fragment ff)
    {
        if (ff is null)
        {
            return;
        }

        ff.wasTouched = true;
        if (!gameSettings.CenterFragments.Contains(ff))
        {
            gameSettings.CenterFragments.Add(ff);
        }

        gameSettings.ActiveDraggedFragment = ff;
        gameSettings.HoveredFragment = null;
    }

    private IEnumerable<Fragment> AllFragments()
    {
        if (drawables is null)
        {
            return Enumerable.Empty<Fragment>();
        }

        return drawables.drawables.Skip(1).OfType<Fragment>();
    }

    private async Task LoadProgressAndRecordsAsync()
    {
        installId = await progressStore.GetOrCreateInstallIdAsync();

        levelProgress = await progressStore.LoadLevelProgressAsync(puzzleKey);
        if (levelProgress.BestSnapshot is null)
        {
            levelProgress.BestSnapshot = await progressStore.LoadSnapshotAsync(puzzleKey);
        }

        gameSettings.BestCoveragePercent = levelProgress.BestCoveragePercent;
        gameSettings.WorldRecordCoveragePercent = levelProgress.WorldRecordCoveragePercent;
        gameSettings.WorldRecordHolderInstallId = levelProgress.WorldRecordHolderInstallId;
        restoreBestButton.IsEnabled = levelProgress.BestSnapshot is not null;

        try
        {
            RecordSnapshot? remote = await leaderboardClient.GetRecordAsync(puzzleKey, installId);
            if (remote is not null)
            {
                gameSettings.WorldRecordCoveragePercent = remote.WorldRecordCoveragePercent;
                gameSettings.WorldRecordHolderInstallId = remote.WorldRecordHolderInstallId;
                gameSettings.BestCoveragePercent = Math.Max(gameSettings.BestCoveragePercent, remote.PlayerBestCoveragePercent ?? 0m);

                levelProgress.WorldRecordCoveragePercent = remote.WorldRecordCoveragePercent;
                levelProgress.WorldRecordHolderInstallId = remote.WorldRecordHolderInstallId;
                levelProgress.BestCoveragePercent = gameSettings.BestCoveragePercent;
                levelProgress.LastSyncedAtUtc = remote.UpdatedAtUtc ?? DateTimeOffset.UtcNow;

                await progressStore.SaveLevelProgressAsync(levelProgress);
            }
        }
        catch
        {
            // Offline or unreachable server is expected; local progress remains authoritative until next sync.
        }

        UpdateGui(gameSettings.AreaFilled);
        Invalidate();
    }

    private void UpdateStatusLabel()
    {
        if (recordStatusLabel is null || gameSettings is null)
        {
            return;
        }

        string world = gameSettings.WorldRecordCoveragePercent.HasValue
            ? $"{gameSettings.WorldRecordCoveragePercent.Value:F2}%"
            : "--";

        recordStatusLabel.Text =
            $"Best {gameSettings.BestCoveragePercent:F2}% | World {world} | Stars {gameSettings.CurrentStars}/3";
    }

    private async Task SaveBestIfImprovedAsync()
    {
        decimal currentCoverage = sessionState.CoveragePercent;
        if (currentCoverage <= gameSettings.BestCoveragePercent + CoverageComparisonTolerance)
        {
            return;
        }

        gameSettings.BestCoveragePercent = currentCoverage;
        LevelSnapshot snapshot = BuildCurrentSnapshot(currentCoverage);
        sessionState.CurrentPlacements = snapshot.PlacedFragments;

        levelProgress ??= new LevelProgress
        {
            PuzzleKey = puzzleKey
        };

        levelProgress.PuzzleKey = puzzleKey;
        levelProgress.BestCoveragePercent = currentCoverage;
        levelProgress.BestSnapshot = snapshot;

        await progressStore.SaveSnapshotAsync(snapshot);
        await progressStore.SaveLevelProgressAsync(levelProgress);

        restoreBestButton.IsEnabled = true;

        if (!string.IsNullOrWhiteSpace(installId))
        {
            ScoreSubmission submission = new()
            {
                PuzzleKey = puzzleKey,
                InstallId = installId,
                CoveragePercent = currentCoverage,
                AchievedAtUtc = DateTimeOffset.UtcNow
            };
            await recordSyncService.EnqueueBestScoreAsync(submission);
        }

        UpdateGui(gameSettings.AreaFilled);
        Invalidate();
    }

    private LevelSnapshot BuildCurrentSnapshot(decimal coveragePercent)
    {
        LevelSnapshot snapshot = new()
        {
            PuzzleKey = puzzleKey,
            CoveragePercent = coveragePercent
        };

        List<Fragment> fragments = AllFragments().ToList();
        for (int index = 0; index < fragments.Count; index++)
        {
            Fragment fragment = fragments[index];
            if (!fragment.wasTouched)
            {
                continue;
            }

            snapshot.PlacedFragments.Add(new PlacedFragmentState
            {
                FragmentIndex = index,
                PositionXWorld = fragment.PositionP.X,
                PositionYWorld = fragment.PositionP.Y,
                WasTouched = true
            });
        }

        return snapshot;
    }

    private void ApplySnapshot(LevelSnapshot snapshot)
    {
        List<Fragment> fragments = AllFragments().ToList();
        gameSettings.ActiveDraggedFragment = null;
        gameSettings.HoveredFragment = null;

        gameSettings.CenterFragments.Clear();
        gameSettings.TooLeftFragments.Clear();
        gameSettings.TooRightFragments.Clear();
        gameSettings.TooTopFragments.Clear();
        gameSettings.TooBottomFragments.Clear();

        for (int row = 0; row < gameSettings.Rows; row++)
        {
            for (int col = 0; col < gameSettings.Cols; col++)
            {
                gameSettings.untouchedFragments[col, row] = null;
            }
        }

        foreach (Fragment fragment in fragments)
        {
            fragment.wasTouched = false;
            gameSettings.untouchedFragments[fragment.IndexX, fragment.IndexY] = fragment;
        }

        foreach (PlacedFragmentState placed in snapshot.PlacedFragments)
        {
            if (placed.FragmentIndex < 0 || placed.FragmentIndex >= fragments.Count)
            {
                continue;
            }

            Fragment fragment = fragments[placed.FragmentIndex];
            fragment.wasTouched = placed.WasTouched;
            if (!fragment.wasTouched)
            {
                continue;
            }

            SKPoint screenPosition = coordinateTransformer.WorldToScreen(
                new SKPoint(placed.PositionXWorld, placed.PositionYWorld),
                (float)squir.Width,
                (float)squir.Height,
                zoomFactor: 1f,
                cameraOffsetWorld: new SKPoint(0f, 0f));

            fragment.PositionS = screenPosition;
            gameSettings.untouchedFragments[fragment.IndexX, fragment.IndexY] = null;
            gameSettings.CenterFragments.Add(fragment);
        }

        UpdateCover();
        Invalidate();
    }

    private async void RestoreBestButton_Clicked(object sender, EventArgs e)
    {
        LevelSnapshot? snapshot = levelProgress?.BestSnapshot ?? await progressStore.LoadSnapshotAsync(puzzleKey);
        if (snapshot is null)
        {
            restoreBestButton.IsEnabled = false;
            return;
        }

        ApplySnapshot(snapshot);
    }

    private async void SettingsButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }

    private void SnapToggle_Toggled(object sender, ToggledEventArgs e)
    {
        sessionState.SnapEnabled = e.Value;
        if (gameSettings is not null)
        {
            gameSettings.SnapEnabled = e.Value;
        }

        UpdateStatusLabel();
    }
   
}


