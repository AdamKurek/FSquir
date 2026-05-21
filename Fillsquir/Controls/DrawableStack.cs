using Clipper2Lib;
using Fillsquir.Interfaces;
using Fillsquir.Visuals;
using SkiaSharp;

namespace Fillsquir.Controls
{
    internal class DrawableStack : GeometryElement
    {
        private float screenWidth = 1000;
        private float screenHeight = 1000;
        private readonly DropParticleSystem dropParticleSystem = new();
        public List<GeometryElement> drawables = new();
        public GeometryElement cover = null!;

        internal DrawableStack(GameSettings settings) : base(settings)
        {
        }

        internal HashSet<SKPoint> allActivePoints(int ignoreIndex)
        {
            var set = new HashSet<SKPoint>();
            Squir sq = (Squir)drawables[0];
            foreach (var pt in sq.VisiblePoints)
            {
                set.Add(pt);
            }

            for (int i = 1; i < drawables.Count; i++)
            {
                if (drawables[i] is not Fragment drawable || !drawable.wasTouched)
                {
                    continue;
                }

                if (i == ignoreIndex)
                {
                    continue;
                }

                foreach (var pt in drawable.VisiblePointsS)
                {
                    set.Add(pt);
                }
            }

            return set;
        }

        internal GeometryElement this[int i]
        {
            get => drawables[i];
            set => drawables[i] = value;
        }

        internal void AddDrawable(GeometryElement drawable)
        {
            drawables.Add(drawable);
            drawable.Resize(screenWidth, screenHeight);
        }

        internal void AddCover(GeometryElement drawable)
        {
            cover = drawable;
        }

#if DebugClickingLines
        public class Line
        {
            public Point p;
            public Point q;
        }

        public Line testLine;
        public bool isCrossing;
#endif

#if DebugClicking
        public struct Drawpoint
        {
            public SKPoint point;
            public bool inBounds;
        }

        public List<Drawpoint> clickPoints = new List<Drawpoint> { };

        public void AddDot(SKPoint point, bool inBounds = false)
        {
            Drawpoint drawpoint = new Drawpoint();
            drawpoint.point.X = (float)point.X;
            drawpoint.point.Y = (float)point.Y;
            drawpoint.inBounds = inBounds;
            clickPoints.Add(drawpoint);
        }
#endif

        internal SKCanvas DrawPreZoom(SKCanvas canvas, double nowSeconds)
        {
            this[0].Draw(canvas);

            foreach (Fragment drawable in gameSettings.CenterFragments)
            {
                if (drawable.wasTouched)
                {
                    drawable.Draw(canvas);
                }
            }

            cover?.Draw(canvas);
            DrawOverlapCues(canvas);
            DrawOutsideBoardDeadZone(canvas);
            dropParticleSystem.Draw(canvas, nowSeconds);

#if DebugClickingLines
            if (isCrossing)
            {
                canvas.StrokeColor = Colors.Magenta;
            }
            else
            {
                canvas.StrokeColor = Colors.Yellow;
            }

            if (testLine is not null)
            {
                canvas.DrawLine(testLine.q, testLine.p);
            }
#endif
            return canvas;
        }

