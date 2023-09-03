#define nDebugVisuals

using Fillsquir.Controls;
using Fillsquir.Interfaces;
using Microsoft.Maui.Graphics;
using SkiaSharp;
using System;
using System.Runtime.ExceptionServices;
public class Fragment : GeometryElement
{

    public SKPoint[] PointsP;

    private SKPoint[] UntouchedPointsS { get {
            var Up = new SKPoint[PointsP.Length];
            for (int i = 0; i < PointsP.Length; i++)
            {
                Up[i].X = scaleToMiddleX((PointsP[i].X - MoveToFillXP) - (0.5f * sizeP.X)) * scaleX / 8 + PositionS.X;// + (0.5f * canvasWidth);// ;
                Up[i].Y = scaleToMiddleY((PointsP[i].Y - MoveToFillYP) - (0.5f * sizeP.Y)) * scaleY / 8 + PositionS.Y;// +(0.5f * canvasHeight);// ;
            }
            return Up;
        }
    }
    public SKPoint PositionS;
    public SKPoint PositionP { get
        {
            SKPoint ret = new SKPoint();
            ret.X = (PositionS.X - dravingMoveX )/ scaleX;
            ret.Y = PositionS.Y/scaleY;
            return ret;
        }
    }

    public SKPoint sizeP;
    //private SKPoint MoveToFill;
    private float MoveToFillXP;
    private float MoveToFillYP;


    public SKPoint MidpointS { get {
            SKPoint midpoint = new SKPoint();
            if (wasTouched) 
            {
                midpoint.X = PositionS.X + (sizeP.X / 2 * scaleX);
                midpoint.Y = PositionS.Y + (sizeP.Y / 2 * scaleY);
                //it's in wrong place if screen is vertical 
                //so try this instead:



                return midpoint;
            }
            midpoint.X = PositionS.X;
            midpoint.Y = PositionS.Y;

            return midpoint;

        } }

#if DebugVisuals
    public float RadiusS { get {
            return Math.Max(sizeP.X * scaleX, sizeP.Y*scaleY) / 2;
        } }
#endif
    public bool wasTouched = false;
    int index;
    int rows = 4;
    int cols = 2;
    protected override void ResizePrecize(float Width, float Height)
    {
        PositionS.X = PositionS.X * (Width / canvasWidth);
        PositionS.Y = PositionS.Y * (Height / canvasHeight);

    }

    public int IndexX { get { return index % rows; } }
    public int IndexY { get { return index - (IndexX * rows) ; } }
    float dravingMoveX => (canvasWidth - ((prop1 / prop2) * canvasWidth)) / 2;
    private float Xoffset => PositionS.X - (MoveToFillXP * scaleX);
    private float Yoffset => PositionS.Y - (MoveToFillYP *scaleY);
    private SKPoint[] visiblePointsS;
    public SKPoint[] VisiblePointsS
    {
        get
        {
            var pts = new SKPoint[PointsP.Length];
            for (int i = 0; i < PointsP.Length; i++)
            {
                pts[i] = new SKPoint((PointsP[i].X * scaleX) + Xoffset, (PointsP[i].Y * scaleY) + Yoffset);
            }
            return pts;
        }
    }

    public SKPoint[] VisiblePointsP {
            get{
                var pts = new SKPoint[PointsP.Length];
                for (int i = 0; i < PointsP.Length; i++)
                {
                    pts[i] = new SKPoint(PointsP[i].X + PositionP.X - MoveToFillXP, PointsP[i].Y + PositionP.Y - MoveToFillYP);
                }
                return pts;
            }
        }

    public Fragment(SKPoint[] Points, int index)
    {
        {
            float xMin = float.MaxValue, yMin = float.MaxValue, xMax = 0, yMax = 0;
            this.PointsP = Points;

            foreach (SKPoint point in PointsP)
            {
                if (point.X < xMin) { xMin = point.X; }
                if (point.X > xMax) { xMax = point.X; }
                if (point.Y < yMin) { yMin = point.Y; }
                if (point.Y > yMax) { yMax = point.Y; }
            }
            sizeP = new SKPoint((xMax - xMin), (yMax - yMin));

            //MoveToFill = new SKPoint() { X = xMin, Y = yMin };
            MoveToFillXP = xMin;
            MoveToFillYP = yMin;

            this.index = index;
        }
        {
            
        }
    }


    protected override void DrawMainShape(SKCanvas canvas)
    {
        SKPaint paintStroke = new()
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            IsAntialias = true,
            Color = SKColors.Blue
        };
        SKPaint paintFill = new()
        {
            Style = SKPaintStyle.Fill,
            StrokeWidth = 1,
            IsAntialias = true,
            Color = SKColors.BlueViolet
        };

