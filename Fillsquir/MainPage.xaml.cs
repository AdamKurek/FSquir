#define nDebugClicking
using Fillsquir.Controls;
using Fillsquir.Interfaces;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Net.WebSockets;

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

    PointF startingPoint = new();
    Point mousePosition = new();
    Fragment moved;
    private void InitializeSquir(int dots)
    {

        // Create a 10x10 template for the pattern
        using (PictureCanvas picture = new PictureCanvas(0, 0, 0, 0))   //wtf
        {

            //    picture.StrokeColor = Colors.Silver;
            //    picture.DrawLine(0, 0, 70, 10);
            //    picture.DrawLine(0, 10, 10, 0);
            drawa = new Squir(1000, 1000);
            drawables = new DrawableStack();
            drawables.AddDrawable(drawa);
            // drawa.Resize(squir.Width, squir.Height);
            var fragmentpoints = drawa.SplitSquir();
            for (int i = 0; i < fragmentpoints.Count; i++)
            {
                var fragment = new Fragment(fragmentpoints[i], i);
                drawables.AddDrawable((Fragment)fragment);
                //GraphicsView graphicsView = new GraphicsView();
                //FragmentsGrid.Add(graphicsView, i, 0);
                //graphicsView.Drawable = fragment;
                // gest

            }

            DropGestureRecognizer dropGestureRecognizer = new DropGestureRecognizer();
            squir.GestureRecognizers.Add(dropGestureRecognizer);
            squir.Drawable = drawables;

            var panGesture = new PanGestureRecognizer();
            var pointGesture = new PointerGestureRecognizer();

            pointGesture.PointerEntered += (s, e) =>
            {
                //    var st = e.GetPosition(this);
                //    startingPoint.X = (float)st.Value.X;
                //    startingPoint.Y = (float)st.Value.Y;
                //    moved = (Fragment)drawables[1];
            };

            pointGesture.PointerMoved += (s, e) => {
                mousePosition = (Point)e.GetPosition(this);
                Console.WriteLine(mousePosition);

                // moved.position.X = startingPoint.X + (float)st.Value.X;
                // moved.position.Y = startingPoint.Y + (float)st.Value.Y;
                // squir.Invalidate();
            };


            panGesture.PanUpdated += (s, e) =>
            {
                // Handle the pan
#if DebugClicking

                drawables.AddDot(mousePosition, true);
                squir.Invalidate();
                return;
#endif
                switch (e.StatusType)
                {
                    case GestureStatus.Started:
                        moved = drawables.getNearestFragment(mousePosition);

                        //drawables.AddDrawable(
                        //    new Fragment(new PointF[]
                        //    {
                        //        new PointF(){X = (float)mousePosition.X,Y = (float)mousePosition.Y},
                        //        new PointF(){X = (float)mousePosition.X + 10,Y = (float)mousePosition.Y+ 10}
                        //    }, 1));

                        startingPoint = moved.position;
                        moved.wasTouched = true;
                        break;
                    case GestureStatus.Running:
                        if (moved == null) { return; }
                        moved.position.X = startingPoint.X + (float)e.TotalX;
                        moved.position.Y = startingPoint.Y + (float)e.TotalY;

                        squir.Invalidate();


                        // Content.TranslationX = Math.Max(Math.Min(0, x + e.TotalX), -Math.Abs(Content.Width - Application.Current.MainPage.Width));
                        // Content.TranslationY = Math.Max(Math.Min(0, y + e.TotalY), -Math.Abs(Content.Height - Application.Current.MainPage.Height));

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
                        var wtf1 = moved.VisiblePoints;
                        var wtf2 = drawables.allActivePoints(drawableindex);
                        foreach (var pt in moved.VisiblePoints)
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
                        if (min < 50)
                        {
                            moved.SetPositionToPointLocation(assignedPoint, finalIndex);
                        }
                        PointF[] ptssss = new PointF[4];
                        ptssss[0] = assignedPoint.Offset(-12, -12);
                        ptssss[1] = assignedPoint.Offset(-12, 12);
                        ptssss[2] = assignedPoint.Offset(12,12);
                        ptssss[3] = assignedPoint.Offset(-12, 12);
                        var ass = new Fragment(ptssss, i);
                        ass.wasTouched = true;
                        //drawables.AddDrawable(ass);
                        Invalidate();
                        break;
                }
                // moved = null;
            };
            squir.GestureRecognizers.Add(pointGesture);
            squir.GestureRecognizers.Add(panGesture);
            //squir.Drawable = new drawab
            //Microsoft.Maui.Graphics
            //squir.Drawable.Draw(picture, RectF.Zero);

        }
        //using(ScalingCanvas canvas =
        //new ScalingCanvas())


        //squir.Invalidate();resize
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

