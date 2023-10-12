using Fillsquir.Controls;
using SkiaSharp;

namespace Fillsquir;

public partial class GamePage : ContentPage, IQueryAttributable
{
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
        settings = new(0, level, level+4);
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
        commonArea = new(gameSettings);
        drawa = new Squir(1000, 1000, gameSettings);
        gameSettings.MaxArea = FSMath.CalculateArea(drawa.PointsP);

        var fragmentpoints = drawa.SplitSquir();
        drawables = new DrawableStack(gameSettings);
        drawables.AddDrawable(drawa);

        for (int r = 0, i = 0; i < gameSettings.fragments; r++) 
        {
            for(int c = 0; c < gameSettings.Rows; c++)
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
            var FiguresAsPointlists = new List<SKPoint[]>();
            foreach (var a in drawables.drawables.Skip(1))
            {
                FiguresAsPointlists.Add(((Fragment)a).VisiblePointsP);
            }
            //var u1 = ((Fragment)drawables.drawables[1]).VisiblePointsP;
            //var u2 = ((Squir)drawables[0]).PointsP;
            commonArea.FiguresP = FSMath.CommonArea(((Squir)drawables[0]).PointsP, FiguresAsPointlists);
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
                        moved.wasTouched = true;
                        goto case moveStatus.fragment;
                    }
                    break;
                }
            case moveStatus.map:
                {
                    gameSettings.xoffset = location.X + offsetMoveLocation.X;
                    gameSettings.yoffset = location.Y + offsetMoveLocation.Y;
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
                        var TotalStripLenth = ((float)gameSettings.Rows / (float)gameSettings.VisibleRows) * (float)squir.Width - (float)squir.Width;
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
                    moved.wasTouched = true;
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
                    moved.wasTouched = true;
                    movingStatus = moveStatus.fragment;
                    break;
                }
            case SkiaSharp.Views.Maui.SKTouchAction.Released:
                {
                    if (moved == null) { return; }
                    if (!moved.wasTouched) { return; }
                    float min = float.MaxValue;
                    int i = 0, finalIndex = 0;
                    SKPoint assignedPoint = new();
                    int drawableindex = 0;
                    for (; drawableindex < drawables.drawables.Count; drawableindex++)
                    {
                        if (Object.ReferenceEquals(drawables[drawableindex], moved))
                        {
                            break;
                        }
                    }
                    foreach (var pt in moved.VisiblePointsS)
                    {
                        foreach (var oneOfMilion in drawables.allActivePoints(drawableindex))
                        {
                            float cur = FSMath.CalculateDistance(pt, oneOfMilion);
                            if (cur < min)
                            {
                                min = cur;
                                finalIndex = i;
                                assignedPoint = oneOfMilion;
                            }
                        }
                        i++;
                    }
                    if (min < 10)
                    {
                        moved.SetPositionToPointLocation(assignedPoint, finalIndex);
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

    private void StartMovingMap()
    {
        offsetMoveLocation.X =  gameSettings.xoffset;
        offsetMoveLocation.Y =  gameSettings.yoffset;
        movingStatus = moveStatus.map;
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
        selectedCell.Item2 = (int)(onStripLocation.Y / bottomStripHeight * gameSettings.Cols);
        selectedCell.Item1 = (int)((onStripLocation.X + gameSettings.bottomStripMove) / ((float)squir.Width / gameSettings.VisibleRows));
        return selectedCell;
    }

   
}
