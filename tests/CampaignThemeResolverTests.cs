using Fillsquir.Campaign;

namespace tests;

[TestClass]
public class CampaignThemeResolverTests
{
    [TestMethod]
    public void Resolve_IsStableForTheSameSectionIndex()
    {
        CampaignSectionTheme first = CampaignThemeResolver.Resolve(3);
        CampaignSectionTheme second = CampaignThemeResolver.Resolve(3);

        Assert.AreEqual(first, second);
    }

    [TestMethod]
    public void Resolve_IncreasesDramaMonotonically()
    {
        double previousDrama = double.MinValue;
        double previousOverlay = double.MinValue;
        double previousDensity = double.MinValue;

        for (int sectionIndex = 0; sectionIndex < 12; sectionIndex++)
        {
            CampaignSectionTheme theme = CampaignThemeResolver.Resolve(sectionIndex);

            Assert.IsTrue(theme.Drama >= previousDrama, $"Drama regressed at section {sectionIndex}.");
            Assert.IsTrue(theme.OverlayIntensity >= previousOverlay, $"Overlay intensity regressed at section {sectionIndex}.");
            Assert.IsTrue(theme.DecorativeDensity >= previousDensity, $"Decorative density regressed at section {sectionIndex}.");

            previousDrama = theme.Drama;
            previousOverlay = theme.OverlayIntensity;
            previousDensity = theme.DecorativeDensity;
        }
    }
}
