using Fillsquir.Interfaces;
using Fillsquir.Visuals;
using SkiaSharp;

namespace Fillsquir.Controls
{
    internal class PercentageDisplay : GeometryElement
    {
        private sealed class StarCelebration
        {
            internal int StarIndex { get; init; }
            internal float StartTimeSeconds { get; init; }
            internal float DurationSeconds { get; init; }
            internal float SourceProgress { get; init; }
        }

        private const float BarHeight = 34f;
        private const int TotalStars = 3;
        private readonly List<StarCelebration> activeCelebrations = new();
        private int syncedStars = -1;

        public PercentageDisplay(GameSettings settings) : base(settings)
        {
        }

        internal bool HasActiveStarAnimations
        {
            get
            {
                float now = gameSettings.RenderTimeSeconds;
                return activeCelebrations.Any(animation => now <= (animation.StartTimeSeconds + animation.DurationSeconds + 0.14f));
            }
        }

        internal void SyncStars(int targetStars)
        {
            targetStars = Math.Clamp(targetStars, 0, TotalStars);
            if (syncedStars < 0)
            {
                syncedStars = targetStars;
                activeCelebrations.Clear();
                return;
            }

            if (targetStars < syncedStars)
            {
                syncedStars = targetStars;
                activeCelebrations.Clear();
                return;
            }

            if (targetStars == syncedStars)
            {
                return;
            }

            float startTime = gameSettings.RenderTimeSeconds;
            float sourceProgress = (float)Math.Clamp(gameSettings.CoveragePercent / 100m, 0m, 1m);
            for (int star = syncedStars; star < targetStars; star++)
            {
                activeCelebrations.Add(new StarCelebration
                {
                    StarIndex = star,
                    StartTimeSeconds = startTime + ((star - syncedStars) * 0.12f),
                    DurationSeconds = 0.95f,
                    SourceProgress = sourceProgress
                });
            }

            syncedStars = targetStars;
        }

#if DebugString
        public string debugString = "";
#endif

