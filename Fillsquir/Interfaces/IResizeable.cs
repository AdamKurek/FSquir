using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fillsquir.Interfaces
{
    public abstract class GeometryElement: IDrawable
    {

        static public float defaultCanvasWidth = 1000;
        static public float defaultCanvasHeight = 1000;
        public float canvasWidth = 1000;
        public float canvasHeight = 1000;
        public static float prop1 = 3;
        public static float prop2 = 4;
        protected float scaleX => (canvasWidth / (defaultCanvasWidth / prop1 * prop2));

        protected float scaleY => (canvasHeight / (defaultCanvasHeight / prop1 * prop2));
        public void Resize(float Width, float Height)
        {
            if (Width <= 0 || Height <= 0)
                return;
            canvasWidth = Width;
            canvasHeight = Height;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            DrawMainShape(canvas, dirtyRect);

        }

        protected abstract void DrawMainShape(ICanvas canvas, RectF dirtyRect);
    }
}
