#define nDebugVisualsCommonArea
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Clipper2Lib;
using Fillsquir.Interfaces;
using Microsoft.Maui.Graphics;
using SkiaSharp;

namespace Fillsquir.Controls
{
    internal class CommonArea : GeometryElement
    {
        internal double Area
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
        internal List<SKPoint[]> FiguresP = new();
        //float Xoffset => (canvasWidth - ((gameSettings.prop1 / gameSettings.prop2) * canvasWidth)) / 2;

        internal List<SKPoint[]> VisibleFiguresS
        {
            get
            {
                var fgs = new List<SKPoint[]>();
                foreach (var f in FiguresP)
                {
                    var visibleFigure = new SKPoint[f.Count()];
                    for (int i = 0; i < f.Length; i++)
                    {
                        visibleFigure[i] = new SKPoint((f[i].X * scaleX)  + gameSettings.xoffset, f[i].Y * scaleY + gameSettings.yoffset);//+ Xoffset
                    }
                    fgs.Add(visibleFigure);
                }
                return fgs;

            }
        }

        private List<Fragment> fragmentsInside = new List<Fragment>();
        public List<Fragment> FragmentsInside {
            get { return fragmentsInside; }
            internal set
            {
                fragmentsInside = value;
                FiguresP = calculateCommonArea(squir.PointsP, fragmentsInside);
            } 
        }


        public List<SKPoint[]> calculateCommonArea(SKPoint[] p1, List<Fragment> p2)
        {
            Paths64 subject = new Paths64();
            Paths64 clip = new Paths64();
            subject.Add(FSMath.SKPointArrayToPath64(p1));
            foreach (var figure in p2)
            {
                clip.Add(FSMath.SKPointArrayToPath64(figure.VisiblePointsP));
            }
            Paths64 commonArea = Clipper.Intersect(subject, clip, FillRule.NonZero);
            List<SKPoint[]> result = new List<SKPoint[]>();
            foreach (var path in commonArea)
            {
                result.AddRange(FSMath.Path64ToSKPointArrayList(path));
            }
            return result;
        }

        public Squir squir { get; }
        internal CommonArea(GameSettings settings, Squir squir):base(settings)
        {
            this.squir = squir;
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
