using Fillsquir.Controls;
using SkiaSharp;

namespace tests;

[TestClass]
public class GenerationTests
{
    [TestMethod]
    public void WallAngleSet_UsesTenDegreeIncrements()
    {
        const int seed = 0;
        foreach (int level in new[] { 1, 16, 17, 51, 52, 100 })
        {
            int wallAngles = WallAngleSet.WallAnglesForLevel(level);
            Assert.IsTrue(wallAngles is WallAngleSet.EasyWallAngles or WallAngleSet.MediumWallAngles or WallAngleSet.HardWallAngles);

            float rotation = WallAngleSet.RotationForLevel(seed, level, wallAngles);
            AssertAngleMultipleOfTenDegrees(rotation);

            var undirected = WallAngleSet.UndirectedDirections(wallAngles, rotation);
            foreach (var dir in undirected)
            {
                AssertAngleMultipleOfTenDegrees(MathF.Atan2(dir.Y, dir.X));
            }
        }
    }

    [TestMethod]
    public void LevelGeneration_UsesSingleWallAngleSet_PerLevel()
    {
        foreach (int level in new[] { 1, 10, 50, 100 })
        {
            const int seed = 0;
            int wallAngles = WallAngleSet.WallAnglesForLevel(level);
            float rotation = WallAngleSet.RotationForLevel(seed, level, wallAngles);
            var undirected = WallAngleSet.UndirectedDirections(wallAngles, rotation);
            var directed = WallAngleSet.DirectedDirections(undirected);

            var rand = new Random(seed);
            var mainShape = SquirGenerator.GenerateMainShape(undirected, rand);
            FSMath.FitShapeUniform(ref mainShape, 1000, 1000, padding: 40f);

            Assert.IsTrue(FSMath.IsSimplePolygon(mainShape));
            AssertUsesOnlyWallDirections(mainShape, undirected);

            float mainArea = FSMath.CalculateArea(mainShape);
            float minArea = mainArea / 42f;
            float maxArea = mainArea / 7f;

            var pieces = SquirGenerator.GenerateFragments(fragmentCount: 20, directed, level, minArea, maxArea, rand);
            Assert.AreEqual(20, pieces.Count);

            for (int pieceIndex = 0; pieceIndex < pieces.Count; pieceIndex++)
            {
                var piece = pieces[pieceIndex];
                Assert.IsTrue(FSMath.IsSimplePolygon(piece), $"Generated piece is not simple (level={level}, piece={pieceIndex}, vertices={piece.Length}).");
                AssertUsesOnlyWallDirections(piece, undirected);
            }
        }
    }

    [TestMethod]
    public void Level91_DoesNotGenerateSelfIntersectingPolygons()
    {
        const int seed = 0;
        const int level = 91;
        const int fragments = level;

        int wallAngles = WallAngleSet.WallAnglesForLevel(level);
        float rotation = WallAngleSet.RotationForLevel(seed, level, wallAngles);
        var undirected = WallAngleSet.UndirectedDirections(wallAngles, rotation);
        var directed = WallAngleSet.DirectedDirections(undirected);

        var rand = new Random(seed);
        var mainShape = SquirGenerator.GenerateMainShape(undirected, rand);
        FSMath.FitShapeUniform(ref mainShape, 1000, 1000, padding: 40f);

        Assert.IsTrue(FSMath.IsSimplePolygon(mainShape));
        AssertUsesOnlyWallDirections(mainShape, undirected);

        float mainArea = FSMath.CalculateArea(mainShape);
        float minArea = mainArea / 42f;
        float maxArea = mainArea / 7f;

        var pieces = SquirGenerator.GenerateFragments(fragments, directed, level, minArea, maxArea, rand);
        Assert.AreEqual(fragments, pieces.Count);

        for (int i = 0; i < pieces.Count; i++)
        {
            Assert.IsTrue(FSMath.IsSimplePolygon(pieces[i]), $"Piece {i} is self-intersecting.");
            AssertUsesOnlyWallDirections(pieces[i], undirected);
        }
    }

    private static void AssertUsesOnlyWallDirections(SKPoint[] polygon, SKPoint[] allowedUndirectedDirections)
    {
        const float tolerance = 1e-3f;

        for (int i = 0; i < polygon.Length; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Length];
            var edge = new SKPoint(b.X - a.X, b.Y - a.Y);

            float length = MathF.Sqrt(edge.X * edge.X + edge.Y * edge.Y);
            Assert.IsTrue(length > 0f);

            var unit = new SKPoint(edge.X / length, edge.Y / length);

            float best = 0f;
            foreach (var allowed in allowedUndirectedDirections)
            {
                float dot = MathF.Abs((unit.X * allowed.X) + (unit.Y * allowed.Y));
                best = Math.Max(best, dot);
            }

            Assert.IsTrue(best >= 1f - tolerance, $"Edge {i} direction does not match allowed walls (best dot={best}).");
        }
    }

    private static void AssertAngleMultipleOfTenDegrees(float radians)
    {
        const double toleranceDegrees = 1e-3;
        double degrees = radians * (180.0 / Math.PI);
        degrees %= 360.0;
        if (degrees < 0)
        {
            degrees += 360.0;
        }

        double mod = degrees % 10.0;
        double distance = Math.Min(mod, 10.0 - mod);
        Assert.IsTrue(distance <= toleranceDegrees, $"Angle {degrees} deg is not a multiple of 10 deg (off by {distance} deg).");
    }

}
