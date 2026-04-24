using Fillsquir.Controls;
using Fillsquir.Domain;
using Fillsquir.Services;
using Fillsquir.Visuals;
using Microsoft.Maui.ApplicationModel;
using SkiaSharp;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Fillsquir.Campaign;

namespace Fillsquir;


public partial class GamePage : ContentPage, IQueryAttributable
{
    private const float WallSnapAngleDotThreshold = 0.998f;
    private const float WallSnapDistanceThreshold = 24f;
    private const float WallSnapAlongAxisThreshold = 18f;
    private const float WallSnapMaxTranslation = 40f;
    private const float WallSnapTranslationAgreement = 3f;
    private const float PinchZoomSensitivity = 1f / 3f;
    private const float WheelZoomStep = 0.5f / 3f;
    private static readonly TimeSpan RenderTickerPollInterval = TimeSpan.FromMilliseconds(16);
    private const double LowQualityFrameMs = 50d;
    private const double MediumQualityFrameMs = 1000d / 28d;
    private const double HighQualityFrameMs = 1000d / 36d;
    private const double PointerGlintInvalidateMinMs = 24d;
    private const float PointerGlintMoveThresholdPx = 2f;

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
    private bool renderTickerRunning;
    private bool isPageVisible;
    private double lastContinuousRenderAtMs;
    private readonly Stopwatch renderClock = Stopwatch.StartNew();
    private int renderTickerGeneration;
    private double lastPointerGlintInvalidateAtMs;
    private SKPoint lastPointerGlintInvalidatePosition;
    private bool hasLastPointerGlintInvalidatePosition;
    private bool handlersWired;
    private GameHudController hud = null!;

    private readonly PanGestureRecognizer panGesture = new();
    private readonly PointerGestureRecognizer pointGesture = new();
    private readonly PinchGestureRecognizer zoomGesture = new();

    GameSettings settings = null!;
    public GamePage()
    {
        BindingContext = new GamePageViewModel();

        IServiceProvider? services = App.Services;
        progressStore = services?.GetService(typeof(IProgressStore)) as IProgressStore ?? new JsonFileProgressStore();
        leaderboardClient = services?.GetService(typeof(ILeaderboardClient)) as ILeaderboardClient
            ?? new HttpLeaderboardClient(LeaderboardClientFactory.CreateHttpClient());
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
        hud = new GameHudController(
            levelStatusLabel,
            coverageStatusLabel,
            recordStatusLabel,
            syncStatusLabel,
            coverageProgressBar,
            statusToast,
            statusToastLabel);
        WireInputAndRenderHandlers();
        snapToggle.IsToggled = true;
        UpdateStatusLabel();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        isPageVisible = true;
        if (!subscribedToVisualSettings)
        {
            visualSettingsState.Changed += VisualSettingsState_Changed;
            subscribedToVisualSettings = true;
        }

        _ = recordSyncService.TriggerSyncAsync();
        RestoreCanvasLayoutAndRedraw();
        _ = LoadAndApplyVisualSettingsAsync();
        EnsureAdaptiveRenderTicker();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        isPageVisible = false;
        hud.CancelToast();
        CancelTransientInteractions(restoreDetachedUntouchedFragment: true);
        StopRenderTicker();
        if (!subscribedToVisualSettings)
        {
            return;
        }

        visualSettingsState.Changed -= VisualSettingsState_Changed;
        subscribedToVisualSettings = false;
    }

    private void CancelTransientInteractions(bool restoreDetachedUntouchedFragment)
    {
        if (gameSettings is null)
        {
            moved = null;
            movingStatus = moveStatus.none;
            return;
        }

        if (restoreDetachedUntouchedFragment && moved is not null && !moved.wasTouched)
        {
            int col = moved.IndexX;
            int row = moved.IndexY;
            if (col >= 0
                && row >= 0
                && col < gameSettings.untouchedFragments.GetLength(0)
                && row < gameSettings.untouchedFragments.GetLength(1)
                && gameSettings.untouchedFragments[col, row] is null)
            {
                gameSettings.untouchedFragments[col, row] = moved;
            }
        }

        moved = null;
        movingStatus = moveStatus.none;
        isZooming = 0;
        zoomSum = 0f;
        was2FingerTouched = false;
        fingersMove = default;
        fingersLocked = false;
        currMoveWhenZooming = default;
        removeFromMoveWhenZooming = default;
        isPanTracking = false;
        previousPanTotal = default;
        isTouchInteractionActive = false;
        hasLastTouchLocation = false;
        lastTouchLocation = default;
        touchDragTotal = default;
        hasLastPointerGlintInvalidatePosition = false;
        lastPointerGlintInvalidateAtMs = 0d;
        gameSettings.ActiveDraggedFragment = null;
        gameSettings.HoveredFragment = null;
        gameSettings.HasGlintPointer = false;
        ClearPendingStripGrabAnchor();
    }

