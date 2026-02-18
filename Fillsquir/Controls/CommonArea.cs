#define nDebugVisualsCommonArea
using Clipper2Lib;
using Fillsquir.Interfaces;
using Fillsquir.Visuals;
using Microsoft.Maui.Graphics;
using SkiaSharp;

namespace Fillsquir.Controls
{
    internal class CommonArea : GeometryElement
    {
        internal double Area
        {
            get
            {
                var area = 0.0;
                foreach (SKPoint[] shape in FiguresP)
                {
                    int parents = 0;
                    foreach (SKPoint[] parent in FiguresP)
                    {
                        bool inParent = true;
                        foreach (SKPoint point in shape)
                        {
                            if (!FSMath.IsPointInShape(point, parent))
                            {
                                inParent = false;
                                break;
                            }
                        }

                        if (inParent)
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

        internal List<SKPoint[]> VisibleFiguresS
        {
            get
            {
                var fgs = new List<SKPoint[]>();
                foreach (SKPoint[] f in FiguresP)
                {
                    var visibleFigure = new SKPoint[f.Count()];
                    for (int i = 0; i < f.Length; i++)
                    {
                        visibleFigure[i] = new SKPoint((f[i].X * scaleX) + gameSettings.xoffset, f[i].Y * scaleY + gameSettings.yoffset);
                    }

                    fgs.Add(visibleFigure);
                }

                return fgs;
            }
        }

        private List<Fragment> fragmentsInside = new();

        public List<Fragment> FragmentsInside
        {
            get => fragmentsInside;
            internal set
            {
                fragmentsInside = value;
                FiguresP = calculateCommonArea(squir.PointsP, fragmentsInside);
            }
        }

        public List<SKPoint[]> calculateCommonArea(SKPoint[] p1, List<Fragment> p2)
        {
            Paths64 subject = new();
            Paths64 clip = new();
            subject.Add(FSMath.SKPointArrayToPath64(p1));

            foreach (Fragment figure in p2)
            {
                clip.Add(FSMath.SKPointArrayToPath64(figure.VisiblePointsP));
            }

            Paths64 commonArea = Clipper.Intersect(subject, clip, FillRule.NonZero);
            List<SKPoint[]> result = new();
            foreach (Path64 path in commonArea)
            {
                result.AddRange(FSMath.Path64ToSKPointArrayList(path));
            }

            return result;
        }

        public Squir squir { get; }

        internal CommonArea(GameSettings settings, Squir squir)
            : base(settings)
        {
            this.squir = squir;
        }

        protected override void DrawMainShape(SKCanvas canvas)
        {
            List<SKPoint[]> visibleFigures = VisibleFiguresS;
            if (visibleFigures.Count == 0)
            {
                return;
            }

            var visualSettings = CurrentVisualSettings;
            SkinDefinition skin = SkinCatalog.Resolve(visualSettings.SelectedSkinId);
            SKPaint outlinePaint = PuzzleMaterialService.GetOutlinePaint(visualSettings);

            using SKPaint fillPaint = new()
            {
                Style = SKPaintStyle.Fill,
                StrokeWidth = 1,
                IsAntialias = true
            };

            foreach (SKPoint[] shape in visibleFigures)
            {
                int parents = 0;
                foreach (SKPoint[] parent in visibleFigures)
                {
                    bool inParent = true;
                    foreach (SKPoint point in shape)
                    {
                        if (!FSMath.IsPointInShape(point, parent))
                        {
                            inParent = false;
                            break;
                        }
                    }

                    if (inParent)
                    {
                        parents++;
                    }
                }

                bool isHole = parents % 2 == 0;
                if (isHole)
                {
                    fillPaint.BlendMode = SKBlendMode.Multiply;
                    fillPaint.Color = BlendColor(skin.BoardColor, skin.ShadowColor, 0.52f).WithAlpha(82);
                }
                else
                {
                    fillPaint.BlendMode = SKBlendMode.Screen;
                    fillPaint.Color = BlendColor(skin.FillLightColor, skin.HoverColor, 0.25f).WithAlpha(108);
                }

                using SKPath path = new();
                path.AddPoly(shape);
                canvas.DrawPath(path, fillPaint);
                canvas.DrawPath(path, outlinePaint);

#if DebugVisualsCommonArea
                canvas.StrokeColor = Colors.Gray;
                canvas.FillColor = Colors.AntiqueWhite;
                foreach (SKPoint pt in shape)
                {
                    canvas.FillCircle(pt.X, pt.Y, 3);
                }
#endif
            }
        }

        private static SKColor BlendColor(SKColor from, SKColor to, float amount)
        {
            float t = Math.Clamp(amount, 0f, 1f);

            byte r = (byte)Math.Clamp((int)MathF.Round(from.Red + ((to.Red - from.Red) * t)), 0, 255);
            byte g = (byte)Math.Clamp((int)MathF.Round(from.Green + ((to.Green - from.Green) * t)), 0, 255);
            byte b = (byte)Math.Clamp((int)MathF.Round(from.Blue + ((to.Blue - from.Blue) * t)), 0, 255);
            byte a = (byte)Math.Clamp((int)MathF.Round(from.Alpha + ((to.Alpha - from.Alpha) * t)), 0, 255);

            return new SKColor(r, g, b, a);
        }
    }
}
