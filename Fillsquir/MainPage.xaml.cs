using Fillsquir.Controls;
using SkiaScene.TouchManipulation;
using SkiaSharp;

namespace Fillsquir;

public partial class MainPage : ContentPage
{

    public MainPage()
    {
        InitializeComponent();
        InitializeSquir(1);
        
    }


    Squir drawa;
    DrawableStack drawables;
    double SquirArea; 
    SKPoint startingPoint = new();
    Microsoft.Maui.Graphics.Point mousePosition = new();
    Fragment moved;
    CommonArea commonArea = new();
    GameSettings gameSettings = new();
#if DebugClickingLines
#endif
    private void InitializeSquir(int dots)
    {
        drawa = new Squir(1000, 1000);
        SquirArea = FSMath.CalculateArea(drawa.PointsP);
        drawables = new DrawableStack();
        drawables.AddDrawable(drawa);
        // drawa.Resize(squir.Width, squir.Height);
        var fragmentpoints = drawa.SplitSquir();
        for (int i = 0; i < fragmentpoints.Count; i++)
        {
            var fragment = new Fragment(fragmentpoints[i], i);
            drawables.AddDrawable(fragment);

        }
        drawables.AddCover(commonArea); // keep it on top somehow
        drawables.Gui = new PercentageDisplay();

        //so now it's not graphicView and it's canvasView so how do i set canvas to it?
        //you don't, you set canvas to picture

        squir.PaintSurface += (s, e) =>
        {
            e.Surface.Canvas.Clear();

            drawables.Draw(e.Surface.Canvas);

        };

        var panGesture = new PanGestureRecognizer();
        var pointGesture = new PointerGestureRecognizer();

        squir.GestureRecognizers.Add(panGesture);

        panGesture.PanUpdated += PanGesture_PanUpdated; 

        var theHero = new TouchGestureRecognizer();
        theHero.OnSingleTap += (s, e) =>
        {
            //why is it never called when i tap on screen?
            //because you need to set it to some view

            var point = e.ViewPoint;
            mousePosition.X = point.X;
            mousePosition.Y = point.Y;

            (drawables.Gui as PercentageDisplay).debugString = mousePosition.ToString();

            Invalidate();
        };

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
            // nie działa
            // moved.position.X = startingPoint.X + (float)st.Value.X;
            // moved.position.Y = startingPoint.Y + (float)st.Value.Y;
            // squir.Invalidate();
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

#if DebugClicking
                bool inisde = false;
                if (FSMath.IsPointInShape(mousePosition, ((Squir)drawables[0]).VisiblePoints))
                {
                    inisde = true;
                }
                drawables.AddDot(mousePosition, inisde);
                squir.Invalidate();
                return;
#endif

            //using(ScalingCanvas canvas =
            //new ScalingCanvas())


            //squir.Invalidate();resize
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
            UpdateGui(((CommonArea)drawables.cover).Area * 100);
        }

        void UpdateGui(double percentage)
        {
            (drawables.Gui as PercentageDisplay).Percentage = percentage / SquirArea;
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

    int wtfff = 0;

    private void PanGesture_PanUpdated(object sender, PanUpdatedEventArgs e)
    {
        var d = drawables.Gui as PercentageDisplay;
        d.debugString = wtfff++.ToString();

        if (moved == null) { return; }
        if(e.StatusType != GestureStatus.Running) { return; }
        moved.PositionS.X = startingPoint.X + (float)e.TotalX;
        moved.PositionS.Y = startingPoint.Y + (float)e.TotalY;
        UpdateCover();
        Invalidate();

    }
    private void squir_Touch(object sender, SkiaSharp.Views.Maui.SKTouchEventArgs e)
        {

        var d = drawables.Gui as PercentageDisplay;
        //d.debugString = wtfff++.ToString();
        //how do i get movement of the touch
        //you need to store the starting point and then calculate the difference
        //how do i get the difference
        //like this:
        var diff = e.Location;
        //but i feel like this method is called only on touch and then it's done
        //


        switch (e.ActionType)
            {
                case SkiaSharp.Views.Maui.SKTouchAction.Pressed:
                    {
                        moved = drawables.getNearestFragment(e.Location);
                        startingPoint = moved.PositionS;
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
                                float cur = (float)DrawableStack.CalculateDistance(pt, oneOfMilion);
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
            }

    }
}
