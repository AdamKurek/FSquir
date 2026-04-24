using Fillsquir.Domain;
using Fillsquir.Visuals;
using SkiaSharp;

namespace tests;

[TestClass]
public class TextureMappingTests
{
    private const float WorldLockedPieceTolerance = 18.0f;
    private const float BoardTranslationTolerance = 8.0f;

    private static readonly PuzzleKey Key = new(7, 1234, "v2");
    private static readonly SKRect BoardRect = new(0f, 0f, 1000f, 1000f);

    [TestMethod]
    public void WorldLockedMapping_ReusesSameSampleForSameSourceRegionAfterPieceTranslation()
    {
        using WorldTextureProvider provider = new();
        using PuzzleMaterialService service = new(provider);

        VisualSettings settings = new()
        {
            SelectedSkinId = "paper",
            QualityTier = GraphicsQualityTier.Medium,
            MappingMode = TextureMappingMode.WorldLocked,
            ShowStrongOutlines = true
        };

        SKRect sourceRect = new(140f, 160f, 380f, 400f);
        SKRect pieceA = new(140f, 160f, 380f, 400f);
        SKRect pieceB = new(560f, 240f, 800f, 480f);
        SKPoint sampleA = PointAt(pieceA, 0.42f, 0.57f);
        SKPoint sampleB = PointAt(pieceB, 0.42f, 0.57f);

        SKColor colorA = RenderSampleAt(service, settings, BoardRect, sourceRect, pieceA, sampleA);
        SKColor colorB = RenderSampleAt(service, settings, BoardRect, sourceRect, pieceB, sampleB);

        Assert.IsTrue(
            ColorDistance(colorA, colorB) <= WorldLockedPieceTolerance,
            $"World-locked piece mapping drifted after translation (distance={ColorDistance(colorA, colorB)}).");
    }

    [TestMethod]
    public void PieceLocalMapping_RemainsStableUnderPieceTranslation()
    {
        using WorldTextureProvider provider = new();
        using PuzzleMaterialService service = new(provider);

        VisualSettings settings = new()
        {
            SelectedSkinId = "nature",
            QualityTier = GraphicsQualityTier.Low,
            MappingMode = TextureMappingMode.PieceLocal,
            ShowStrongOutlines = true
        };

        SKRect pieceA = new(120f, 180f, 280f, 340f);
        SKRect pieceB = new(420f, 180f, 580f, 340f);

        SKPoint sampleA = new((pieceA.Left + pieceA.Right) * 0.5f, (pieceA.Top + pieceA.Bottom) * 0.5f);
        SKPoint sampleB = new((pieceB.Left + pieceB.Right) * 0.5f, (pieceB.Top + pieceB.Bottom) * 0.5f);

        SKColor colorA = RenderSampleAt(service, settings, BoardRect, pieceA, pieceA, sampleA);
        SKColor colorB = RenderSampleAt(service, settings, BoardRect, pieceB, pieceB, sampleB);

        float distance = ColorDistance(colorA, colorB);
        Assert.IsTrue(distance <= 6.0f, $"Piece-local mapping drifted under translation (distance={distance}).");
    }

    [TestMethod]
    public void BoardMapping_RemainsStableUnderCameraTranslation()
    {
        using WorldTextureProvider provider = new();
        using PuzzleMaterialService service = new(provider);

        VisualSettings settings = new()
        {
            SelectedSkinId = "paper",
            QualityTier = GraphicsQualityTier.Medium,
            MappingMode = TextureMappingMode.WorldLocked
        };

        SKPoint worldSample = new(240f, 340f);

        SKColor sampleA = RenderInsetBoardSampleAt(service, settings, zoomFactor: 1f, cameraOffset: new SKPoint(0f, 0f), worldSample);
        SKColor sampleB = RenderInsetBoardSampleAt(service, settings, zoomFactor: 1f, cameraOffset: new SKPoint(180f, 120f), worldSample);

        Assert.IsTrue(
            ColorDistance(sampleA, sampleB) <= BoardTranslationTolerance,
            $"Board mapping drifted under translation (distance={ColorDistance(sampleA, sampleB)}).");
    }

    [TestMethod]
    public void BoardMapping_RemainsStableUnderCameraZoom()
    {
        using WorldTextureProvider provider = new();
        using PuzzleMaterialService service = new(provider);

        VisualSettings settings = new()
        {
            SelectedSkinId = "nature",
            QualityTier = GraphicsQualityTier.Medium,
            MappingMode = TextureMappingMode.WorldLocked
        };

        SKPoint worldSample = new(320f, 260f);

        SKColor sampleA = RenderInsetBoardSampleAt(service, settings, zoomFactor: 1f, cameraOffset: new SKPoint(80f, 60f), worldSample);
        SKColor sampleB = RenderInsetBoardSampleAt(service, settings, zoomFactor: 1.8f, cameraOffset: new SKPoint(80f, 60f), worldSample);

        Assert.IsTrue(
            ColorDistance(sampleA, sampleB) <= 2.0f,
            $"Board mapping drifted under zoom (distance={ColorDistance(sampleA, sampleB)}).");
    }

