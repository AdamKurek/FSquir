#if DebugString
// #define dbgs ((PercentageDisplay)(drawables.Gui)).debugString   
#endif
using Fillsquir.Controls;
using Microsoft.Maui.Devices.Sensors;
using SkiaSharp;

namespace Fillsquir;



public partial class MainPage : ContentPage
{

    public MainPage()
    {

        //NavigationPage.SetHasNavigationBar(this, false);
        Shell.SetNavBarIsVisible(this, false);
        
        
        InitializeComponent();

        GameSettings settings = new(0, 20, 10);
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

    bool isZooming = false;
    float zoomSum = 0f;

    static List<(float, float, float, float)> floats = new();

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
                var fragment = new Fragment(fragmentpoints[i++], c,r, gameSettings);
                gameSettings.untouchedFragments[c,r] = fragment;
                drawables.AddDrawable(fragment);
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
        var slide2FingersGesture = new PanGestureRecognizer();
        slide2FingersGesture.TouchPoints = 2;
        slide2FingersGesture.PanUpdated += (s, e) =>
        {
            //gameSettings.xoffset = (float)e.TotalX;
            //gameSettings.yoffset = (float)e.TotalY;
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    {
                        
                        break;
                    }
                case GestureStatus.Running: { break; }
                case GestureStatus.Completed: { break; }
            }

            Invalidate();
        };
        zoom.PinchUpdated += (s, e) =>
        { 
            var xd = drawables.Gui as PercentageDisplay;

            switch (e.Status)
            {
                case GestureStatus.Started:
                    isZooming = true;


                    //dlocation.X -= gameSettings.xoffset;
                    //dlocation.Y -= gameSettings.yoffset;

                    prevXOffset = gameSettings.xoffset;
                    prevYOffset = gameSettings.yoffset;
                    dlocation = new SKPoint((float)(e.ScaleOrigin.X * squir.Width), (float)(e.ScaleOrigin.Y * squir.Height));
                    dlocation.X /= gameSettings.zoomFactor;
                    dlocation.Y /= gameSettings.zoomFactor;
                    d = new SKPoint((float)(e.ScaleOrigin.X * squir.Width), (float)(e.ScaleOrigin.Y * squir.Height));
                    break;

                case GestureStatus.Running:

                    zoomSum += 1f - (float)e.Scale;
                    zoomPrev = gameSettings.zoomFactor;
                    gameSettings.zoomFactor *= (float)e.Scale;
                    gameSettings.xoffset = -dlocation.X + prevXOffset + (d.X / gameSettings.zoomFactor);
                    gameSettings.yoffset = -dlocation.Y + prevYOffset + (d.Y / gameSettings.zoomFactor);

                    //gameSettings.xoffset = -location.X + gameSettings.xoffset + (e.Location.X / gameSettings.zoomFactor);

                    // floats.Add((-d.X , gameSettings.xoffset , dlocation.X , gameSettings.zoomFactor));
                    //gameSettings.yoffset = -d.Y + gameSettings.yoffset + (dlocation.Y / gameSettings.zoomFactor);

                    ((PercentageDisplay)(drawables.Gui)).debugString = e.Scale.ToString();

                    drawables.AddDot(dlocation);
                    drawables.AddDot(d,true);
                    break;

                case GestureStatus.Completed:
                    isZooming = false;
                    zoomSum = 0f;
                    break;

            }
            Invalidate();
        };
        squir.GestureRecognizers.Add(panGesture);
        grid.GestureRecognizers.Add(zoom);
        grid.GestureRecognizers.Add(slide2FingersGesture);
        grid.GestureRecognizers.Add(pointGesture);
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
        //location.Offset(+gameSettings.xoffset);

        SKPoint location =new((float)e.TotalX, (float)e.TotalY);


      
        location.X /= gameSettings.zoomFactor;
        location.Y /= gameSettings.zoomFactor;
       // location.X += gameSettings.xoffset;
       // location.Y += gameSettings.yoffset;

