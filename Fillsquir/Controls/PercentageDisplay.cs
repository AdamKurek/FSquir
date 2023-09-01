using Fillsquir.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics.Text;
using Microsoft.Maui.Graphics;

namespace Fillsquir.Controls
{
    internal class PercentageDisplay : GeometryElement
    {
        public double Percentage { get; set; } = 0;

#if DebugString
public string debugString = "";
#endif
        protected override void DrawMainShape(ICanvas canvas, RectF dirtyRect)
        {
            canvas.StrokeColor = Colors.DarkRed;
            canvas.FillColor = Colors.White;

            canvas.DrawRectangle(0,0, (float)Percentage * scaleX, 30);
            canvas.FillRectangle(0, 0, (float)Percentage * scaleX, 30);



            canvas.FontSize = 18;
            canvas.FontColor = Colors.LightGoldenrodYellow;//
            canvas.FontColor = Colors.BlueViolet;

            string text = $"{Percentage}%";
            //IAttributedText attributedText = new AttributedText(text, new List<IAttributedTextRun>(),true);

            //why calculating this crashes the app? 
            // because the canvasWidth is not the same as the defaultCanvasWidth
#if DebugString
            text = debugString;

#endif
            canvas.DrawString(text, canvasWidth-20, 15, HorizontalAlignment.Right);
                //canvas.DrawText(attributedText, 10, 10, 400, 400);
        }
    }
}