        private void DrawOverlapCues(SKCanvas canvas)
        {
            List<Fragment> activeFragments = gameSettings.CenterFragments
                .Where(static fragment => fragment.wasTouched)
                .ToList();
            if (activeFragments.Count < 2)
            {
                return;
            }

            VisualSettings visualSettings = CurrentVisualSettings.Normalize();
            SkinDefinition skin = SkinCatalog.Resolve(visualSettings.SelectedSkinId);

            byte fillAlpha = visualSettings.QualityTier switch
            {
                GraphicsQualityTier.Low => 42,
                GraphicsQualityTier.Medium => 58,
                _ => 72
            };
            byte strokeAlpha = visualSettings.QualityTier switch
            {
                GraphicsQualityTier.Low => 84,
                GraphicsQualityTier.Medium => 108,
                _ => 132
            };
            byte glowAlpha = visualSettings.QualityTier switch
            {
                GraphicsQualityTier.High => 54,
                GraphicsQualityTier.Medium => 34,
                _ => 0
            };

            using SKPaint fillPaint = new()
            {
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
                BlendMode = SKBlendMode.Screen,
                Color = BlendColor(skin.HoverColor, skin.FillLightColor, 0.42f).WithAlpha(fillAlpha)
            };

            using SKPaint strokePaint = new()
            {
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                StrokeJoin = SKStrokeJoin.Round,
                StrokeCap = SKStrokeCap.Round,
                StrokeWidth = 1.35f + (0.95f * visualSettings.DepthIntensity),
                BlendMode = SKBlendMode.Screen,
                Color = BlendColor(skin.KeyLightColor, skin.HoverColor, 0.35f).WithAlpha(strokeAlpha)
            };

            using SKPaint glowPaint = new()
            {
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
                BlendMode = SKBlendMode.Screen,
                Color = skin.KeyLightColor.WithAlpha(glowAlpha)
            };

            if (glowAlpha > 0)
            {
                float blurRadius = visualSettings.QualityTier == GraphicsQualityTier.High ? 9f : 5f;
                glowPaint.ImageFilter = SKImageFilter.CreateBlur(blurRadius, blurRadius);
            }

            for (int i = 0; i < activeFragments.Count; i++)
            {
                Fragment first = activeFragments[i];
                SKPoint[] firstPoints = first.VisiblePointsS;
                SKRect firstBounds = ComputeBounds(firstPoints);

                for (int j = i + 1; j < activeFragments.Count; j++)
                {
                    Fragment second = activeFragments[j];
                    SKPoint[] secondPoints = second.VisiblePointsS;
                    if (!firstBounds.IntersectsWith(ComputeBounds(secondPoints)))
                    {
                        continue;
                    }

                    foreach (SKPoint[] overlapShape in IntersectPolygons(firstPoints, secondPoints))
                    {
                        if (overlapShape.Length < 3 || MathF.Abs(FSMath.CalculateArea(overlapShape)) < 1f)
                        {
                            continue;
                        }

                        using SKPath overlapPath = new();
                        overlapPath.AddPoly(overlapShape);

                        if (glowAlpha > 0)
                        {
                            canvas.DrawPath(overlapPath, glowPaint);
                        }

                        canvas.DrawPath(overlapPath, fillPaint);
                        canvas.DrawPath(overlapPath, strokePaint);
                    }
                }
            }
        }

        internal void SpawnDropParticles(Fragment fragment, double nowSeconds)
        {
            if (fragment is null)
            {
                return;
            }

            VisualSettings visualSettings = CurrentVisualSettings.Normalize();
            SkinDefinition skin = SkinCatalog.Resolve(visualSettings.SelectedSkinId);
            int baseCount = visualSettings.QualityTier switch
            {
                GraphicsQualityTier.Low => 14,
                GraphicsQualityTier.Medium => 22,
                _ => 30
            };
            int count = Math.Max(8, (int)MathF.Round(baseCount * Math.Clamp(skin.DropParticleProfile.CountScale, 0.5f, 2f)));

            dropParticleSystem.Spawn(
                fragment.VisiblePointsS,
                skin.HoverColor.WithAlpha(220),
                skin.KeyLightColor.WithAlpha(235),
                count,
                skin.DropParticleProfile,
                nowSeconds);
        }

        internal bool HasActiveDropParticles => dropParticleSystem.HasActiveParticles;

        private void DrawOutsideBoardDeadZone(SKCanvas canvas)
        {
            if (drawables.Count == 0 || drawables[0] is not Squir board)
            {
                return;
            }

            SKPoint[] boardPoints = board.VisiblePoints;
            if (boardPoints.Length < 3)
            {
                return;
            }

            using SKPath boardPath = new();
            boardPath.AddPoly(boardPoints);

            using SKPaint deadZonePaint = new()
            {
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
                BlendMode = SKBlendMode.Multiply,
                Color = new SKColor(54, 58, 62, 96)
            };

            SKRect fullRect = new(
                -screenWidth,
                -screenHeight,
                screenWidth * 3f,
                screenHeight * 3f);

            canvas.Save();
            canvas.ClipPath(boardPath, SKClipOperation.Difference, antialias: true);
            canvas.DrawRect(fullRect, deadZonePaint);
            canvas.Restore();
        }