    private void RestoreCanvasLayoutAndRedraw()
    {
        if (gameSettings is null || drawables is null)
        {
            return;
        }

        CancelTransientInteractions(restoreDetachedUntouchedFragment: true);

        float width = (float)squir.Width;
        float height = (float)squir.Height;
        if (width > 0f && height > 0f)
        {
            drawables.Resize(width, height);
            drawables.cover.Resize(width, height);
            drawables.Gui.Resize(width, height);
        }

        Invalidate();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        int level = 1;
        if (query is not null
            && query.TryGetValue("Level", out object? levelObject)
            && int.TryParse(levelObject?.ToString(), out int parsedLevel)
            && parsedLevel > 0)
        {
            level = parsedLevel;
        }

        const int seed = 0;

        settings = new(seed, level);
        // Load optional level profile and apply overrides (best-effort)
        try
        {
            string profilesPath = Path.Combine(AppContext.BaseDirectory, "Campaign", "level_profiles.json");
            if (File.Exists(profilesPath))
            {
                string json = File.ReadAllText(profilesPath);
                LevelProfiles? levelProfiles = JsonSerializer.Deserialize<LevelProfiles>(json);
                LevelProfile? profile = levelProfiles?.ForLevel(level);
                if (profile is not null)
                {
                    if (profile.Fragments.HasValue)
                    {
                        settings.fragments = Math.Max(1, profile.Fragments.Value);
                    }

                    settings.SnapMultiplier = profile.SnapMultiplier ?? settings.SnapMultiplier;
                    settings.EnableHint = profile.EnableHint;
                    settings.SingleUseGhostHint = profile.SingleUseGhostHint;
                    settings.TimeLimitSeconds = profile.TimeLimitSeconds;
                    settings.AnchorMode = profile.AnchorMode ?? settings.AnchorMode;
                }
            }
        }
        catch
        {
            // best-effort only; ignore failures
        }

        puzzleKey = new PuzzleKey(level, seed, GameRules.RulesVersion);
        ApplyVisualSettingsToSettings(settings, currentVisualSettings, invalidateTextureCache: false);
        InitializeSquir(settings);
        _ = LoadProgressAndRecordsAsync();
        _ = LoadAndApplyVisualSettingsAsync();
    }

    Squir drawa = null!;
    DrawableStack drawables = null!;
    SKPoint startingPoint = new();
    SKPoint TapPosition = new();
    Fragment? moved;
    GameSettings gameSettings = null!;
    CommonArea commonArea = null!;

    SKPoint dlocation;
    SKPoint d;

    int isZooming = 0;
    float zoomSum = 0f;
    bool was2FingerTouched = false;
    SKPoint fingersMove;
    bool fingersLocked = false;

    SKPoint fingersMoveOnZooming { get { return new(); } }
    SKPoint currMoveWhenZooming;
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
                try
                {
                    var fragment = new Fragment(fragmentpoints[i++], c, r, gameSettings);
                    gameSettings.untouchedFragments[c, r] = fragment;
                    drawables.AddDrawable(fragment);
                }
                catch
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

        // Apply anchors from level profile (best-effort)
        try
        {
            if (!string.IsNullOrWhiteSpace(gameSettings.AnchorMode) && !string.Equals(gameSettings.AnchorMode, "none", StringComparison.OrdinalIgnoreCase))
            {
                var fragmentsList = drawables.drawables.Skip(1).OfType<Fragment>().ToList();
                if (fragmentsList.Count > 0)
                {
                    Fragment? anchor = null;
                    if (string.Equals(gameSettings.AnchorMode, "lock-first", StringComparison.OrdinalIgnoreCase))
                    {
                        anchor = fragmentsList.First();
                    }
                    else if (gameSettings.AnchorMode.StartsWith("lock-random", StringComparison.OrdinalIgnoreCase))
                    {
                        int idx = Math.Clamp(gameSettings.rand.Next(0, fragmentsList.Count), 0, fragmentsList.Count - 1);
                        anchor = fragmentsList[idx];
                    }

                    if (anchor is not null)
                    {
                        // Mark as touched and add to center list
                        TouchFragment(anchor);

                        // Place roughly at board center
                        try
                        {
                            var boardPts = drawa.VisiblePoints;
                            if (boardPts is not null && boardPts.Length > 0)
                            {
                                float minX = boardPts.Min(p => p.X);
                                float minY = boardPts.Min(p => p.Y);
                                float maxX = boardPts.Max(p => p.X);
                                float maxY = boardPts.Max(p => p.Y);
                                SKPoint center = new SKPoint((minX + maxX) / 2f, (minY + maxY) / 2f);
                                int finalIndex = Math.Max(0, anchor.PointsP.Length / 2);
                                anchor.SetPositionToPointLocation(center, finalIndex);
                            }
                        }
                        catch
                        {
                            // ignore placement failures
                        }

                        anchor.IsLocked = true;
                        gameSettings.ActiveDraggedFragment = null;
                    }
                }
            }
        }
        catch
        {
            // best-effort only
        }

