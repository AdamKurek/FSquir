#define nDebugVisualsCommonArea
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fillsquir.Interfaces;
using Microsoft.Maui.Graphics;
using SkiaSharp;

namespace Fillsquir.Controls
{
    public class CommonArea : GeometryElement
    {
        public double Area
        {
            get{
                var area = 0.0;
                foreach (var shape in FiguresP)
                {
                    int parents = 0;
                    foreach (var parent in FiguresP)//optimize
                    {
                        bool InParent = true;
                        foreach (var point in shape)
                        {
                            if (!FSMath.IsPointInShape(point, parent))
                            {
                                InParent = false;
                                break;
                            }
                        }
                        if (InParent)
                        {
                            parents++;
                        }
                    }
                    if (parents % 2 == 0)
                    {
                        area -= FSMath.CalculateArea(shape);
                    }
                    else
                    {
                        area += FSMath.CalculateArea(shape);
                    }
                }
                return area;
            }
        }
        public List<SKPoint[]> FiguresP = new();
        float Xoffset => (canvasWidth - ((gameSettings.prop1 / gameSettings.prop2) * canvasWidth)) / 2;

        public List<SKPoint[]> VisibleFiguresS
        {
            get
            {
                var fgs = new List<SKPoint[]>();
                foreach (var f in FiguresP)
                {
                    var visibleFigure = new SKPoint[f.Count()];
                    for (int i = 0; i < f.Length; i++)
                    {
                        visibleFigure[i] = new SKPoint((f[i].X * scaleX) + Xoffset + gameSettings.xoffset, f[i].Y * scaleY + gameSettings.yoffset);
                    }
                    fgs.Add(visibleFigure);
                }
                return fgs;

            }
        }

        internal CommonArea(GameSettings settings):base(settings)
        {
        }

        internal void AddFigure(SKPoint[] figure)
        {
            FiguresP.Add(figure);
        }


        protected override void DrawMainShape(SKCanvas canvas)
        {

            SKPaint paintStroke = new()
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                IsAntialias = true
            };
            SKPaint paintFill = new()
            {
                Style = SKPaintStyle.Fill,
                StrokeWidth = 1,
                IsAntialias = true
            };
            foreach (var shape in VisibleFiguresS)
            {
                int parents = 0;
                foreach (var parent in VisibleFiguresS)//optimize
                {
                    bool InParent = true;
                    foreach (var point in shape)
                    {

                        if (!FSMath.IsPointInShape(point, parent))
                        {
                            InParent = false;
                            break;
                        }
                    }
                    if (InParent)
                    {
                        parents++;
                    }
                }

                if (parents % 2 == 0)
                {
                    paintFill.Color = SKColors.Black;
                    paintStroke.Color = SKColors.Yellow;
                }
                else
                {
                    paintFill.Color = SKColors.DarkOrange;
                    paintStroke.Color = SKColors.Orange;
                }

                SKPath path = new();
                path.AddPoly(shape);
                canvas.DrawPath(path, paintStroke);
                canvas.DrawPath(path, paintFill);

#if DebugVisualsCommonArea
                canvas.StrokeColor = Colors.Gray;
                canvas.FillColor = Colors.AntiqueWhite;
                foreach (var pt in shape)
                {
                    canvas.FillCircle(pt.X, pt.Y, 3);

                }
#endif
            }
        }
    }
}
