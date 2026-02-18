using Fillsquir.Visuals;
using SkiaSharp;

namespace tests;

[TestClass]
public class OutlineToggleTests
{
    [TestMethod]
    public void OutlinePaint_ChangesThicknessAndContrast_WhenToggleChanges()
    {
        using WorldTextureProvider provider = new();
        using PuzzleMaterialService service = new(provider);

        VisualSettings strongSettings = new()
        {
            SelectedSkinId = "paper",
            ShowStrongOutlines = true
        };

        VisualSettings softSettings = new()
        {
            SelectedSkinId = "paper",
            ShowStrongOutlines = false
        };

        SKPaint strong = service.GetOutlinePaint(strongSettings);
        SKPaint soft = service.GetOutlinePaint(softSettings);
        SkinDefinition skin = SkinCatalog.Resolve("paper");

        float strongContrast = ColorDistance(strong.Color, skin.PieceBaseColor);
        float softContrast = ColorDistance(soft.Color, skin.PieceBaseColor);

        Assert.IsTrue(strong.StrokeWidth > soft.StrokeWidth);
        Assert.IsTrue(strongContrast > softContrast);
    }

    private static float ColorDistance(SKColor a, SKColor b)
    {
        float dr = a.Red - b.Red;
        float dg = a.Green - b.Green;
        float db = a.Blue - b.Blue;
        return MathF.Sqrt((dr * dr) + (dg * dg) + (db * db));
    }
}