        internal SKCanvas DrawPastZoom(SKCanvas canvas)
        {
            float stripTop = screenHeight * gameSettings.prop1 / gameSettings.prop2;
            SKRect stripRect = new(0f, stripTop, screenWidth, screenHeight);
            float radius = MathF.Min(24f, (screenHeight - stripTop) * 0.24f);

            var visualSettings = CurrentVisualSettings.Normalize();
            SKPaint stripPaint = PuzzleMaterialService.GetStripBackgroundPaint(CurrentPuzzleKey, visualSettings, stripRect);
            canvas.DrawRoundRect(new SKRoundRect(stripRect, radius, radius), stripPaint);

            SKPaint dividerPaint = PuzzleMaterialService.GetStripDividerPaint(visualSettings);
            canvas.DrawLine(stripRect.Left + 8f, stripRect.Top + 0.5f, stripRect.Right - 8f, stripRect.Top + 0.5f, dividerPaint);

            using SKPaint dividerShadow = new()
            {
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                StrokeWidth = dividerPaint.StrokeWidth + 0.3f,
                BlendMode = SKBlendMode.Multiply,
                Color = dividerPaint.Color.WithAlpha((byte)Math.Clamp(dividerPaint.Color.Alpha / 3, 8, 72))
            };

            canvas.DrawLine(stripRect.Left + 8f, stripRect.Top + 2f, stripRect.Right - 8f, stripRect.Top + 2f, dividerShadow);

            canvas.Save();
            canvas.ClipRect(stripRect);

            var cols = gameSettings.Cols;
            if (cols > gameSettings.VisibleRows)
            {
                cols = gameSettings.VisibleRows;
            }

            int colsmove = (int)(gameSettings.bottomStripMove / (screenWidth / gameSettings.VisibleRows));
            cols += colsmove + 1;
            if (cols >= gameSettings.untouchedFragments.Length / gameSettings.Rows)
            {
                cols = gameSettings.untouchedFragments.Length / gameSettings.Rows;
            }

            for (int j = colsmove; j < cols; j++)
            {
                for (int i = 0; i < gameSettings.Rows; i++)
                {
                    gameSettings.untouchedFragments[j, i]?.Draw(canvas);
                }
            }

            canvas.Restore();

            return canvas;
        }

        internal void DrawFragmentsoutlines(SKCanvas canvas)
        {
            foreach (Fragment drawable in gameSettings.CenterFragments)
            {
#if DebugVisuals
                drawable.DrawVertices(canvas);
#endif
                if (drawable.wasTouched)
                {
                    drawable.DrawVertices(canvas);
                }
            }

#if DebugClicking
            foreach (var circle in clickPoints)
            {
                var pt = new SKPaint();
                pt.Color = SKColors.Green;
                if (!circle.inBounds)
                {
                    pt.Color = SKColors.Red;
                }

                canvas.DrawCircle(circle.point.X, circle.point.Y, 1, pt);
            }
#endif
        }

        protected override void ResizePrecize(float width, float height)
        {
            screenWidth = width;
            screenHeight = height;
            foreach (var drawable in drawables)
            {
                drawable.Resize(width, height);
            }

            cover?.Resize(width, height);
        }

        internal Fragment? SelectFragmentOnClick(SKPoint mousePosition)
        {
            float nearestDistance = float.MaxValue;
            Fragment? nearestFragment = null;

            foreach (Fragment fragment in drawables.Skip(1).OfType<Fragment>())
            {
                if (!fragment.wasTouched)
                {
                    continue;
                }

                if (!FSMath.IsPointInShape(mousePosition, fragment.VisiblePointsS))
                {
                    continue;
                }

                float distance = fragment.Distance(mousePosition);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestFragment = fragment;
                }
            }

            return nearestFragment;
        }

        private static List<SKPoint[]> IntersectPolygons(SKPoint[] first, SKPoint[] second)
        {
            Paths64 subject = new() { FSMath.SKPointArrayToPath64(first) };
            Paths64 clip = new() { FSMath.SKPointArrayToPath64(second) };
            Paths64 overlap = Clipper.Intersect(subject, clip, FillRule.NonZero);

            List<SKPoint[]> result = new();
            foreach (Path64 path in overlap)
            {
                result.AddRange(FSMath.Path64ToSKPointArrayList(path));
            }

            return result;
        }

        private static SKRect ComputeBounds(SKPoint[] points)
        {
            if (points.Length == 0)
            {
                return SKRect.Empty;
            }

            float minX = points[0].X;
            float minY = points[0].Y;
            float maxX = points[0].X;
            float maxY = points[0].Y;

            for (int i = 1; i < points.Length; i++)
            {
                SKPoint point = points[i];
                if (point.X < minX) minX = point.X;
                if (point.Y < minY) minY = point.Y;
                if (point.X > maxX) maxX = point.X;
                if (point.Y > maxY) maxY = point.Y;
            }

            return new SKRect(minX, minY, maxX, maxY);
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

        protected override void DrawMainShape(SKCanvas canvas)
        {
            throw new NotImplementedException();
        }
    }
}
