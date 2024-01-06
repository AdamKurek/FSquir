using Fillsquir.Interfaces;
using SkiaSharp;

namespace Fillsquir.Controls
{
    internal class PercentageDisplay : GeometryElement
    {
        public PercentageDisplay(GameSettings settings) : base(settings)
        {

        }

        public double Percentage { get 
            { 
                return gameSettings.percentageFilled / gameSettings.percentageRequired *100000;
            } 
        }

#if DebugString
public string debugString = "pusty text";
#endif
        protected override void DrawMainShape(SKCanvas canvas)
        {
            SKPaint paint = new SKPaint() { 
                Color = SKColors.Red,
                TextAlign = SKTextAlign.Right,
                TextSize = 20,
            };
            SKPaint stripPaint = new SKPaint()
            {
                Color = SKColors.WhiteSmoke,
                Style = SKPaintStyle.StrokeAndFill,
            };

            canvas.DrawRect(0, 0, (float)Percentage*scaleX, 30, stripPaint);//????

            string text = $"{Percentage}%";
            //IAttributedText attributedText = new AttributedText(text, new List<IAttributedTextRun>(),true);

#if DebugString
            text = debugString;
#endif
            canvas.DrawText(text, canvasWidth - 20, 150, paint);
        }
    }
}
