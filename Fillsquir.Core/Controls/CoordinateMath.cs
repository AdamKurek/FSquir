using SkiaSharp;

namespace Fillsquir.Controls;

internal static class CoordinateMath
{
    internal static float MapScaleX(float canvasWidth, float worldWidth)
    {
        return canvasWidth / worldWidth;
    }

    internal static float MapScaleY(float canvasHeight, float worldHeight)
    {
        return canvasHeight / worldHeight;
    }

    internal static SKPoint WorldToScreen(
        SKPoint world,
        float canvasWidth,
        float canvasHeight,
        float worldWidth,
        float worldHeight,
        float zoomFactor,
        SKPoint cameraOffsetWorld)
    {
        float sx = MapScaleX(canvasWidth, worldWidth);
        float sy = MapScaleY(canvasHeight, worldHeight);

        return new SKPoint(
            ((world.X + cameraOffsetWorld.X) * sx) * zoomFactor,
            ((world.Y + cameraOffsetWorld.Y) * sy) * zoomFactor);
    }

    internal static SKPoint ScreenToWorld(
        SKPoint screen,
        float canvasWidth,
        float canvasHeight,
        float worldWidth,
        float worldHeight,
        float zoomFactor,
        SKPoint cameraOffsetWorld)
    {
        float safeZoom = zoomFactor <= 0f ? 1f : zoomFactor;
        float sx = MapScaleX(canvasWidth, worldWidth);
        float sy = MapScaleY(canvasHeight, worldHeight);
        float safeSx = sx == 0f ? 1f : sx;
        float safeSy = sy == 0f ? 1f : sy;

        return new SKPoint(
            (screen.X / (safeSx * safeZoom)) - cameraOffsetWorld.X,
            (screen.Y / (safeSy * safeZoom)) - cameraOffsetWorld.Y);
    }
}
