using Fillsquir.Controls;
using SkiaSharp;

namespace tests;

[TestClass]
public class FSMathTests
{
    [TestMethod]
    public void GetFurtherstDirectionVector_ReturnsOrthogonalEdge_ForHorizontalDirection()
    {
        SKPoint[] shape =
        [
            new(0, 0),
            new(0, 100),
            new(100, 100),
            new(100, 0),
        ];

        var result = FSMath.GetFurtherstDirectionVector(shape, new SKPoint(1, 0));

        Assert.AreEqual(0f, result.X, 0.0001f);
        Assert.AreEqual(1f, result.Y, 0.0001f);
    }

    [TestMethod]
    public void GetFurtherstDirectionVector_ReturnsOrthogonalEdge_ForVerticalDirection()
    {
        SKPoint[] shape =
        [
            new(0, 0),
            new(0, 100),
            new(100, 100),
            new(100, 0),
        ];

        var result = FSMath.GetFurtherstDirectionVector(shape, new SKPoint(0, 1));

        Assert.AreEqual(1f, result.X, 0.0001f);
        Assert.AreEqual(0f, result.Y, 0.0001f);
    }
}