        if (!wasTouched)
        {
            {
                var cellWidth = canvasWidth / rows;
                PositionS.X = (cellWidth * IndexX) + (cellWidth /2) ;
                var SQHeight = canvasHeight * (prop1 / prop2);
                var MovePerColl = (canvasHeight - SQHeight) / cols;
                var afterMove = 1/2f * MovePerColl;
                PositionS.Y = SQHeight + ((index/rows)*MovePerColl) + afterMove;
            }

            //float scaleX = (canvasWidth / (defaultCanvasWidth / prop1 * prop2));
            //float Xoffset = (canvasWidth - ((prop1 / prop2) * canvasWidth)) / 2;
           
            //for (int i = 0; i < Points.Length - 1; i++)
            //{
            //    SKPoint start = new SKPoint(Points[i].X * scaleX/8 + position.X, Points[i].Y * scaleY / 8 + position.Y);
            //    SKPoint end = new SKPoint(Points[i + 1].X * scaleX / 8 + position.X, Points[i + 1].Y * scaleY / 8 + position.Y);
            //    canvas.DrawLine(start, end);
            //}
            //SKPoint startOfLastLine = new SKPoint(Points[Points.Length - 1].X * scaleX / 8 + position.X, Points[Points.Length - 1].Y * scaleY / 8 + position.Y);
            //SKPoint endOfLastLine = new SKPoint(Points[0].X * scaleX / 8 + position.X, Points[0].Y * scaleY / 8 + position.Y);
            SKPath path = new();
            //SKPoint[] skpts = new SKPoint[];
            //for (int i = 0; i < PointsP.Length; i++)
            //{
            //    //skpts[i] = new(UntouchedPointsS[i].X * scaleX / 8 + PositionS.X, UntouchedPointsS[i].Y * scaleY / 8 + PositionS.Y);
            //    path.MoveTo(UntouchedPointsS[i].X * scaleX / 8 + PositionS.X, UntouchedPointsS[i].Y * scaleY / 8 + PositionS.Y);
            //}
            //path.Close();
            //path.MoveTo(UntouchedPointsS[0].X * scaleX / 8 + PositionS.X, UntouchedPointsS[0].Y * scaleY / 8 + PositionS.Y);
            //path.AddPoly(skpts);
            path.AddPoly(UntouchedPointsS);
            canvas.DrawPath(path, paintFill);
            canvas.DrawPath(path, paintStroke);
#if DebugVisuals
            SKPaint sKPaint = new()
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                IsAntialias = true,
                Color = SKColors.BlueViolet
            };
            canvas.DrawCircle(PositionS.X, PositionS.Y, 3, sKPaint);
#endif
            return;
        }
        {
            SKPath path = new();
            path.AddPoly(VisiblePointsS);
            canvas.DrawPath(path, paintStroke);
            canvas.DrawPath(path, paintFill);
#if DebugVisuals
            SKPaint sKPaint = new()
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                IsAntialias = true,
                Color = SKColors.BlueViolet
            };
            foreach (var pt in VisiblePointsS)
            {
                canvas.DrawCircle(pt.X, pt.Y, 5, sKPaint);
            }
            sKPaint.Color = SKColors.BurlyWood;
            canvas.DrawCircle(MidpointS.X, MidpointS.Y, RadiusS, sKPaint);
            sKPaint.Color = SKColors.DarkViolet;
            canvas.DrawRect(PositionS.X, PositionS.Y, sizeP.X * scaleX, sizeP.Y * scaleY, sKPaint);
            //sKPaint.Color = SKColors.DarkOrange;
            //canvas.DrawRectangle(new RectF() { Height = 1000 * scaleY, Width = 1000 * scaleX, X = PositionS.X, Y = PositionS.Y });
#endif
        }
        //PathF path = new PathF(Points[0]);
        //for (int i = 1; i < Points.Length; i++)
        //{
        //    path.MoveTo(Points[i]);
        //}
        // canvas.DrawPath(path);

    }

    public void SetPositionToPointLocation(SKPoint VisiblePointToAdjust, int finalIndex) {
        //position = VisiblePoints[finalIndex] - moveto;
        PositionS.X = (VisiblePointToAdjust.X - (PointsP[finalIndex].X * scaleX) + (MoveToFillXP * scaleX)) ;// - (Points[finalIndex].X * scaleX)+ Xoffset;
        PositionS.Y = VisiblePointToAdjust.Y - (PointsP[finalIndex].Y * scaleY) + (MoveToFillYP* scaleY);// - (Points[finalIndex].Y * scaleY)+ Yoffset;
            /*
            movetofillx
            position
            pt

             */
    }


    internal float Distance(SKPoint mousePosition)
    {
        return FSMath.CalculateDistance(mousePosition, MidpointS);
    }
   
    public float scaleToMiddleX(float from)
    {
            return from * canvasWidth / defaultCanvasWidth;
    }
    public float scaleToMiddleY(float from)
    {
            return from  * canvasWidth / defaultCanvasWidth;


    }
}
