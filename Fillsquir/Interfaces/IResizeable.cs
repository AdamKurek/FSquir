using Fillsquir.Controls;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fillsquir.Interfaces
{
    internal abstract class GeometryElement: SKDrawable
    {

        static internal float defaultCanvasWidth = 1000;
        static internal float defaultCanvasHeight = 1000;
        internal float canvasWidth = 1000;
        internal float canvasHeight = 1000;
        internal GameSettings gameSettings;
        private GeometryElement() { }
        internal GeometryElement(GameSettings gameSettings)
        {
            this.gameSettings = gameSettings;
        } 


        protected float scaleX => (canvasWidth / (defaultCanvasWidth / gameSettings.prop1 * gameSettings.prop2));

        protected float scaleY => (canvasHeight / (defaultCanvasHeight / gameSettings.prop1 * gameSettings.prop2));
        internal void Resize(float Width, float Height)
        {
            if (Width <= 0 || Height <= 0)
                return;
            ResizePrecize(Width, Height);
            canvasWidth = Width;
            canvasHeight = Height;
        }
        protected virtual void ResizePrecize(float Width, float Height) { }

        internal void Draw(SKCanvas canvas)
        {
            DrawMainShape(canvas);

        }

        protected abstract void DrawMainShape(SKCanvas canvas);
    }
}
