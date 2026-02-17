using Fillsquir.Controls;
using SkiaSharp;

namespace tests;

[TestClass]
public class ScoreAndTransformTests
{
    [TestMethod]
    public void CoordinateMath_RoundTripsWorldAndScreen()
    {
        SKPoint world = new(421.25f, 133.75f);
        SKPoint cameraOffset = new(12.5f, -7.25f);
        float canvasWidth = 1920f;
        float canvasHeight = 1080f;
        float zoomFactor = 1.65f;

        SKPoint screen = CoordinateMath.WorldToScreen(
            world,
            canvasWidth,
            canvasHeight,
            1000f,
            1000f,
            zoomFactor,
            cameraOffset);

        SKPoint roundTrip = CoordinateMath.ScreenToWorld(
            screen,
            canvasWidth,
            canvasHeight,
            1000f,
            1000f,
            zoomFactor,
            cameraOffset);

        Assert.AreEqual(world.X, roundTrip.X, 0.001f);
        Assert.AreEqual(world.Y, roundTrip.Y, 0.001f);
    }

    [TestMethod]
    public void ScoreMath_ComputesStarsAtThresholdBoundaries()
    {
        decimal worldRecord = 80m;

        int oneStar = ScoreMath.ComputeStars(72m, worldRecord, null, 0.90m, 0.95m, 0.98m);
        int twoStar = ScoreMath.ComputeStars(76m, worldRecord, null, 0.90m, 0.95m, 0.98m);
        int threeStar = ScoreMath.ComputeStars(78.4m, worldRecord, null, 0.90m, 0.95m, 0.98m);

        Assert.AreEqual(1, oneStar);
        Assert.AreEqual(2, twoStar);
        Assert.AreEqual(3, threeStar);
    }
}
