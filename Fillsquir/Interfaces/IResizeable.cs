using Fillsquir.Controls;
using Fillsquir.Services;
using SkiaSharp;

namespace Fillsquir.Interfaces
{
    internal abstract class GeometryElement : SKDrawable
    {
        static internal float defaultCanvasWidth = 1000;
        static internal float defaultCanvasHeight = 1000;
        internal float canvasWidth = 1000;
        internal float canvasHeight = 1000;
        internal GameSettings gameSettings;

        private ICoordinateTransformer? coordinateTransformer;

        private GeometryElement()
        {
        }

        internal GeometryElement(GameSettings gameSettings)
        {
            this.gameSettings = gameSettings;
        }

        protected ICoordinateTransformer CoordinateTransformer
        {
            get
            {
                if (coordinateTransformer is not null)
                {
                    return coordinateTransformer;
                }

                coordinateTransformer = App.Services?.GetService(typeof(ICoordinateTransformer)) as ICoordinateTransformer;
                coordinateTransformer ??= new CoordinateTransformer();
                return coordinateTransformer;
            }
        }

        protected float scaleX => CoordinateTransformer.MapScaleX(canvasWidth);

        protected float scaleY => CoordinateTransformer.MapScaleY(canvasHeight);

        protected SKPoint WorldToScaledScreen(SKPoint worldPoint)
        {
            return new SKPoint(worldPoint.X * scaleX, worldPoint.Y * scaleY);
        }

        protected SKPoint ScaledScreenToWorld(SKPoint screenPoint)
        {
            float safeScaleX = scaleX == 0f ? 1f : scaleX;
            float safeScaleY = scaleY == 0f ? 1f : scaleY;
            return new SKPoint(screenPoint.X / safeScaleX, screenPoint.Y / safeScaleY);
        }

        internal void Resize(float Width, float Height)
        {
            if (Width <= 0 || Height <= 0)
            {
                return;
            }

            ResizePrecize(Width, Height);
            canvasWidth = Width;
            canvasHeight = Height;
        }

        protected virtual void ResizePrecize(float Width, float Height)
        {
        }

        internal void Draw(SKCanvas canvas)
        {
            DrawMainShape(canvas);
        }

        protected abstract void DrawMainShape(SKCanvas canvas);
    }
}
