using Fillsquir.Controls;
using Fillsquir.Interfaces;
using SkiaSharp;

namespace Fillsquir.Services;

internal sealed class CoordinateTransformer : ICoordinateTransformer
{
    public float MapScaleX(float canvasWidth)
    {
        return CoordinateMath.MapScaleX(canvasWidth, GeometryElement.defaultCanvasWidth);
    }

    public float MapScaleY(float canvasHeight)
    {
        return CoordinateMath.MapScaleY(canvasHeight, GeometryElement.defaultCanvasHeight);
    }

    public SKPoint WorldToScreen(SKPoint world, float canvasWidth, float canvasHeight, float zoomFactor, SKPoint cameraOffsetWorld)
    {
        return CoordinateMath.WorldToScreen(
            world,
            canvasWidth,
            canvasHeight,
            GeometryElement.defaultCanvasWidth,
            GeometryElement.defaultCanvasHeight,
            zoomFactor,
            cameraOffsetWorld);
    }

    public SKPoint ScreenToWorld(SKPoint screen, float canvasWidth, float canvasHeight, float zoomFactor, SKPoint cameraOffsetWorld)
    {
        return CoordinateMath.ScreenToWorld(
            screen,
            canvasWidth,
            canvasHeight,
            GeometryElement.defaultCanvasWidth,
            GeometryElement.defaultCanvasHeight,
            zoomFactor,
            cameraOffsetWorld);
    }
}
