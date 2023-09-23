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
                return midpoint;
            }
            midpoint.X = PositionS.X;
            midpoint.Y = PositionS.Y;

            return midpoint;

        } }

    public SKPoint Centroid { get
        {
            if(wasTouched)
            {
                return FSMath.Centroid(VisiblePointsS);
            }
            return MidpointS;//noone cares it's a bug

        } 
    }

#if DebugVisuals
    public float RadiusS { get {
            return Math.Max(sizeP.X * scaleX, sizeP.Y*scaleY) / 2;
        } }
#endif
    public bool wasTouched = false;
    protected override void ResizePrecize(float Width, float Height)
    {
        PositionS.X = PositionS.X * (Width / canvasWidth);
        PositionS.Y = PositionS.Y * (Height / canvasHeight);

    }

    public int IndexX;
    public int IndexY;
    float dravingMoveX => (canvasWidth - ((gameSettings.prop1 / gameSettings.prop2) * canvasWidth)) / 2;
    private float Xoffset => PositionS.X - (MoveToFillXP * scaleX);
    private float Yoffset => PositionS.Y - (MoveToFillYP *scaleY);
    public SKPoint[] VisiblePointsS
    {
        get
        {
            if (!wasTouched)
            {
                return UntouchedPointsS;
            }
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
    internal Fragment(SKPoint[] Points, int indexX, int indexY, GameSettings settings) : base(settings)
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
            IndexX = indexX;
            IndexY = indexY;
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
                var cellWidth = canvasWidth / gameSettings.VisibleRows;
                PositionS.X = (cellWidth * IndexX) + (cellWidth /2) - gameSettings.bottomStripMove;
                var SQHeight = canvasHeight * (gameSettings.prop1 / gameSettings.prop2);
                var MovePerColl = (canvasHeight - SQHeight) / gameSettings.Cols;
                var afterMove = 1/2f * MovePerColl;
                PositionS.Y = SQHeight + (IndexY*MovePerColl) + afterMove;
            }

            SKPath path = new();
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
            sKPaint.Color = SKColors.IndianRed;
            canvas.DrawCircle(Centroid.X, Centroid.Y, RadiusS, sKPaint);
            sKPaint.Color = SKColors.DarkViolet;
            canvas.DrawRect(PositionS.X, PositionS.Y, sizeP.X * scaleX, sizeP.Y * scaleY, sKPaint);
            //sKPaint.Color = SKColors.DarkOrange;
            //canvas.DrawRectangle(new RectF() { Height = 1000 * scaleY, Width = 1000 * scaleX, X = PositionS.X, Y = PositionS.Y });
#endif
        }

    }

    public void DrawVertices(SKCanvas canvas)
    {
        SKPaint paintStroke = new()
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            IsAntialias = true,
            Color = SKColors.Blue
        };
        SKPath path = new();

        if (!wasTouched)
        {
            path.AddPoly(UntouchedPointsS);
        }
        else
        {
            path.AddPoly(VisiblePointsS);
        }
        canvas.DrawPath(path, paintStroke);

    }

    public void SetPositionToPointLocation(SKPoint VisiblePointToAdjust, int finalIndex) {
        PositionS.X = (VisiblePointToAdjust.X - (PointsP[finalIndex].X * scaleX) + (MoveToFillXP * scaleX)) ;// - (Points[finalIndex].X * scaleX)+ Xoffset;
        PositionS.Y = VisiblePointToAdjust.Y - (PointsP[finalIndex].Y * scaleY) + (MoveToFillYP* scaleY);// - (Points[finalIndex].Y * scaleY)+ Yoffset;
    }

    internal float Distance(SKPoint mousePosition)
    {
        return FSMath.CalculateDistance(mousePosition, Centroid);//use to use midpoint but centroid is better?
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