        if (moved == null) {
            if (movingBottomStrip)
            {
                if (e.TotalX == 0) { return; }
                var pos = bottomStripMovePre - (float)e.TotalX;
                if (pos <= 0)
                {
                    pos = 0;
                }
                else
                {
                    var TotalStripLenth = ((float)gameSettings.Rows/(float)gameSettings.VisibleRows) * (float)squir.Width - (float)squir.Width;
                    if (TotalStripLenth <= pos)
                    {
                        pos = TotalStripLenth;
                    }
                }
                gameSettings.bottomStripMove = pos;
                Invalidate();
                return;
            }
            if (movingMap)
            {

                return;
            }
            return;
        }
        if(e.StatusType != GestureStatus.Running) { return; }
        moved.PositionS.X = startingPoint.X + location.X;
        moved.PositionS.Y = startingPoint.Y + location.Y;
        UpdateCover();
        Invalidate();

    }

    SKPoint offsetMoveLocation;
    bool movingBottomStrip = false;
    bool movingMap = false;

    float zoomPrev = 1;
    SKPoint zoomPos;
    float prevXOffset;
    float prevYOffset;
    private void squir_Touch(object sender, SkiaSharp.Views.Maui.SKTouchEventArgs e)
        {
        TapPosition = e.Location;
        var location = e.Location;
        if (location.Y > squir.Height * gameSettings.prop1 / gameSettings.prop2&& e.ActionType == SkiaSharp.Views.Maui.SKTouchAction.Pressed)
        {
            if(e.MouseButton == SkiaSharp.Views.Maui.SKMouseButton.Left)
            {
                (int,int) selectedCell = FindSlotOnBottomStrip(e.Location);

                moved = gameSettings.untouchedFragments[selectedCell.Item1,selectedCell.Item2];
                gameSettings.untouchedFragments[selectedCell.Item1, selectedCell.Item2] = null;
#if DebugString
                //((PercentageDisplay)(drawables.Gui)).debugString = selectedCell.ToString();
#endif
                if (moved != null)
                {
                    moved.wasTouched = true;
                    location.X /= gameSettings.zoomFactor;
                    location.Y /= gameSettings.zoomFactor;
                    location.X -= gameSettings.xoffset;
                    location.Y -= gameSettings.yoffset;
                    startingPoint = location;
                    return;
                }
                else
                {
                    ;
                }
            }
            else if (e.MouseButton == SkiaSharp.Views.Maui.SKMouseButton.Middle)
            {
              
                movingBottomStrip = true; movingMap = false;
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
                        offsetMoveLocation.X = location.X - gameSettings.xoffset;
                        offsetMoveLocation.Y = location.Y - gameSettings.yoffset;
                        movingBottomStrip = false;
                        movingMap = true;
                        break;
                    }

                    moved = drawables.SelectFragmentOnClick(location);
                    if(moved == null) {
                        bottomStripMovePre = gameSettings.bottomStripMove;
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
                    if (e.MouseButton == SkiaSharp.Views.Maui.SKMouseButton.Middle)
                    {
                        if (movingMap) { 
                            gameSettings.xoffset = location.X - offsetMoveLocation.X;
                            gameSettings.yoffset = location.Y - offsetMoveLocation.Y;
                            Invalidate();
                            break;
                        }

                    }
                    if (movingBottomStrip)
                    {
                        ;
                    }
                    break;
                }
            }
        
        Invalidate();
    }

    private void zoomTo(float zoomVal, SKPoint OnMapLocation, SKPoint OnScreenLocation, float previousZoom)
    {
            gameSettings.zoomFactor *= zoomVal;
        if (gameSettings.zoomFactor > 1.5) { gameSettings.zoomFactor = 1.5f; }
        if (gameSettings.zoomFactor < 0.5) { gameSettings.zoomFactor = 0.5f; }

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
