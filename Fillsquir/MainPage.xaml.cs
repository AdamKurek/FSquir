#define nDebugClickingLines
using Fillsquir.Controls;

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
    PointF startingPoint = new();
    Point mousePosition = new();
    Fragment moved;
    CommonArea commonArea = new();
    GameSettings gameSettings = new();
#if DebugClickingLines
#endif
    private void InitializeSquir(int dots)
    {
        using (PictureCanvas picture = new PictureCanvas(0, 0, 0, 0))   //wtf
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
                drawables.AddDrawable((Fragment)fragment);

            }
            drawables.AddCover(commonArea); // keep it on top somehow
            drawables.Gui = new PercentageDisplay();
            DropGestureRecognizer dropGestureRecognizer = new DropGestureRecognizer();
            squir.GestureRecognizers.Add(dropGestureRecognizer);
            squir.Drawable = drawables;

            var panGesture = new PanGestureRecognizer();
            var pointGesture = new PointerGestureRecognizer();
            //i need recognizer that will give me position from where user taps on phone
            var recoginizer = new TapGestureRecognizer();
            //how do i trigger tapped event?
            //recoginizer.Tapped +=
            //no, i want to trigger event of this recognizer from other function, can i do it?
            //yes, i can, i just need to call recoginizer.SendTapped

            recoginizer.Tapped += (s, e) =>
            {
                //can i somehow make it so i get coordinates even if i hold finger on screen?
                //yes , i can, but i need to use pan gesture recognizer
                //but then i need to get coordinates using pan gesture recognizer
                //but pan gesture recognizer gives me movement of the finger, not coordinates of the finger
                //so use some library like xamarin.forms that is called 

                var point = e.GetPosition(((View)s));

                //fix syntaxof 2 lines below
                mousePosition.X = point.Value.X;
                mousePosition.Y = point.Value.Y;
                Invalidate();
            };  

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
                (drawables.Gui as PercentageDisplay).debugString = mousePosition.ToString();
#endif


                return;
                // nie działa
                // moved.position.X = startingPoint.X + (float)st.Value.X;
                // moved.position.Y = startingPoint.Y + (float)st.Value.Y;
                // squir.Invalidate();
            };
            
            panGesture.PanUpdated += (s, e) =>
            {
                
                //how do i get position where user taps on phone

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
                switch (e.StatusType)
                {
                    case GestureStatus.Started:
                        
                        moved = drawables.getNearestFragment(mousePosition);

                        startingPoint = moved.PositionS;
                        moved.wasTouched = true;
                        break;
                    case GestureStatus.Running:
                        if (moved == null) { return; }
                        moved.PositionS.X = startingPoint.X + (float)e.TotalX;
                        moved.PositionS.Y = startingPoint.Y + (float)e.TotalY;
                        UpdateCover();
                        //UpdateGui(50.0f);
                        squir.Invalidate();
                        break;

                    case GestureStatus.Completed:
                        if (moved == null) { return; }
                        if (!moved.wasTouched) { return; }
                        float min = float.MaxValue;
                        int i = 0, finalIndex = 0;
                        PointF assignedPoint = new();
                        int drawableindex = 0;
                        for (;drawableindex<drawables.drawables.Count;drawableindex++)
                        {
                            if (Object.ReferenceEquals(drawables[drawableindex], moved))
                            {
                                break;
                            }
                        }
                        var wtf1 = moved.VisiblePointsS;
                        var wtf2 = drawables.allActivePoints(drawableindex);
                        foreach (var pt in moved.VisiblePointsS)
                        {
                            foreach (var oneOfMilion in drawables.allActivePoints(drawableindex)) {
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
                        UpdateCover();
                        //UpdateGui();
                        Invalidate();
                        break;
                }
            };
            squir.GestureRecognizers.Add(pointGesture);
            squir.GestureRecognizers.Add(panGesture);
            squir.GestureRecognizers.Add(recoginizer);
            //squir.Drawable = new drawab
            //Microsoft.Maui.Graphics
            //squir.Drawable.Draw(picture, RectF.Zero);

        }
        //using(ScalingCanvas canvas =
        //new ScalingCanvas())


        //squir.Invalidate();resize
    }
    void UpdateCover()                  
    {
        var FiguresAsPointlists = new List<PointF[]>();
        foreach (var a in drawables.drawables.Skip(1))
        {
            FiguresAsPointlists.Add(((Fragment)a).VisiblePointsP);
        }

        //var u1 = ((Fragment)drawables.drawables[1]).VisiblePointsP;
        //var u2 = ((Squir)drawables[0]).PointsP;
        commonArea.FiguresP = FSMath.CommonArea(((Squir)drawables[0]).PointsP, FiguresAsPointlists);
        UpdateGui(((CommonArea)drawables.cover).Area*100);
    }

    void UpdateGui(double percentage)
    {
        (drawables.Gui as PercentageDisplay).Percentage = percentage/SquirArea;
    }

    void squir_DragInteraction(object sender, TouchEventArgs e)
    {
        //e.IsInsideBounds = true;xd
        //Console.WriteLine(e.ToString());
    }

    void squir_SizeChanged(object sender, EventArgs e)
    {
        if (drawa != null)
        {
            //drawa.Resize(squir.Width, squir.Height);
            drawables.Resize((float)squir.Width, (float)squir.Height);
        }
        drawables.cover.Resize((float)squir.Width, (float)squir.Height);
        drawables.Gui.Resize((float)squir.Width, (float)squir.Height);
    }



    double wr, hr;
    void DragGestureRecognizer_DragStarting(object sender, DragStartingEventArgs e)
    {
        var dragged = (sender as DragGestureRecognizer).Parent as GraphicsView;
        var wtr = dragged.Drawable as Fragment;

        //wr = dragged.WidthRequest;
        //hr = dragged.HeightRequest;


        //dragged.WidthRequest = squir.Width;
        //dragged.HeightRequest = squir.Height;
        //dragged.Frame.Inflate(1000, 1000);
        //dragged.ScaleY = 3;
        ////wtr.Resize(squir.Width, squir.Height);
        //PointF[] ps= new PointF[4] {
        //new PointF(1000,10),
        //new PointF(100,100),
        //new PointF(30,40),
        //new PointF(30,10),

        //};
        //var largerDrawable = new Fragment(ps); // This method should return a new larger Drawable
        //dragged.Drawable = largerDrawable;
        //dragged.Invalidate();

        //e.Data.Properties.Add("Text", label.Text);
        e.Data.Properties.Add("val", dragged);
        //dragged.WidthRequest = wr;
        //dragged.HeightRequest = hr;


    }

    void GestureRecognizer_DropCompleted(object sender, DropCompletedEventArgs e)
    {
        var dragged = (sender as DragGestureRecognizer).Parent as GraphicsView;
        dragged.WidthRequest = wr;
        dragged.HeightRequest = hr;
    }

    void Invalidate()
    {
        squir.Invalidate();
    }
    void DropGestureRecognizer_Drop(object sender, DropEventArgs e)
    {
        var points = e.Data.Properties["val"] as GraphicsView;
        var frame = (sender as Element)?.Parent as GraphicsView;

        if (points?.Drawable is Fragment fragment)
        {
            //(frame.Drawable as DrawableStack).AddDrawable(new Fragment(((Fragment)points.Drawable).Points)); // Now the frame's Drawable is replaced with the fragment from points.
        }

        frame.Invalidate();

    }
}

