using Fillsquir.Domain;
using Fillsquir.Visuals;
using SkiaSharp;

namespace tests;

[TestClass]
public class TextureMappingTests
{
    private static readonly PuzzleKey Key = new(7, 1234, "v2");
    private static readonly SKRect BoardRect = new(0f, 0f, 1000f, 1000f);

    [TestMethod]
    public void WorldLockedMapping_ReusesSameSampleAtSameWorldCoordinate()
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

        SKRect pieceA = new(100f, 100f, 360f, 360f);
        SKRect pieceB = new(180f, 180f, 500f, 500f);
        SKPoint sharedSample = new(240f, 240f);

        SKColor sampleA = RenderSampleAt(service, settings, pieceA, sharedSample);
        SKColor sampleB = RenderSampleAt(service, settings, pieceB, sharedSample);

        Assert.IsTrue(ColorDistance(sampleA, sampleB) <= 2.0f);
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

        SKColor colorA = RenderSampleAt(service, settings, pieceA, sampleA);
        SKColor colorB = RenderSampleAt(service, settings, pieceB, sampleB);

        float distance = ColorDistance(colorA, colorB);
        Assert.IsTrue(distance <= 6.0f, $"Piece-local mapping drifted under translation (distance={distance}).");
    }

    private static SKColor RenderSampleAt(PuzzleMaterialService service, VisualSettings settings, SKRect pieceRect, SKPoint samplePoint)
    {
        using SKBitmap bitmap = new(1000, 1000, SKColorType.Rgba8888, SKAlphaType.Premul);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.Transparent);

        using SKPath path = new();
        path.AddRect(pieceRect);

        SKPaint fill = service.GetPieceFillPaint(
            Key,
            settings,
            BoardRect,
            pieceRect,
            forcePieceLocal: false);

        canvas.DrawPath(path, fill);

        int x = Math.Clamp((int)MathF.Round(samplePoint.X), 0, bitmap.Width - 1);
        int y = Math.Clamp((int)MathF.Round(samplePoint.Y), 0, bitmap.Height - 1);

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
}
