using SkiaSharp;

namespace Fillsquir.Services;

public interface ICoordinateTransformer
{
    float MapScaleX(float canvasWidth);
    float MapScaleY(float canvasHeight);
    SKPoint WorldToScreen(SKPoint world, float canvasWidth, float canvasHeight, float zoomFactor, SKPoint cameraOffsetWorld);
    SKPoint ScreenToWorld(SKPoint screen, float canvasWidth, float canvasHeight, float zoomFactor, SKPoint cameraOffsetWorld);
}