    [TestMethod]
    public void BoardMapping_RemainsStableUnderCameraTranslationWhileZoomed()
    {
        using WorldTextureProvider provider = new();
        using PuzzleMaterialService service = new(provider);

        VisualSettings settings = new()
        {
            SelectedSkinId = "paper",
            QualityTier = GraphicsQualityTier.Medium,
            MappingMode = TextureMappingMode.WorldLocked
        };

        SKPoint worldSample = new(240f, 340f);

        SKColor sampleA = RenderInsetBoardSampleAt(service, settings, zoomFactor: 1.8f, cameraOffset: new SKPoint(40f, 20f), worldSample);
        SKColor sampleB = RenderInsetBoardSampleAt(service, settings, zoomFactor: 1.8f, cameraOffset: new SKPoint(220f, 140f), worldSample);

        Assert.IsTrue(
            ColorDistance(sampleA, sampleB) <= BoardTranslationTolerance,
            $"Board mapping drifted under translation while zoomed (distance={ColorDistance(sampleA, sampleB)}).");
    }

    private static SKColor RenderSampleAt(
        PuzzleMaterialService service,
        VisualSettings settings,
        SKRect textureRect,
        SKRect sourceRect,
        SKRect surfaceRect,
        SKPoint samplePoint)
    {
        using SKBitmap bitmap = new(1000, 1000, SKColorType.Rgba8888, SKAlphaType.Premul);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.Transparent);

        SKPaint fill = service.GetPieceFillPaint(
            Key,
            settings,
            textureRect,
            sourceRect,
            surfaceRect,
            surfaceRect,
            forcePieceLocal: false);
        fill.IsAntialias = false;

        canvas.DrawRect(surfaceRect, fill);

        int x = Math.Clamp((int)MathF.Round(samplePoint.X), 0, bitmap.Width - 1);
        int y = Math.Clamp((int)MathF.Round(samplePoint.Y), 0, bitmap.Height - 1);

        return bitmap.GetPixel(x, y);
    }

    private static SKColor RenderBoardSampleAt(
        PuzzleMaterialService service,
        VisualSettings settings,
        float zoomFactor,
        SKPoint cameraOffset,
        SKPoint worldSample)
    {
        using SKBitmap bitmap = new(2400, 2400, SKColorType.Rgba8888, SKAlphaType.Premul);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.Transparent);

        SKRect textureRect = BoardRect;
        SKRect sourceRect = BoardRect;
        SKRect geometryRect = new(
            cameraOffset.X,
            cameraOffset.Y,
            cameraOffset.X + BoardRect.Width,
            cameraOffset.Y + BoardRect.Height);
        using SKShader shader = service.GetBoardShader(Key, settings, textureRect, sourceRect, geometryRect, geometryRect);
        using SKPaint fill = new()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = false,
            Shader = shader,
            Color = SKColors.White
        };

        canvas.Save();
        canvas.Scale(zoomFactor);
        canvas.DrawRect(geometryRect, fill);
        canvas.Restore();

        int x = Math.Clamp((int)MathF.Round((cameraOffset.X + worldSample.X) * zoomFactor), 0, bitmap.Width - 1);
        int y = Math.Clamp((int)MathF.Round((cameraOffset.Y + worldSample.Y) * zoomFactor), 0, bitmap.Height - 1);

        return bitmap.GetPixel(x, y);
    }

    private static SKColor RenderInsetBoardSampleAt(
        PuzzleMaterialService service,
        VisualSettings settings,
        float zoomFactor,
        SKPoint cameraOffset,
        SKPoint worldSample)
    {
        using SKBitmap bitmap = new(2400, 2400, SKColorType.Rgba8888, SKAlphaType.Premul);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.Transparent);

        SKRect boardTextureRect = new(
            0f,
            0f,
            BoardRect.Width,
            BoardRect.Height);
        SKRect boardSourceRect = new(
            40f,
            40f,
            BoardRect.Width - 40f,
            BoardRect.Height - 40f);
        SKRect boardSurfaceRect = new(
            cameraOffset.X + 40f,
            cameraOffset.Y + 40f,
            cameraOffset.X + BoardRect.Width - 40f,
            cameraOffset.Y + BoardRect.Height - 40f);

        using SKShader shader = service.GetBoardShader(Key, settings, boardTextureRect, boardSourceRect, boardSurfaceRect, boardSurfaceRect);
        using SKPaint fill = new()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = false,
            Shader = shader,
            Color = SKColors.White
        };

        canvas.Save();
        canvas.Scale(zoomFactor);
        canvas.DrawRect(boardSurfaceRect, fill);
        canvas.Restore();

        int x = Math.Clamp((int)MathF.Round((cameraOffset.X + worldSample.X) * zoomFactor), 0, bitmap.Width - 1);
        int y = Math.Clamp((int)MathF.Round((cameraOffset.Y + worldSample.Y) * zoomFactor), 0, bitmap.Height - 1);

        return bitmap.GetPixel(x, y);
    }

    private static float ColorDistance(SKColor a, SKColor b)
    {
        float dr = a.Red - b.Red;
        float dg = a.Green - b.Green;
        float db = a.Blue - b.Blue;
        float da = a.Alpha - b.Alpha;
        return MathF.Sqrt((dr * dr) + (dg * dg) + (db * db) + (da * da));
    }

    private static SKPoint PointAt(SKRect rect, float normalizedX, float normalizedY)
    {
        return new SKPoint(
            rect.Left + (rect.Width * normalizedX),
            rect.Top + (rect.Height * normalizedY));
    }
}
