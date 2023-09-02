using Fillsquir.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics.Text;
using Microsoft.Maui.Graphics;
using SkiaSharp;

namespace Fillsquir.Controls
{
    internal class PercentageDisplay : GeometryElement
    {
        public double Percentage { get; set; } = 0;

#if DebugString
public string debugString = "pusty text";
#endif
        protected override void DrawMainShape(SKCanvas canvas)
        {
            SKPaint paint = new SKPaint() { 
                Color = SKColors.Red,
                TextAlign = SKTextAlign.Right,
                TextSize = 18
                
            };

            //canvas.DrawRectangle(0,0, (float)Percentage * scaleX, 30);
            //canvas.FillRectangle(0, 0, (float)Percentage * scaleX, 30);




            string text = $"{Percentage}%";
            //IAttributedText attributedText = new AttributedText(text, new List<IAttributedTextRun>(),true);

            //why calculating this crashes the app? 
            // because the canvasWidth is not the same as the defaultCanvasWidth

#if DebugString
            text = debugString;

#endif
            canvas.DrawText(text, canvasWidth - 20, 15, paint);
         //   canvas.DrawString(text, canvasWidth-20, 15, HorizontalAlignment.Right);
        }
    }
}
