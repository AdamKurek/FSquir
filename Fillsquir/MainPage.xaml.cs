using Fillsquir.Controls;
using SkiaSharp;

namespace Fillsquir;

public partial class MainPage : ContentPage
{

    public MainPage()
    {

        //NavigationPage.SetHasNavigationBar(this, false);
        Shell.SetNavBarIsVisible(this, false);
        
        
        InitializeComponent();

        GameSettings settings = new(0, 10, 10);
        InitializeSquir(settings);
        
    }


    Squir drawa;
    DrawableStack drawables;
    double SquirArea; 
    SKPoint startingPoint = new();
    Microsoft.Maui.Graphics.Point mousePosition = new();
    Fragment moved;
    GameSettings gameSettings;
    CommonArea commonArea;
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
        for (int i = 0; i < fragmentpoints.Count; i++)
        {
            var fragment = new Fragment(fragmentpoints[i], i, gameSettings);
            drawables.AddDrawable(fragment);
        }
        drawables.AddCover(commonArea); 
        drawables.Gui = new PercentageDisplay(gameSettings);


        squir.PaintSurface += (s, e) =>
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear();
            canvas.Scale(gameSettings.zoomFactor);
            canvas.Translate(gameSettings.xoffset, gameSettings.yoffset);
            drawables.DrawPreZoom(canvas);
            canvas.ResetMatrix();
            drawables.DrawPastZoom(canvas);
            canvas.Scale(gameSettings.zoomFactor);
            canvas.Translate(gameSettings.xoffset, gameSettings.yoffset);
            drawables.DrawFragmentsoutlines(canvas);
            // Draw remaining contents
            // Assuming drawables.DrawNonZoomed() draws the remaining contents
            // drawables.DrawNonZoomed(canvas);

        };

        var panGesture = new PanGestureRecognizer();
        var pointGesture = new PointerGestureRecognizer();
        var zoom = new PinchGestureRecognizer();
        zoom.PinchUpdated += (s, e) =>
        { 
        var d = e.ScaleOrigin;
            
            switch (e.Status)
            {
                case GestureStatus.Started:
                    break;
                case GestureStatus.Running:
                    gameSettings.zoomFactor *= (float)e.Scale;
                    break;
                case GestureStatus.Completed:
                    break;
            }
            squir.InvalidateSurface();
        };
        squir.GestureRecognizers.Add(panGesture);
        squir.GestureRecognizers.Add(zoom);
        //why it never triggers zoom.PinchUpdated
        //because you need to add it to view
        panGesture.PanUpdated += PanGesture_PanUpdated; 
        //is it how i add it to view?
        // squir.GestureRecognizers.Add(theHero);
        //no, you can't do it cause theHero doesn't implement IGestureRecognizer interface  
        //so how do i add it to view?
        //you don't, you add it to canvas like this:

        //squir.CanvasView.GestureRecognizers.Add(theHero);
        squir.EnableTouchEvents = true;
        //how do i trigger tapped event?
        //recoginizer.Tapped +=
        //no, i want to trigger event of this recognizer from other function, can i do it?
        //yes, i can, i just need to call recoginizer.SendTapped

        //recoginizer.NumberOfTapsRequired = 1;
        //recoginizer.Tapped += (s, e) =>
        //{
        //    var point = e.GetPosition(((View)s));
        //    (drawables[0] as Squir).PointsP[0].X = (float)point.Value.X;
        //    (drawables[0] as Squir).PointsP[0].Y = (float)point.Value.Y;

        //    //fix syntaxof 2 lines below
        //    mousePosition.X = point.Value.X;
        //    mousePosition.Y = point.Value.Y;
        //};
        // var rr= new Gesturerecognizer();
        //how do i get position where user taps on phone

        pointGesture.PointerEntered += (s, e) =>
        {

            //fix syntaxof 2 lines below
            // mousePosition.X = point.Value

            // mousePosition.X = point.X;
            // mousePosition.Y = point.Y;
            //    var st = e.GetPosition(this);
            //    startingPoint.X = (float)st.Value.X;
            //    startingPoint.Y = (float)st.Value.Y;
            //    moved = (Fragment)drawables[1];
        };

        pointGesture.PointerMoved += (s, e) => {

            //i want it to work only on windows what directive do i use 
            //use 

#if windows
            mousePosition = (Point)e.GetPosition(this);
#endif
#if DebugString
            //(drawables.Gui as PercentageDisplay).debugString = mousePosition.ToString();
#endif


            return;
        };




#if DebugString
            //(drawables.Gui as PercentageDisplay).debugString = mousePosition.ToString();
#endif


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


#if DebugString
        var d = drawables.Gui as PercentageDisplay;
        //d.debugString = wtfff++.ToString();
#endif

        if (moved == null) {
            if (e.TotalX == 0) { return; }
            gameSettings.bottomStripMove = bottomStripMovePre + (float)e.TotalX;
            return; 
        }
        if(e.StatusType != GestureStatus.Running) { return; }
        moved.PositionS.X = startingPoint.X + location.X;
        moved.PositionS.Y = startingPoint.Y + location.Y;
        UpdateCover();
        Invalidate();

    }

    SKPoint offsetMoveLocation;
    private void squir_Touch(object sender, SkiaSharp.Views.Maui.SKTouchEventArgs e)
        {
        var d = drawables.Gui as PercentageDisplay;
        //d.debugString = wtfff++.ToString();
        var location = e.Location;
        location.X /= gameSettings.zoomFactor;
        location.Y /= gameSettings.zoomFactor;
        //location.Offset(+gameSettings.xoffset);
        location.X -= gameSettings.xoffset;
        location.Y -= gameSettings.yoffset;
        var diff = e.Location;
        //but i feel like this method is called only on touch and then it's done
        //

#if DebugClicking
        bool inisde = false;
        SKPoint mp = new SKPoint() { X = location.X, Y = location.Y };
        if (FSMath.IsPointInShape(mp, ((Squir)drawables[0]).VisiblePoints))
        {
            inisde = true;
        }
        drawables.AddDot(mp, inisde);
        Invalidate();
#endif





        switch (e.ActionType)
        {
            case SkiaSharp.Views.Maui.SKTouchAction.Pressed:
                {
                    if (e.MouseButton == SkiaSharp.Views.Maui.SKMouseButton.Middle)
                    {
                        offsetMoveLocation = location;
                        bottomStripMovePre = gameSettings.bottomStripMove;
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

                    var zoomPrev = gameSettings.zoomFactor;
                    squir.AnchorX = location.X;
                    if (e.WheelDelta > 0)
                    {
                        gameSettings.zoomFactor += 0.25f;
                    }
                    else 
                    {
                        if (gameSettings.zoomFactor <= 0.5f)
                        {
                            return;
                        }
                        gameSettings.zoomFactor -= 0.25f;
                    }
                    var zoomprop = gameSettings.zoomFactor / zoomPrev;
                    var xfromhere = -gameSettings.xoffset + (e.Location.X / zoomPrev);
                    var yfromhere = -gameSettings.yoffset + (e.Location.Y / zoomPrev);
                    gameSettings.xoffset = -location.X + (e.Location.X / gameSettings.zoomFactor);
                    gameSettings.yoffset = -location.Y + (e.Location.Y / gameSettings.zoomFactor);


                    


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
                        gameSettings.xoffset += location.X - offsetMoveLocation.X;
                        gameSettings.yoffset += location.Y - offsetMoveLocation.Y;
                        Invalidate();
                        break;
                    }
                    break;
                }
            }
        

    }
    bool wtf = true;
    int wtfint = 0;
}
