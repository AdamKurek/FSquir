using Fillsquir.Domain;
using Fillsquir.Visuals;

namespace tests;

[TestClass]
public class StripTransparencyTests
{
    [TestMethod]
    public void StripPaint_UsesTranslucentOpacity()
    {
        using WorldTextureProvider provider = new();
        using PuzzleMaterialService service = new(provider);

        VisualSettings settings = new()
        {
            SelectedSkinId = "nature",
            StripOpacity = 0.52f,
            StripFrostAmount = 0.40f
        };

        var stripPaint = service.GetStripBackgroundPaint(
            new PuzzleKey(12, 3, "v2"),
            settings,
            new SkiaSharp.SKRect(0f, 720f, 1000f, 1000f));

        Assert.IsNotNull(stripPaint.Shader);
        Assert.IsTrue(stripPaint.Color.Alpha < byte.MaxValue);
        Assert.IsTrue(stripPaint.Color.Alpha > 0);
    }

    [TestMethod]
    public void StripDivider_UsesSemiTransparentColor()
    {
        using WorldTextureProvider provider = new();
        using PuzzleMaterialService service = new(provider);

        VisualSettings settings = new()
        {
            SelectedSkinId = "neon",
            DepthIntensity = 0.72f,
            StripOpacity = 0.61f
        };

        var dividerPaint = service.GetStripDividerPaint(settings);

        Assert.IsTrue(dividerPaint.Color.Alpha < byte.MaxValue);
        Assert.IsTrue(dividerPaint.Color.Alpha > 0);
    }
}
