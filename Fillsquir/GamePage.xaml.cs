using Fillsquir.Controls;
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
    GameSettings settings;
    public GamePage()
    {
        BindingContext = new GamePageViewModel();

        var navigationState = Shell.Current.CurrentState;
        int xd = 100;

        //NavigationPage.SetHasNavigationBar(this, false);
        Shell.SetNavBarIsVisible(this, false);
        InitializeComponent();
        try { 
        }catch(Exception ex) { }
    }
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        int level = int.Parse(query["Level"].ToString());
        settings = new(0, level);
        InitializeSquir(settings);
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

        pointGesture.PointerEntered += (s, e) =>
        {
            var wtwd =  e.GetPosition(grid);
            
            drawables.AddDot(new SKPoint((float)wtwd.Value.X, (float)wtwd.Value.Y));

        };

        pointGesture.PointerMoved += (s, e) => {

#if windows
            mousePosition = (Point)e.GetPosition(this);
#endif


            return;
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
        }


        void Invalidate()
        {
            squir.InvalidateSurface();
        }

        private void squir_SizeChanged(object sender, EventArgs e)
        {
            if (drawa != null)
            {
                //drawa.Resize(squir.Width, squir.Height);
                drawables.Resize((float)squir.Width, (float)squir.Height);
            }
            drawables.cover.Resize((float)squir.Width, (float)squir.Height);
            drawables.Gui.Resize((float)squir.Width, (float)squir.Height);
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
                            drawable => (((drawable.PositionP.Y * (squir.Height / 1000))) + (gameSettings.yoffset)) < (squir.Width / gameSettings.zoomFactor));
                    }
                    if (yMoveTotal > 0)
                    {
                        GameSettings.MoveFragmentsBetweenLists(gameSettings.TooTopFragments, gameSettings.CenterFragments,
                            drawable => (((drawable.PositionP.Y + drawable.sizeP.Y) * (squir.Height / 1000)) + gameSettings.yoffset > absolute0y));
                        GameSettings.MoveFragmentsBetweenLists(gameSettings.CenterFragments, gameSettings.TooTopFragments,
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
                        return;
                    }
                    moved.PositionS.X = startingPoint.X + location.X;
                    moved.PositionS.Y = startingPoint.Y + location.Y;
                    UpdateCover();
                    break;
                }
            case moveStatus.bottomStrip: 
                {
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
                (int,int) selectedCell = FindSlotOnBottomStrip(e.Location);
                moved = gameSettings.untouchedFragments[selectedCell.Item1,selectedCell.Item2];
                movingStatus = moveStatus.fragment;
                gameSettings.untouchedFragments[selectedCell.Item1, selectedCell.Item2] = null;
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
                        bottomStripMovePre = gameSettings.bottomStripMove;
                        return;
                    }
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
                movingStatus = moveStatus.bottomStrip;
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

#else
                        StartMovingMap();
                    
#endif
                        return;
                    }//probably will be needed one day
                    if(moved.wasTouched) {
                        startingPoint = moved.PositionS;
                    }
                    else
                    {
                        startingPoint = location;
                                        //here add offset
                    }
                    //TouchFragment(moved);
                    movingStatus = moveStatus.fragment;
                    break;
                }
            case SkiaSharp.Views.Maui.SKTouchAction.Released:
                {
                    if (moved == null) { return; }
                    if (!moved.wasTouched) { return; }
                    if (TryGetWallSnapTranslation(moved, out var snapTranslation))
                    {
                        moved.PositionS = new SKPoint(
                            moved.PositionS.X + snapTranslation.X,
                            moved.PositionS.Y + snapTranslation.Y);
                    }
                    moved = null;
                    movingStatus = moveStatus.none;
                    UpdateCover();
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
        ff.wasTouched = true;
        gameSettings.CenterFragments.Add(ff);
    }
   
}
