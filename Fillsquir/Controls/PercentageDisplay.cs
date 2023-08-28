using Fillsquir.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics.Text;

namespace Fillsquir.Controls
{
    internal class PercentageDisplay : GeometryElement
    {
        public double Percentage { get; set; } = 0;

        protected override void DrawMainShape(ICanvas canvas, RectF dirtyRect)
        {
            canvas.StrokeColor = Colors.DarkRed;
            canvas.FillColor = Colors.White;

            canvas.DrawRectangle(0,0, (float)Percentage/canvasWidth, 30);
            canvas.FillRectangle(0, 0, (float)Percentage / canvasWidth, 30);



            canvas.FontSize = 18;
            canvas.FontColor = Colors.LightGoldenrodYellow;
            string text = $"{Percentage}%";
            IAttributedText attributedText = new AttributedText(text, new List<IAttributedTextRun>(),true);
            canvas.DrawString(text, 1000, 0, HorizontalAlignment.Right);
                //canvas.DrawText(attributedText, 10, 10, 400, 400);
        }
    }
}