        protected override void DrawMainShape(SKCanvas canvas)
        {
            float progress = (float)Math.Clamp(gameSettings.CoveragePercent / 100m, 0m, 1m);
            float nowSeconds = gameSettings.RenderTimeSeconds;
            activeCelebrations.RemoveAll(animation => nowSeconds > (animation.StartTimeSeconds + animation.DurationSeconds + 0.14f));

            VisualSettings settings = CurrentVisualSettings.Normalize();
            SkinDefinition skin = SkinCatalog.Resolve(settings.SelectedSkinId);

            using SKPaint bgPaint = new()
            {
                Color = BlendColor(skin.ShadowColor, skin.BoardColor, 0.18f).WithAlpha(196),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            using SKPaint fillPaint = new()
            {
                Color = BlendColor(skin.HoverColor, skin.FillLightColor, 0.34f).WithAlpha(228),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            using SKPaint textPaint = new()
            {
                Color = skin.KeyLightColor.WithAlpha(236),
                TextSize = 15,
                IsAntialias = true
            };
            using SKPaint dividerPaint = new()
            {
                Color = skin.OutlineColor.WithAlpha(92),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1f,
                IsAntialias = true
            };

            canvas.DrawRect(0, 0, canvasWidth, BarHeight, bgPaint);
            canvas.DrawRect(0, 0, canvasWidth * progress, BarHeight, fillPaint);
            canvas.DrawLine(0, BarHeight - 0.5f, canvasWidth, BarHeight - 0.5f, dividerPaint);

            DrawStarSlots(canvas, settings, skin);
            DrawStarCelebrations(canvas, settings, skin);

            string world = gameSettings.WorldRecordCoveragePercent.HasValue
                ? $"{gameSettings.WorldRecordCoveragePercent.Value:F2}%"
                : "--";
            string text = $"Coverage {gameSettings.CoveragePercent:F2}%  Best {gameSettings.BestCoveragePercent:F2}%  World {world}";
#if DebugString
            if (!string.IsNullOrWhiteSpace(debugString))
            {
                text = debugString;
            }
#endif
            canvas.DrawText(text, 12, 22, textPaint);
        }

        private void DrawStarSlots(SKCanvas canvas, VisualSettings settings, SkinDefinition skin)
        {
            for (int i = 0; i < TotalStars; i++)
            {
                SKPoint center = GetStarSlotCenter(i);
                bool isFilled = i < gameSettings.CurrentStars;
                using SKPath starPath = BuildStarPath(center, 9.5f, 4.3f, -90f);

                using SKPaint slotFill = new()
                {
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true,
                    BlendMode = isFilled ? SKBlendMode.Screen : SKBlendMode.SrcOver,
                    Color = isFilled
                        ? BlendColor(skin.HoverColor, skin.KeyLightColor, 0.46f).WithAlpha(224)
                        : BlendColor(skin.BoardColor, skin.ShadowColor, 0.32f).WithAlpha(148)
                };
                using SKPaint slotStroke = new()
                {
                    Style = SKPaintStyle.Stroke,
                    IsAntialias = true,
                    StrokeWidth = isFilled ? 1.8f : 1.25f,
                    StrokeJoin = SKStrokeJoin.Round,
                    BlendMode = isFilled ? SKBlendMode.Screen : SKBlendMode.SrcOver,
                    Color = isFilled
                        ? skin.KeyLightColor.WithAlpha(248)
                        : skin.OutlineColor.WithAlpha(176)
                };

                if (isFilled && settings.QualityTier != GraphicsQualityTier.Low)
                {
                    using SKPaint glowPaint = new()
                    {
                        Style = SKPaintStyle.Fill,
                        IsAntialias = true,
                        BlendMode = SKBlendMode.Screen,
                        Color = skin.FillLightColor.WithAlpha(settings.QualityTier == GraphicsQualityTier.High ? (byte)74 : (byte)44),
                        ImageFilter = SKImageFilter.CreateBlur(settings.QualityTier == GraphicsQualityTier.High ? 6f : 3f, settings.QualityTier == GraphicsQualityTier.High ? 6f : 3f)
                    };
                    canvas.DrawPath(starPath, glowPaint);
                }

                canvas.DrawPath(starPath, slotFill);
                canvas.DrawPath(starPath, slotStroke);
            }
        }

        private void DrawStarCelebrations(SKCanvas canvas, VisualSettings settings, SkinDefinition skin)
        {
            foreach (StarCelebration celebration in activeCelebrations)
            {
                float elapsed = gameSettings.RenderTimeSeconds - celebration.StartTimeSeconds;
                if (elapsed < 0f)
                {
                    continue;
                }

                float t = Math.Clamp(elapsed / celebration.DurationSeconds, 0f, 1f);
                float eased = EaseOutCubic(t);
                SKPoint source = new(14f + (celebration.SourceProgress * Math.Max(0f, canvasWidth - 120f)), BarHeight - 8f);
                SKPoint target = GetStarSlotCenter(celebration.StarIndex);
                float arcHeight = (18f + (celebration.StarIndex * 6f)) * MathF.Sin(t * MathF.PI);
                SKPoint position = new(
                    Lerp(source.X, target.X, eased),
                    Lerp(source.Y, target.Y, eased) - arcHeight);

                DrawStarTrail(canvas, skin, source, position, t);

                float pulse = MathF.Sin(MathF.Min(1f, t * 1.35f) * MathF.PI);
                float outerRadius = 8.5f + (4f * pulse);
                float innerRadius = 3.8f + (1.45f * pulse);
                float rotation = -90f + (t * 260f);
                byte starAlpha = (byte)Math.Clamp((int)MathF.Round(255f * MathF.Min(1f, t * 3f)), 0, 255);

                using SKPath starPath = BuildStarPath(position, outerRadius, innerRadius, rotation);
                if (settings.QualityTier != GraphicsQualityTier.Low)
                {
                    using SKPaint glowPaint = new()
                    {
                        Style = SKPaintStyle.Fill,
                        IsAntialias = true,
                        BlendMode = SKBlendMode.Screen,
                        Color = skin.KeyLightColor.WithAlpha((byte)Math.Clamp((int)(starAlpha * 0.42f), 0, 255)),
                        ImageFilter = SKImageFilter.CreateBlur(settings.QualityTier == GraphicsQualityTier.High ? 9f : 5f, settings.QualityTier == GraphicsQualityTier.High ? 9f : 5f)
                    };
                    canvas.DrawPath(starPath, glowPaint);
                }

                using SKPaint starFill = new()
                {
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true,
                    BlendMode = SKBlendMode.Screen,
                    Color = BlendColor(skin.HoverColor, skin.KeyLightColor, 0.56f).WithAlpha(starAlpha)
                };
                using SKPaint starStroke = new()
                {
                    Style = SKPaintStyle.Stroke,
                    IsAntialias = true,
                    StrokeJoin = SKStrokeJoin.Round,
                    StrokeWidth = 2f,
                    BlendMode = SKBlendMode.Screen,
                    Color = skin.KeyLightColor.WithAlpha(starAlpha)
                };

                canvas.DrawPath(starPath, starFill);
                canvas.DrawPath(starPath, starStroke);

                if (t > 0.72f)
                {
                    float ringProgress = (t - 0.72f) / 0.28f;
                    using SKPaint ringPaint = new()
                    {
                        Style = SKPaintStyle.Stroke,
                        IsAntialias = true,
                        StrokeWidth = 2.2f - ringProgress,
                        BlendMode = SKBlendMode.Screen,
                        Color = skin.FillLightColor.WithAlpha((byte)Math.Clamp((int)(110f * (1f - ringProgress)), 0, 255))
                    };
                    canvas.DrawCircle(target.X, target.Y, 10f + (ringProgress * 12f), ringPaint);
                }
            }
        }

        private void DrawStarTrail(SKCanvas canvas, SkinDefinition skin, SKPoint source, SKPoint current, float t)
        {
            for (int i = 0; i < 4; i++)
            {
                float trailT = Math.Clamp(t - (i * 0.09f), 0f, 1f);
                if (trailT <= 0f)
                {
                    continue;
                }

                float eased = EaseOutCubic(trailT);
                SKPoint trailPosition = new(
                    Lerp(source.X, current.X, eased),
                    Lerp(source.Y, current.Y, eased) - (4f * i));
                float radius = 3.6f - (i * 0.65f);
                byte alpha = (byte)Math.Clamp((int)(124f - (i * 24f)), 0, 255);

                using SKPaint sparklePaint = new()
                {
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true,
                    BlendMode = SKBlendMode.Screen,
                    Color = skin.FillLightColor.WithAlpha(alpha)
                };
                canvas.DrawCircle(trailPosition.X, trailPosition.Y, radius, sparklePaint);
            }
        }

        private SKPoint GetStarSlotCenter(int index)
        {
            float spacing = 26f;
            float rightPadding = 18f;
            float totalWidth = (TotalStars - 1) * spacing;
            float startX = canvasWidth - rightPadding - totalWidth;
            return new SKPoint(startX + (index * spacing), BarHeight * 0.5f);
        }

        private static SKPath BuildStarPath(SKPoint center, float outerRadius, float innerRadius, float rotationDegrees)
        {
            SKPath path = new();
            float rotationRadians = rotationDegrees * (MathF.PI / 180f);

            for (int i = 0; i < 10; i++)
            {
                float angle = rotationRadians + (i * MathF.PI / 5f);
                float radius = (i % 2 == 0) ? outerRadius : innerRadius;
                SKPoint point = new(
                    center.X + (MathF.Cos(angle) * radius),
                    center.Y + (MathF.Sin(angle) * radius));

                if (i == 0)
                {
                    path.MoveTo(point);
                }
                else
                {
                    path.LineTo(point);
                }
            }

            path.Close();
            return path;
        }

        private static float EaseOutCubic(float t)
        {
            float clamped = Math.Clamp(t, 0f, 1f);
            float inv = 1f - clamped;
            return 1f - (inv * inv * inv);
        }

        private static float Lerp(float from, float to, float amount)
        {
            return from + ((to - from) * amount);
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
