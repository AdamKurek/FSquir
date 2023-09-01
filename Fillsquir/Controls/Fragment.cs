﻿#define nDebugVisuals

using Fillsquir.Controls;
using Fillsquir.Interfaces;
using Microsoft.Maui.Graphics;
using System;
using System.Runtime.ExceptionServices;
public class Fragment : GeometryElement
{

    public PointF[] PointsP;

    private PointF[] UntouchedPointsS { get {
            var Up = new PointF[PointsP.Length];
            for (int i = 0; i < PointsP.Length; i++)
            {
                Up[i].X = scaleToMiddleX((PointsP[i].X - MoveToFillXP) - (0.5f * sizeP.X));// + (0.5f * canvasWidth);// ;
                Up[i].Y = scaleToMiddleY((PointsP[i].Y - MoveToFillYP) - (0.5f * sizeP.Y));// +(0.5f * canvasHeight);// ;
            }
            return Up;
        }
    }
    public PointF PositionS;
    public PointF PositionP { get
        {
            PointF ret = new PointF();
            ret.X = (PositionS.X - dravingMoveX )/ scaleX;
            ret.Y = PositionS.Y/scaleY;
            return ret;
        }
    }

    public PointF sizeP;
    //private PointF MoveToFill;
    private float MoveToFillXP;
    private float MoveToFillYP;


    public PointF MidpointS { get {
            PointF midpoint = new PointF();
            if (wasTouched) 
            {
                midpoint.X = PositionS.X + (sizeP.X / 2);
                midpoint.Y = PositionS.Y + (sizeP.Y / 4);
                return midpoint;
            }
            midpoint.X = PositionS.X;
            midpoint.Y = PositionS.Y;

            return midpoint;

        } }
    public float RadiusP { get {
            return Math.Max(sizeP.X, sizeP.Y) / 2;
        } }

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
    private PointF[] visiblePointsS;
    public PointF[] VisiblePointsS
    {
        get
        {
            var pts = new PointF[PointsP.Length];
            for (int i = 0; i < PointsP.Length; i++)
            {
                pts[i] = new PointF((PointsP[i].X * scaleX) + Xoffset, (PointsP[i].Y * scaleY) + Yoffset);
            }
            return pts;
        }
    }

    public PointF[] VisiblePointsP {
            get{
                var pts = new PointF[PointsP.Length];
                for (int i = 0; i < PointsP.Length; i++)
                {
                    pts[i] = new PointF(PointsP[i].X + PositionP.X - MoveToFillXP, PointsP[i].Y + PositionP.Y - MoveToFillYP);
                }
                return pts;
            }
        }

    public Fragment(PointF[] Points, int index)
    {
        {
            float xMin = float.MaxValue, yMin = float.MaxValue, xMax = 0, yMax = 0;
            this.PointsP = Points;

            foreach (PointF point in PointsP)
            {
                if (point.X < xMin) { xMin = point.X; }
                if (point.X > xMax) { xMax = point.X; }
                if (point.Y < yMin) { yMin = point.Y; }
                if (point.Y > yMax) { yMax = point.Y; }
            }
            sizeP = new PointF((xMax - xMin), (yMax - yMin));

            //MoveToFill = new PointF() { X = xMin, Y = yMin };
            MoveToFillXP = xMin;
            MoveToFillYP = yMin;

            this.index = index;
        }
        {
            
        }
    }


    protected override void DrawMainShape(ICanvas canvas, RectF dirtyRect)
    {
        //todo draw using path not lines
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
            canvas.StrokeColor = Colors.LightBlue;
            canvas.FillColor = Colors.Aqua;

            //for (int i = 0; i < Points.Length - 1; i++)
            //{
            //    PointF start = new PointF(Points[i].X * scaleX/8 + position.X, Points[i].Y * scaleY / 8 + position.Y);
            //    PointF end = new PointF(Points[i + 1].X * scaleX / 8 + position.X, Points[i + 1].Y * scaleY / 8 + position.Y);
            //    canvas.DrawLine(start, end);
            //}
            //PointF startOfLastLine = new PointF(Points[Points.Length - 1].X * scaleX / 8 + position.X, Points[Points.Length - 1].Y * scaleY / 8 + position.Y);
            //PointF endOfLastLine = new PointF(Points[0].X * scaleX / 8 + position.X, Points[0].Y * scaleY / 8 + position.Y);
            for (int i = 0; i < PointsP.Length - 1; i++)
            {
                PointF start = new PointF(UntouchedPointsS[i].X * scaleX / 8 + PositionS.X, UntouchedPointsS[i].Y * scaleY / 8 + PositionS.Y);
                PointF end = new PointF(UntouchedPointsS[i + 1].X * scaleX / 8 + PositionS.X, UntouchedPointsS[i + 1].Y * scaleY / 8 + PositionS.Y);
                canvas.DrawLine(start, end);
            }
            PointF startOfLastLine = new PointF(UntouchedPointsS[PointsP.Length - 1].X * scaleX / 8 + PositionS.X, UntouchedPointsS[PointsP.Length - 1].Y * scaleY / 8 + PositionS.Y);
            PointF endOfLastLine = new PointF(UntouchedPointsS[0].X * scaleX / 8 + PositionS.X, UntouchedPointsS[0].Y * scaleY / 8 + PositionS.Y);
            canvas.DrawLine(startOfLastLine, endOfLastLine);

#if DebugVisuals
            canvas.DrawCircle(MidpointS.X, MidpointS.Y, 0.1f * RadiusP * (scaleX > scaleY ? scaleX : scaleY));//clickbox
#endif
            return;
        }
        {
            canvas.StrokeColor = Colors.AliceBlue;
            canvas.FillColor = Colors.Aqua;
            var VP = VisiblePointsS;
            PathF path = new();

            for (int i = 0; i < PointsP.Length; i++)
            {
                path.LineTo(VP[i]);
            }
            canvas.FillPath(path);
#if DebugVisuals
            foreach (var pt in VisiblePointsS)
            {
                canvas.FillColor = Colors.Azure;

                canvas.FillCircle(pt.X, pt.Y, 3);
            }
            canvas.StrokeColor = Colors.BurlyWood;

            canvas.DrawCircle(MidpointS.X, MidpointS.Y, RadiusP* (scaleX>scaleY? scaleX:scaleY));//clickbox
            canvas.StrokeColor = Colors.DarkViolet;

            canvas.DrawRectangle(new RectF() { Height = sizeP.Y*scaleY, Width = sizeP.X* scaleX, X =PositionS.X ,Y= PositionS.Y});
            canvas.StrokeColor = Colors.Red;
            canvas.FillColor = Colors.Red;
            canvas.DrawRectangle(new RectF() { Height = 1000 * scaleY, Width = 1000 * scaleX, X = PositionS.X, Y = PositionS.Y });
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
        PositionS.X = (VisiblePointToAdjust.X - (PointsP[finalIndex].X * scaleX) + (MoveToFillXP * scaleX)) ;// - (Points[finalIndex].X * scaleX)+ Xoffset;
        PositionS.Y = VisiblePointToAdjust.Y - (PointsP[finalIndex].Y * scaleY) + (MoveToFillYP* scaleY);// - (Points[finalIndex].Y * scaleY)+ Yoffset;
            /*
            movetofillx
            position
            pt

             */
    }


    internal float Distance(Point mousePosition)
    {
        PointF mouse = new Point() {X = mousePosition.X,Y = mousePosition.Y };
        return (float)DrawableStack.CalculateDistance(mouse, MidpointS);
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
