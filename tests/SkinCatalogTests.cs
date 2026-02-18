using Fillsquir.Visuals;

namespace tests;

[TestClass]
public class SkinCatalogTests
{
    [TestMethod]
    public void SkinCatalog_ContainsRequiredBuiltInSkins()
    {
        Assert.IsTrue(SkinCatalog.TryGet("paper", out _));
        Assert.IsTrue(SkinCatalog.TryGet("neon", out _));
        Assert.IsTrue(SkinCatalog.TryGet("nature", out _));
    }

    [TestMethod]
    public void SkinCatalog_PalettesAndParametersAreValid()
    {
        foreach (SkinDefinition skin in SkinCatalog.All)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(skin.Id));
            Assert.IsFalse(string.IsNullOrWhiteSpace(skin.DisplayName));

            Assert.IsTrue(skin.BoardColor.Alpha > 0);
            Assert.IsTrue(skin.PieceBaseColor.Alpha > 0);
            Assert.IsTrue(skin.OutlineColor.Alpha > 0);
            Assert.IsTrue(skin.HoverColor.Alpha > 0);
            Assert.IsTrue(skin.StripColor.Alpha > 0);
            Assert.IsTrue(skin.TextureLowColor.Alpha > 0);
            Assert.IsTrue(skin.TextureHighColor.Alpha > 0);
            Assert.IsTrue(skin.KeyLightColor.Alpha > 0);
            Assert.IsTrue(skin.FillLightColor.Alpha > 0);
            Assert.IsTrue(skin.ShadowColor.Alpha > 0);

            Assert.IsTrue(skin.NoiseScale > 0f);
            Assert.IsTrue(skin.Contrast > 0f);
            Assert.IsTrue(skin.VignetteIntensity >= 0f && skin.VignetteIntensity <= 1f);
            Assert.IsTrue(skin.AccentIntensity >= 0f && skin.AccentIntensity <= 1f);
            Assert.IsTrue(skin.BevelStrength > 0f);
            Assert.IsTrue(skin.ShadowStrength > 0f);
        }
    }
}