        ResetInteractionStateForNewLevel();
        Invalidate();
    }

    private void WireInputAndRenderHandlers()
    {
        if (handlersWired)
        {
            return;
        }

        handlersWired = true;
        squir.PaintSurface += OnSquirPaintSurface;
        zoomGesture.PinchUpdated += ZoomGesture_PinchUpdated;
        panGesture.PanUpdated += PanGesture_PanUpdated;
        pointGesture.PointerEntered += PointGesture_PointerEntered;
        pointGesture.PointerMoved += PointGesture_PointerMoved;
        pointGesture.PointerExited += PointGesture_PointerExited;

        grid.GestureRecognizers.Add(panGesture);
        grid.GestureRecognizers.Add(zoomGesture);
        grid.GestureRecognizers.Add(pointGesture);
        squir.EnableTouchEvents = true;
    }

    private void OnSquirPaintSurface(object? sender, SkiaSharp.Views.Maui.SKPaintGLSurfaceEventArgs e)
    {
        if (gameSettings is null || drawables is null)
        {
            return;
        }

        double nowSeconds = renderClock.Elapsed.TotalSeconds;
        gameSettings.RenderTimeSeconds = (float)nowSeconds;
        var canvas = e.Surface.Canvas;
        canvas.Clear();

        canvas.Save();
        canvas.Scale(gameSettings.zoomFactor);
        drawables.DrawPreZoom(canvas, nowSeconds);
        canvas.Restore();

        drawables.DrawPastZoom(canvas);

        canvas.Save();
        canvas.Scale(gameSettings.zoomFactor);
        drawables.DrawFragmentsoutlines(canvas);
        canvas.Restore();
    }

    private void ZoomGesture_PinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (gameSettings is null || drawables is null)
        {
            return;
        }

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
                zoomPrev = gameSettings.zoomFactor;
                break;
            case GestureStatus.Running:
            {
                float adjustedPinchScale = 1f + (((float)e.Scale - 1f) * PinchZoomSensitivity);
                zoomSum += 1f - adjustedPinchScale;
                gameSettings.zoomFactor *= adjustedPinchScale;

                currMoveWhenZooming = new SKPoint((float)(e.ScaleOrigin.X * squir.Width), (float)(e.ScaleOrigin.Y * squir.Height));
                currMoveWhenZooming.X -= d.X;
                currMoveWhenZooming.Y -= d.Y;
                currMoveWhenZooming.X /= gameSettings.zoomFactor;
                currMoveWhenZooming.Y /= gameSettings.zoomFactor;
                SetCameraToZoomAndMove(currMoveWhenZooming);

                if (drawables.Gui is PercentageDisplay percentageDisplay)
                {
                    percentageDisplay.debugString = e.Scale.ToString();
                }

                break;
            }
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                isZooming = Math.Max(0, isZooming - 1);
                if (isZooming == 0)
                {
                    removeFromMoveWhenZooming = new();
                    zoomSum = 0f;
                    fingersLocked = false;
                    fingersMove = new();
                    was2FingerTouched = false;
                }

                break;
        }

        Invalidate();
    }

    private void PointGesture_PointerEntered(object? sender, PointerEventArgs e)
    {
        Microsoft.Maui.Graphics.Point? pointerPosition = e.GetPosition(squir);
        if (pointerPosition.HasValue)
        {
            SKPoint pointer = new((float)pointerPosition.Value.X, (float)pointerPosition.Value.Y);
            UpdateGlintPointer(pointer, isActive: true);
            UpdateHoveredFragmentFromPointer(pointer);
            MaybeInvalidateForPointerGlint(pointer);
        }
    }

    private void PointGesture_PointerMoved(object? sender, PointerEventArgs e)
    {
        Microsoft.Maui.Graphics.Point? pointerPosition = e.GetPosition(squir);
        if (pointerPosition.HasValue)
        {
            SKPoint pointer = new((float)pointerPosition.Value.X, (float)pointerPosition.Value.Y);
            UpdateGlintPointer(pointer, isActive: true);
            UpdateHoveredFragmentFromPointer(pointer);
            MaybeInvalidateForPointerGlint(pointer);
            return;
        }

        UpdateGlintPointer(default, isActive: false);
        SetHoveredFragment(null);
    }

    private void PointGesture_PointerExited(object? sender, PointerEventArgs e)
    {
        UpdateGlintPointer(default, isActive: false);
        SetHoveredFragment(null);
    }

    private void ResetInteractionStateForNewLevel()
    {
        StopRenderTicker();
        movingStatus = moveStatus.none;
        moved = null;
        isZooming = 0;
        zoomSum = 0f;
        was2FingerTouched = false;
        fingersMove = default;
        fingersLocked = false;
        currMoveWhenZooming = default;
        removeFromMoveWhenZooming = default;
        isPanTracking = false;
        previousPanTotal = default;
        isTouchInteractionActive = false;
        hasLastTouchLocation = false;
        lastTouchLocation = default;
        touchDragTotal = default;
        hasLastPointerGlintInvalidatePosition = false;
        lastPointerGlintInvalidateAtMs = 0d;
        gameSettings.ActiveDraggedFragment = null;
        gameSettings.HoveredFragment = null;
        gameSettings.HasGlintPointer = false;
        gameSettings.bottomStripMove = 0f;
    }

    private void StopRenderTicker()
    {
        renderTickerRunning = false;
        renderTickerGeneration++;
    }

