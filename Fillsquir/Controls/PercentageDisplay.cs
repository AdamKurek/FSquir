using Fillsquir.Interfaces;
using SkiaSharp;

namespace Fillsquir.Controls
{
    internal class PercentageDisplay : GeometryElement
    {
        public PercentageDisplay(GameSettings settings) : base(settings)
        {
        }

#if DebugString
        public string debugString = "";
#endif

        protected override void DrawMainShape(SKCanvas canvas)
        {
            float barHeight = 28f;
            float progress = (float)Math.Clamp(gameSettings.CoveragePercent / 100m, 0m, 1m);

            using SKPaint bgPaint = new()
            {
                Color = new SKColor(20, 20, 20, 180),
                Style = SKPaintStyle.Fill
            };
            using SKPaint fillPaint = new()
            {
                Color = SKColors.Orange,
                Style = SKPaintStyle.Fill
            };
            using SKPaint textPaint = new()
            {
                Color = SKColors.White,
                TextSize = 16,
                IsAntialias = true
            };

            canvas.DrawRect(0, 0, canvasWidth, barHeight, bgPaint);
            canvas.DrawRect(0, 0, canvasWidth * progress, barHeight, fillPaint);

            string world = gameSettings.WorldRecordCoveragePercent.HasValue
                ? $"{gameSettings.WorldRecordCoveragePercent.Value:F2}%"
                : "--";
            string text = $"Coverage {gameSettings.CoveragePercent:F2}%  Best {gameSettings.BestCoveragePercent:F2}%  World {world}  Stars {gameSettings.CurrentStars}/3";
#if DebugString
            if (!string.IsNullOrWhiteSpace(debugString))
            {
                text = debugString;
            }
#endif
            canvas.DrawText(text, 12, 20, textPaint);
        }
    }
}
