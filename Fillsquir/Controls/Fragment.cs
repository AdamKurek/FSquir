#define DebugVisuals

using Fillsquir.Controls;
using Fillsquir.Interfaces;
using Microsoft.Maui.Graphics;
using System;
using System.Runtime.ExceptionServices;
public class Fragment : GeometryElement
{

    public PointF[] Points;

    private readonly PointF[] UntouchedPoints;
    public PointF position;
    public PointF size;
    //private PointF MoveToFill;
    private float MoveToFillX;
    private float MoveToFillY;


    public PointF Midpoint { get {
            PointF midpoint = new PointF();
            if (wasTouched) 
            {
                midpoint.X = position.X + (size.X / 2);
                midpoint.Y = position.Y + (size.Y / 4);
                return midpoint;
            }
            midpoint.X = position.X + canvasWidth * scaleX / 8;
            midpoint.Y = position.Y + canvasHeight * scaleY / 8;

            return midpoint;

        } }
    public float Radius { get {
            return Math.Max(size.X, size.Y) / 2;
        } }

    public bool wasTouched = false;
    int index;
    int rows = 4;

    protected override void ResizePrecize(float Width, float Height)
    {
        position.X = position.X * (Width / canvasWidth);
        position.Y = position.Y * ( Height / canvasHeight);

    }

    public int IndexX { get { return index % rows; } }
    public int IndexY { get { return index - (IndexX * rows) ; } }

    private float Xoffset => position.X - MoveToFillX;
    private float Yoffset => position.Y - (MoveToFillY / 2);
    private PointF[] visiblePoints;
    public PointF[] VisiblePoints
    {
        get
        {
            var pts = new PointF[Points.Length];
            for (int i = 0; i < Points.Length; i++)
            {
                pts[i] = new PointF((Points[i].X * scaleX) + Xoffset, (Points[i].Y * scaleY) + Yoffset);
            }
            return pts;
        }
    }
    

    public Fragment(PointF[] Points, int index)
    {
        {
            float xMin = float.MaxValue, yMin = float.MaxValue, xMax = 0, yMax = 0;
            this.Points = Points;

            foreach (PointF point in Points)
            {
                if (point.X < xMin) { xMin = point.X; }
                if (point.X > xMax) { xMax = point.X; }
                if (point.Y < yMin) { yMin = point.Y; }
                if (point.Y > yMax) { yMax = point.Y; }
            }
            size = new PointF((xMax - xMin), (yMax - yMin));

            //MoveToFill = new PointF() { X = xMin, Y = yMin };
            MoveToFillX = xMin;
            MoveToFillY = yMin;

            this.index = index;
        }

        {
            UntouchedPoints = new PointF[Points.Length];
            for (int i = 0; i < Points.Length; i++)
            {
                UntouchedPoints[i].X = scaleToMiddleX((Points[i].X - MoveToFillX) + (2000) - (0.5f * size.X));// + (0.5f * canvasWidth);// ;
                UntouchedPoints[i].Y = scaleToMiddleY((Points[i].Y - MoveToFillY) + (1000) - (0.5f * size.Y)) ;// +(0.5f * canvasHeight);// ;
            }
        }
    }