#if DebugClickingLines
    private void HandleDebugClickingLines(SKPoint mousePosition)
    {
        if (drawables is null)
        {
            return;
        }
    }
#endif

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
            if (drawables?.Gui is PercentageDisplay percentageDisplay)
            {
                percentageDisplay.SyncStars(gameSettings.CurrentStars);
            }
            UpdateStatusLabel();
        }


        void Invalidate()
        {
            if (gameSettings is not null)
            {
                gameSettings.RenderTimeSeconds = (float)renderClock.Elapsed.TotalSeconds;
            }

            squir.InvalidateSurface();
            EnsureAdaptiveRenderTicker();
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
            targetSettings.GlintMotionMode = normalized.GlintMotionMode;
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
            float width = (float)squir.Width;
            float height = (float)squir.Height;
            if (width <= 0f || height <= 0f)
            {
                return;
            }

            if (drawa != null && drawables != null)
            {
                //drawa.Resize(squir.Width, squir.Height);
                drawables.Resize(width, height);
                drawables.cover.Resize(width, height);
                drawables.Gui.Resize(width, height);
                Invalidate();
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

    private bool isPanTracking;
    private SKPoint previousPanTotal;
    private bool isTouchInteractionActive;
    private bool hasLastTouchLocation;
    private SKPoint lastTouchLocation;
    private SKPoint touchDragTotal;
    private bool hasPendingStripGrabAnchor;
    private Fragment? pendingStripGrabFragment;
    private float pendingStripGrabRatioX = 0.5f;
    private float pendingStripGrabRatioY = 0.5f;
    private void PanGesture_PanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (isTouchInteractionActive || was2FingerTouched)
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                isPanTracking = true;
                previousPanTotal = new SKPoint((float)e.TotalX, (float)e.TotalY);
                return;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                isPanTracking = false;
                previousPanTotal = default;
                if (movingStatus == moveStatus.map)
                {
                    movingStatus = moveStatus.none;
                    gameSettings.ActiveDraggedFragment = null;
                    touchDragTotal = default;
                }
                return;
            case GestureStatus.Running:
                if (!isPanTracking)
                {
                    isPanTracking = true;
                    previousPanTotal = new SKPoint((float)e.TotalX, (float)e.TotalY);
                    return;
                }

                break;
            default:
                return;
        }

        float deltaScreenX = (float)e.TotalX - previousPanTotal.X;
        float deltaScreenY = (float)e.TotalY - previousPanTotal.Y;
        previousPanTotal = new SKPoint((float)e.TotalX, (float)e.TotalY);

        if (Math.Abs(deltaScreenX) < 0.0001f && Math.Abs(deltaScreenY) < 0.0001f)
        {
            return;
        }

        touchDragTotal.X += deltaScreenX;
        touchDragTotal.Y += deltaScreenY;
        ApplyDragDelta(deltaScreenX, deltaScreenY);
        Invalidate();
    }

    private void ApplyDragDelta(float deltaScreenX, float deltaScreenY)
    {
        float deltaWorldX = deltaScreenX / gameSettings.zoomFactor;
        float deltaWorldY = deltaScreenY / gameSettings.zoomFactor;

        switch (movingStatus)
        {
            case moveStatus.undecided:
                {
                    float absX = Math.Abs(touchDragTotal.X);
                    float absY = Math.Abs(touchDragTotal.Y);

                    if (absX > absY + 5f)
                    {
                        movingStatus = moveStatus.bottomStrip;
                        gameSettings.ActiveDraggedFragment = null;
                        goto case moveStatus.bottomStrip;
                    }

                    if (absY > absX + 5f)
                    {
                        movingStatus = moveStatus.fragment;
                        if (moved is not null)
                        {
                            if (moved.wasTouched)
                            {
                                startingPoint = moved.PositionS;
                            }

                            TouchFragment(moved);
                            AlignDraggedFragmentToPointer(moved, lastTouchLocation);
                            goto case moveStatus.fragment;
                        }
                    }

                    break;
                }
            case moveStatus.map:
                {
                    gameSettings.ActiveDraggedFragment = null;

                    gameSettings.xoffset += deltaWorldX;
                    gameSettings.yoffset += deltaWorldY;

                    var xMoveTotal = deltaWorldX;
                    var yMoveTotal = deltaWorldY;

                    if (xMoveTotal < 0)
                    {
                        GameSettings.MoveFragmentsBetweenLists(gameSettings.CenterFragments, gameSettings.TooLeftFragments,
                            drawable => (((drawable.PositionP.X + drawable.sizeP.X) * (squir.Width / 1000)) + gameSettings.xoffset < 0f));
                        GameSettings.MoveFragmentsBetweenLists(gameSettings.TooRightFragments, gameSettings.CenterFragments,
                            drawable => (((drawable.PositionP.X * (squir.Width / 1000))) + gameSettings.xoffset) < (squir.Width / gameSettings.zoomFactor));
                    }

                    if (xMoveTotal > 0)
                    {
                        GameSettings.MoveFragmentsBetweenLists(gameSettings.TooLeftFragments, gameSettings.CenterFragments,
                            drawable => (((drawable.PositionP.X + drawable.sizeP.X) * (squir.Width / 1000)) + gameSettings.xoffset > 0f));
                        GameSettings.MoveFragmentsBetweenLists(gameSettings.CenterFragments, gameSettings.TooRightFragments,
                            drawable => (((drawable.PositionP.X * (squir.Width / 1000))) + gameSettings.xoffset) > (squir.Width / gameSettings.zoomFactor));
                    }

                    if (yMoveTotal < 0)
                    {
                        GameSettings.MoveFragmentsBetweenLists(gameSettings.CenterFragments, gameSettings.TooTopFragments,
                            drawable => (((drawable.PositionP.Y + drawable.sizeP.Y) * (squir.Height / 1000)) + gameSettings.yoffset < 0f));
                        GameSettings.MoveFragmentsBetweenLists(gameSettings.TooBottomFragments, gameSettings.CenterFragments,
                            drawable => (((drawable.PositionP.Y * (squir.Height / 1000))) + gameSettings.yoffset) < (squir.Height / gameSettings.zoomFactor));
                    }

                    if (yMoveTotal > 0)
                    {
                        GameSettings.MoveFragmentsBetweenLists(gameSettings.TooTopFragments, gameSettings.CenterFragments,
                            drawable => (((drawable.PositionP.Y + drawable.sizeP.Y) * (squir.Height / 1000)) + gameSettings.yoffset > 0f));
                        GameSettings.MoveFragmentsBetweenLists(gameSettings.CenterFragments, gameSettings.TooBottomFragments,
                            drawable => (((drawable.PositionP.Y * (squir.Height / 1000))) + gameSettings.yoffset) > (squir.Height / gameSettings.zoomFactor));
                    }

                    ((PercentageDisplay)drawables.Gui).debugString = gameSettings.CenterFragments.Count.ToString();
                    break;
                }
            case moveStatus.fragment:
                {
                    if (moved == null)
                    {
                        movingStatus = moveStatus.none;
                        gameSettings.ActiveDraggedFragment = null;
                        return;
                    }

                    moved.PositionS.X += deltaWorldX;
                    moved.PositionS.Y += deltaWorldY;
                    UpdateCover();
                    break;
                }
            case moveStatus.bottomStrip:
                {
                    gameSettings.ActiveDraggedFragment = null;
                    var pos = gameSettings.bottomStripMove - deltaScreenX;
                    if (pos <= 0)
                    {
                        pos = 0;
                    }
                    else
                    {
                        var totalStripLength = ((float)gameSettings.Cols / (float)gameSettings.VisibleRows) * (float)squir.Width - (float)squir.Width;
                        if (totalStripLength <= pos)
                        {
                            pos = totalStripLength;
                        }
                    }

                    gameSettings.bottomStripMove = pos;
                    break;
                }
        }

    }

    SKPoint offsetMoveLocation;

    float zoomPrev = 1;
    SKPoint zoomPos;
    float prevXOffset;
    float prevYOffset;

    private void squir_Touch(object sender, SkiaSharp.Views.Maui.SKTouchEventArgs e)
        {
        TapPosition = e.Location;
        var location = e.Location;
        if (e.ActionType == SkiaSharp.Views.Maui.SKTouchAction.Pressed)
        {
            ClearPendingStripGrabAnchor();
            isTouchInteractionActive = true;
            hasLastTouchLocation = true;
            lastTouchLocation = e.Location;
            touchDragTotal = default;
            UpdateGlintPointer(e.Location, isActive: true);
        }
        else if (e.ActionType == SkiaSharp.Views.Maui.SKTouchAction.Released
            || e.ActionType == SkiaSharp.Views.Maui.SKTouchAction.Cancelled)
        {
            isTouchInteractionActive = false;
            hasLastTouchLocation = false;
            touchDragTotal = default;
            UpdateGlintPointer(default, isActive: false);
        }

        if ((was2FingerTouched || isZooming > 0) && e.ActionType != SkiaSharp.Views.Maui.SKTouchAction.WheelChanged)
        {
            hasLastTouchLocation = true;
            lastTouchLocation = e.Location;
            e.Handled = true;
            return;
        }

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
                    if (moved is not null)
                    {
                        CapturePendingStripGrabAnchor(moved, e.Location);
                    }
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
                    if (moved is not null && moved.IsLocked)
                    {
                        // Clicked on a locked anchor — ignore interaction.
                        movingStatus = moveStatus.none;
                        gameSettings.ActiveDraggedFragment = null;
                        SetHoveredFragment(null);
                        return;
                    }
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
                    AlignDraggedFragmentToPointer(moved, e.Location);

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

#if DebugClicking
        SKPoint mp = new SKPoint() { X = location.X, Y = location.Y };
        bool inside = FSMath.IsPointInShape(mp, ((Squir)drawables[0]).VisiblePoints);
        drawables.AddDot(mp, inside);
        Invalidate();
#endif
        zoomPos = location;
        bool shouldInvalidate = false;
        switch (e.ActionType)
        {
            case SkiaSharp.Views.Maui.SKTouchAction.Pressed:
                {
                    Fragment? hoveredAtPress = ResolveFragmentForScreenLocation(e.Location);
                    SetHoveredFragment(hoveredAtPress);
                    if (e.MouseButton == SkiaSharp.Views.Maui.SKMouseButton.Middle)
                    {
                        StartMovingMap();
                        break;
                    }

                    moved = hoveredAtPress;
                    if (moved is not null && moved.IsLocked)
                    {
                        // Pressed on a locked anchor — ignore movement.
                        movingStatus = moveStatus.none;
                        gameSettings.ActiveDraggedFragment = null;
                        SetHoveredFragment(null);
                        shouldInvalidate = true;
                        break;
                    }
                    if (moved == null)
                    {
                        bottomStripMovePre = gameSettings.bottomStripMove;
                        gameSettings.ActiveDraggedFragment = null;
                        StartMovingMap();
                        shouldInvalidate = true;
                        break;
                    } //probably will be needed one day
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
                    shouldInvalidate = true;
                    break;
                }
            case SkiaSharp.Views.Maui.SKTouchAction.Released:
                {
                    if (moved == null)
                    {
                        movingStatus = moveStatus.none;
                        gameSettings.ActiveDraggedFragment = null;
                        SetHoveredFragment(null);
                        touchDragTotal = default;
                        ClearPendingStripGrabAnchor();
                        return;
                    }

                    if (!moved.wasTouched)
                    {
                        if (gameSettings.untouchedFragments[moved.IndexX, moved.IndexY] is null)
                        {
                            gameSettings.untouchedFragments[moved.IndexX, moved.IndexY] = moved;
                        }

                        moved = null;
                        movingStatus = moveStatus.none;
                        gameSettings.ActiveDraggedFragment = null;
                        SetHoveredFragment(null);
                        touchDragTotal = default;
                        ClearPendingStripGrabAnchor();
                        shouldInvalidate = true;
                        break;
                    }

                    if (gameSettings.SnapEnabled && TryGetWallSnapTranslation(moved, out var snapTranslation))
                    {
                        moved.PositionS = new SKPoint(
                            moved.PositionS.X + snapTranslation.X,
                            moved.PositionS.Y + snapTranslation.Y);
                    }

                    if (IsDroppedOnTargetShape(moved))
                    {
                        double nowSeconds = renderClock.Elapsed.TotalSeconds;
                        drawables.SpawnDropParticles(moved, nowSeconds);
                        EnsureAdaptiveRenderTicker();
                    }

                    moved.TriggerReleaseSettle();
                    moved = null;
                    movingStatus = moveStatus.none;
                    gameSettings.ActiveDraggedFragment = null;
                    SetHoveredFragment(null);
                    ClearPendingStripGrabAnchor();
                    UpdateCover();
                    _ = SaveBestIfImprovedAsync();
                    //UpdateGui();
                    shouldInvalidate = true;
                    break;
                }
            case SkiaSharp.Views.Maui.SKTouchAction.WheelChanged:
                {

                    if (e.WheelDelta > 0)
                    {
                        gameSettings.zoomFactor += WheelZoomStep;
                    }
                    else
                    {
                        if (gameSettings.zoomFactor <= 0.5f)
                        {
                            return;
                        }
                        gameSettings.zoomFactor -= WheelZoomStep;
                    }
                    // var zoomprop = gameSettings.zoomFactor / zoomPrev;
                   // var xfromhere = -gameSettings.xoffset + (e.Location.X / zoomPrev);
                   // var yfromhere = -gameSettings.yoffset + (e.Location.Y / zoomPrev);
                    gameSettings.xoffset = -location.X + gameSettings.xoffset + (e.Location.X / gameSettings.zoomFactor);
                    gameSettings.yoffset = -location.Y + gameSettings.yoffset + (e.Location.Y / gameSettings.zoomFactor);
                    

                    


                    //var xd = location.X;
                    //var difference = xd - xfromhere;
                    shouldInvalidate = true;

                    //var 


                    //but it can't be constant 1 pixel it has to be something else, maybe librarys have different approach to this



                    //but notice that by default it zooms to the left top corner
                    //so to adjust xoffset you need to add difference that was made by that




                    break;
                }
                case SkiaSharp.Views.Maui.SKTouchAction.Moved:
                {
                    UpdateGlintPointer(e.Location, isActive: true);

                    if (was2FingerTouched || isZooming > 0)
                    {
                        hasLastTouchLocation = true;
                        lastTouchLocation = e.Location;
                        shouldInvalidate = true;
                        break;
                    }

                    if (!hasLastTouchLocation)
                    {
                        hasLastTouchLocation = true;
                        lastTouchLocation = e.Location;
                        return;
                    }

                    float deltaScreenX = e.Location.X - lastTouchLocation.X;
                    float deltaScreenY = e.Location.Y - lastTouchLocation.Y;
                    lastTouchLocation = e.Location;

                    touchDragTotal.X += deltaScreenX;
                    touchDragTotal.Y += deltaScreenY;
                    ApplyDragDelta(deltaScreenX, deltaScreenY);
                    shouldInvalidate = true;
                    break;
                }
            }

        if (shouldInvalidate)
        {
            Invalidate();
        }
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
    }

    private bool IsDroppedOnTargetShape(Fragment fragment)
    {
        if (fragment is null || drawa is null)
        {
            return false;
        }

        return FSMath.IsPointInShape(fragment.Centroid, drawa.VisiblePoints);
    }

    private void EnsureAdaptiveRenderTicker()
    {
        if (renderTickerRunning || !isPageVisible || !NeedsContinuousRendering())
        {
            return;
        }

        renderTickerRunning = true;
        lastContinuousRenderAtMs = 0d;
        int tickerGeneration = ++renderTickerGeneration;

        Dispatcher.StartTimer(RenderTickerPollInterval, () =>
        {
            if (tickerGeneration != renderTickerGeneration)
            {
                renderTickerRunning = false;
                return false;
            }

            if (!isPageVisible)
            {
                renderTickerRunning = false;
                return false;
            }

            if (gameSettings is null || drawables is null || !NeedsContinuousRendering())
            {
                renderTickerRunning = false;
                return false;
            }

            double nowMs = renderClock.Elapsed.TotalMilliseconds;
            double targetFrameMs = GetTargetFrameIntervalMs(gameSettings.QualityTier);
            if ((nowMs - lastContinuousRenderAtMs) < targetFrameMs)
            {
                return true;
            }

            lastContinuousRenderAtMs = nowMs;
            gameSettings.RenderTimeSeconds = (float)renderClock.Elapsed.TotalSeconds;
            squir.InvalidateSurface();
            return true;
        });
    }

    private bool NeedsContinuousRendering()
    {
        if (!isPageVisible || gameSettings is null || drawables is null)
        {
            return false;
        }

        if (drawables.HasActiveDropParticles)
        {
            return true;
        }

        if (drawables.HasActiveGuiAnimations)
        {
            return true;
        }

        if (!HasGlintPieces())
        {
            return false;
        }

        return gameSettings.GlintMotionMode != GlintMotionMode.MouseDriven;
    }

    private bool HasGlintPieces()
    {
        if (gameSettings is null || gameSettings.QualityTier != GraphicsQualityTier.High)
        {
            return false;
        }

        return gameSettings.CenterFragments.Count > 0;
    }

    private static double GetTargetFrameIntervalMs(GraphicsQualityTier qualityTier)
    {
        return qualityTier switch
        {
            GraphicsQualityTier.Low => LowQualityFrameMs,
            GraphicsQualityTier.Medium => MediumQualityFrameMs,
            _ => HighQualityFrameMs
        };
    }

    private void UpdateGlintPointer(SKPoint screenLocation, bool isActive)
    {
        if (gameSettings is null)
        {
            return;
        }

        if (!isActive)
        {
            gameSettings.HasGlintPointer = false;
            hasLastPointerGlintInvalidatePosition = false;
            lastPointerGlintInvalidateAtMs = 0d;
            return;
        }

        float safeZoom = Math.Max(gameSettings.zoomFactor, 0.0001f);
        gameSettings.GlintPointerPosition = new SKPoint(
            screenLocation.X / safeZoom,
            screenLocation.Y / safeZoom);
        gameSettings.HasGlintPointer = true;
    }

    private void MaybeInvalidateForPointerGlint(SKPoint pointerScreenLocation)
    {
        if (!isPageVisible || gameSettings is null || gameSettings.GlintMotionMode == GlintMotionMode.AlwaysDrift)
        {
            return;
        }

        if (!HasGlintPieces() || !gameSettings.HasGlintPointer)
        {
            return;
        }

        double nowMs = renderClock.Elapsed.TotalMilliseconds;
        if (hasLastPointerGlintInvalidatePosition)
        {
            if ((nowMs - lastPointerGlintInvalidateAtMs) < PointerGlintInvalidateMinMs
                && PointDistance(pointerScreenLocation, lastPointerGlintInvalidatePosition) < PointerGlintMoveThresholdPx)
            {
                return;
            }
        }

        lastPointerGlintInvalidateAtMs = nowMs;
        lastPointerGlintInvalidatePosition = pointerScreenLocation;
        hasLastPointerGlintInvalidatePosition = true;
        Invalidate();
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
        return drawables.SelectFragmentOnClick(normalized);
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

    private void AlignDraggedFragmentToPointer(Fragment fragment, SKPoint pointerScreenLocation)
    {
        if (fragment is null || gameSettings is null)
        {
            return;
        }

        float safeZoom = Math.Max(gameSettings.zoomFactor, 0.0001f);
        SKPoint pointerPreZoom = new(pointerScreenLocation.X / safeZoom, pointerScreenLocation.Y / safeZoom);
        float pieceWidth = fragment.scaleToMiddleX(fragment.sizeP.X);
        float pieceHeight = fragment.scaleToMiddleY(fragment.sizeP.Y);
        float anchorRatioX = 0.5f;
        float anchorRatioY = 0.5f;

        if (hasPendingStripGrabAnchor && ReferenceEquals(pendingStripGrabFragment, fragment))
        {
            anchorRatioX = pendingStripGrabRatioX;
            anchorRatioY = pendingStripGrabRatioY;
        }

        float anchorX = pieceWidth * anchorRatioX;
        float anchorY = pieceHeight * anchorRatioY;

        fragment.PositionS = new SKPoint(
            pointerPreZoom.X - gameSettings.xoffset - anchorX,
            pointerPreZoom.Y - gameSettings.yoffset - anchorY);

        if (hasPendingStripGrabAnchor && ReferenceEquals(pendingStripGrabFragment, fragment))
        {
            ClearPendingStripGrabAnchor();
        }
    }

    private void CapturePendingStripGrabAnchor(Fragment fragment, SKPoint pointerScreenLocation)
    {
        SKPoint[] points = fragment.VisiblePointsS;
        if (points is null || points.Length == 0)
        {
            pendingStripGrabRatioX = 0.5f;
            pendingStripGrabRatioY = 0.5f;
            pendingStripGrabFragment = fragment;
            hasPendingStripGrabAnchor = true;
            return;
        }

        float minX = points[0].X;
        float minY = points[0].Y;
        float maxX = points[0].X;
        float maxY = points[0].Y;

        for (int i = 1; i < points.Length; i++)
        {
            SKPoint point = points[i];
            if (point.X < minX) minX = point.X;
            if (point.X > maxX) maxX = point.X;
            if (point.Y < minY) minY = point.Y;
            if (point.Y > maxY) maxY = point.Y;
        }

        float width = Math.Max(0.001f, maxX - minX);
        float height = Math.Max(0.001f, maxY - minY);

        pendingStripGrabRatioX = Math.Clamp((pointerScreenLocation.X - minX) / width, 0f, 1f);
        pendingStripGrabRatioY = Math.Clamp((pointerScreenLocation.Y - minY) / height, 0f, 1f);
        pendingStripGrabFragment = fragment;
        hasPendingStripGrabAnchor = true;
    }

    private void ClearPendingStripGrabAnchor()
    {
        hasPendingStripGrabAnchor = false;
        pendingStripGrabFragment = null;
        pendingStripGrabRatioX = 0.5f;
        pendingStripGrabRatioY = 0.5f;
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
        SetSyncStatus("Loading");

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
            SetSyncStatus("Syncing");
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
                SetSyncStatus("Synced");
            }
            else
            {
                SetSyncStatus("Local");
            }
        }
        catch
        {
            // Offline or unreachable server is expected; local progress remains authoritative until next sync.
            SetSyncStatus("Local");
        }

        UpdateGui(gameSettings.AreaFilled);
        Invalidate();
    }

    private void UpdateStatusLabel()
    {
        if (hud is null || gameSettings is null)
        {
            return;
        }

        hud.Update(
            gameSettings.Level,
            sessionState.CoveragePercent,
            gameSettings.BestCoveragePercent,
            gameSettings.WorldRecordCoveragePercent,
            gameSettings.CurrentStars);
    }

    private void SetSyncStatus(string status)
    {
        hud.SetSyncStatus(status);
    }

    private Task ShowStatusToastAsync(string message)
    {
        return hud.ShowToastAsync(message);
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
        SetSyncStatus("Queued");
        _ = ShowStatusToastAsync($"New best {currentCoverage:F2}%");

        if (!string.IsNullOrWhiteSpace(installId))
        {
            ScoreSubmission submission = new()
            {
                PuzzleKey = puzzleKey,
                InstallId = installId,
                CoveragePercent = currentCoverage,
                AchievedAtUtc = DateTimeOffset.UtcNow,
                PlacedFragments = snapshot.PlacedFragments
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
        _ = ShowStatusToastAsync($"Restored best {snapshot.CoveragePercent:F2}%");
    }

    private async void SettingsButton_Clicked(object sender, EventArgs e)
    {
        CancelTransientInteractions(restoreDetachedUntouchedFragment: true);
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
        if (isPageVisible)
        {
            _ = ShowStatusToastAsync(e.Value ? "Snap on" : "Snap off");
        }
    }
   
}


