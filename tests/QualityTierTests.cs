using Fillsquir.Visuals;

namespace tests;

[TestClass]
public class QualityTierTests
{
    [TestMethod]
    public void PuzzleMaterialService_ReturnsExpectedEffectFlagsPerTier()
    {
        using WorldTextureProvider provider = new();
        using PuzzleMaterialService service = new(provider);

        MaterialEffectFlags low = service.GetQualityEffects(GraphicsQualityTier.Low);
        MaterialEffectFlags medium = service.GetQualityEffects(GraphicsQualityTier.Medium);
        MaterialEffectFlags high = service.GetQualityEffects(GraphicsQualityTier.High);

        Assert.AreEqual(new MaterialEffectFlags(true, false, true, false, false, false, false), low);
        Assert.AreEqual(new MaterialEffectFlags(true, true, true, true, true, false, false), medium);
        Assert.AreEqual(new MaterialEffectFlags(true, true, true, true, true, true, true), high);

        Assert.IsTrue(medium.UseAmbientOcclusion);
        Assert.IsTrue(medium.UseBevel);
        Assert.IsTrue(high.UseRimHighlight);
        Assert.IsTrue(high.UseGlintOverlay);
    }
}
