using Fillsquir.Domain;
using Fillsquir.Visuals;
using SkiaSharp;

namespace tests;

[TestClass]
public class DepthPassTests
{
    private static readonly PuzzleKey PuzzleKey = new(3, 11, "v2");

    [TestMethod]
    public void QualityTiers_EnableExpectedDepthPasses()
    {
        using WorldTextureProvider provider = new();
        using PuzzleMaterialService service = new(provider);

        MaterialEffectFlags low = service.GetQualityEffects(GraphicsQualityTier.Low);
        MaterialEffectFlags medium = service.GetQualityEffects(GraphicsQualityTier.Medium);
        MaterialEffectFlags high = service.GetQualityEffects(GraphicsQualityTier.High);

        Assert.IsTrue(low.UseShadow);
        Assert.IsFalse(low.UseAmbientOcclusion);
        Assert.IsFalse(low.UseBevel);

        Assert.IsTrue(medium.UseShadow);
        Assert.IsTrue(medium.UseAmbientOcclusion);
        Assert.IsTrue(medium.UseBevel);

        Assert.IsTrue(high.UseRimHighlight);
        Assert.IsTrue(high.UseGlintOverlay);
    }

    [TestMethod]
    public void MaterialService_ProvidesDepthPassPaints()
    {
        using WorldTextureProvider provider = new();
        using PuzzleMaterialService service = new(provider);

        VisualSettings settings = new()
        {
            SelectedSkinId = "paper",
            QualityTier = GraphicsQualityTier.High,
            DepthIntensity = 0.82f
        };

        SKPaint shadowPaint = service.GetPieceShadowPaint(settings, isDragging: true, elevationMultiplier: 1.6f);
        SKPaint bevelLightPaint = service.GetPieceBevelPaint(settings, new SKRect(100f, 100f, 280f, 260f), darkPass: false);
        SKPaint bevelDarkPaint = service.GetPieceBevelPaint(settings, new SKRect(100f, 100f, 280f, 260f), darkPass: true);
        SKPaint stripPaint = service.GetStripBackgroundPaint(PuzzleKey, settings, new SKRect(0f, 700f, 1000f, 1000f));

        Assert.AreEqual(SKPaintStyle.Fill, shadowPaint.Style);
        Assert.AreEqual(SKBlendMode.Multiply, shadowPaint.BlendMode);

        Assert.AreEqual(SKPaintStyle.Stroke, bevelLightPaint.Style);
        Assert.AreEqual(SKBlendMode.Screen, bevelLightPaint.BlendMode);
        Assert.AreEqual(SKPaintStyle.Stroke, bevelDarkPaint.Style);
        Assert.AreEqual(SKBlendMode.Multiply, bevelDarkPaint.BlendMode);

        Assert.AreEqual(SKPaintStyle.Fill, stripPaint.Style);
        Assert.IsNotNull(stripPaint.Shader);
    }
}