    protected override void DrawMainShape(ICanvas canvas, RectF dirtyRect)
    {
        float scale = prop1 / prop2;
        //todo draw using path not lines
        if (!wasTouched)
        {

            {
                position.X = canvasWidth * IndexX / 4;
                position.Y = canvasHeight * (prop1 / prop2);
                position.Y += canvasWidth * scaleY / 8 * (index / 4);
            }

            //float scaleX = (canvasWidth / (defaultCanvasWidth / prop1 * prop2));
            //float Xoffset = (canvasWidth - ((prop1 / prop2) * canvasWidth)) / 2;
            canvas.StrokeColor = Colors.Aqua;
            canvas.FillColor = Colors.Aqua;

            //for (int i = 0; i < Points.Length - 1; i++)
            //{
            //    PointF start = new PointF(Points[i].X * scaleX/8 + position.X, Points[i].Y * scaleY / 8 + position.Y);
            //    PointF end = new PointF(Points[i + 1].X * scaleX / 8 + position.X, Points[i + 1].Y * scaleY / 8 + position.Y);
            //    canvas.DrawLine(start, end);
            //}
            //PointF startOfLastLine = new PointF(Points[Points.Length - 1].X * scaleX / 8 + position.X, Points[Points.Length - 1].Y * scaleY / 8 + position.Y);
            //PointF endOfLastLine = new PointF(Points[0].X * scaleX / 8 + position.X, Points[0].Y * scaleY / 8 + position.Y);
            for (int i = 0; i < Points.Length - 1; i++)
            {
                PointF start = new PointF(UntouchedPoints[i].X * scaleX / 8 + position.X, UntouchedPoints[i].Y * scaleY / 8 + position.Y);
                PointF end = new PointF(UntouchedPoints[i + 1].X * scaleX / 8 + position.X, UntouchedPoints[i + 1].Y * scaleY / 8 + position.Y);
                canvas.DrawLine(start, end);
            }
            PointF startOfLastLine = new PointF(UntouchedPoints[Points.Length - 1].X * scaleX / 8 + position.X, UntouchedPoints[Points.Length - 1].Y * scaleY / 8 + position.Y);
            PointF endOfLastLine = new PointF(UntouchedPoints[0].X * scaleX / 8 + position.X, UntouchedPoints[0].Y * scaleY / 8 + position.Y);
            canvas.DrawLine(startOfLastLine, endOfLastLine);

#if DebugVisuals
            canvas.DrawCircle(Midpoint.X, Midpoint.Y, 0.1f * Radius * (scaleX > scaleY ? scaleX : scaleY));//clickbox
#endif
            return;
        }
        {
            canvas.StrokeColor = Colors.Aqua;
            canvas.FillColor = Colors.Aqua;
            var VP = VisiblePoints;
            PathF path = new();

            for (int i = 0; i < Points.Length - 1; i++)
            {
                path.LineTo(VP[i]);
            }
            canvas.FillPath(path);
#if DebugVisuals
            canvas.DrawCircle(Midpoint.X, Midpoint.Y, Radius* (scaleX>scaleY? scaleX:scaleY));//clickbox
            canvas.DrawRectangle(new RectF() { Height = size.Y*scaleY, Width = size.X* scaleX, X =position.X ,Y= position.Y});
            canvas.StrokeColor = Colors.Red;
            canvas.FillColor = Colors.Red;
            canvas.DrawRectangle(new RectF() { Height = 1000 * scaleY, Width = 1000 * scaleX, X = position.X, Y = position.Y });
#endif
        }
        //PathF path = new PathF(Points[0]);
        //for (int i = 1; i < Points.Length; i++)
        //{
        //    path.MoveTo(Points[i]);
        //}
        // canvas.DrawPath(path);

    }

    public void SetPositionToPointLocation(PointF VisiblePointToAdjust, int finalIndex) {
        //position = VisiblePoints[finalIndex] - moveto;
        position.X = VisiblePointToAdjust.X - (Points[finalIndex].X *scaleX) + MoveToFillX;// - (Points[finalIndex].X * scaleX)+ Xoffset;
        position.Y = VisiblePointToAdjust.Y - (Points[finalIndex].Y *scaleY) + (MoveToFillY/2);// - (Points[finalIndex].Y * scaleY)+ Yoffset;
            /*
            movetofillx
            position
            pt

             */
    }


    internal float Distance(Point mousePosition)
    {
        PointF mouse = new Point() {X = mousePosition.X,Y = mousePosition.Y };
        return (float)DrawableStack.CalculateDistance(mouse, Midpoint);
    }
   
    public float scaleToMiddleX(float from)
    {
            return from * scaleX;
    }
    public float scaleToMiddleY(float from)
    {
            return from  * scaleY;


    }
}
