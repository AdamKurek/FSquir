using SkiaSharp;

namespace Fillsquir.Controls;

internal static class WallAngleSet
{
    // All wall directions live on a 10-degree grid so pieces can only be "off" by 10-degree increments.
    internal const int AngleStepDegrees = 10;
    internal const int AngleStepCountHalfTurn = 180 / AngleStepDegrees; // 18 (0..170)

    // These map to 30/20/10 degree steps (all multiples of 10 degrees).
    internal const int EasyWallAngles = AngleStepCountHalfTurn / 3; // 6
    internal const int MediumWallAngles = AngleStepCountHalfTurn / 2; // 9
    internal const int HardWallAngles = AngleStepCountHalfTurn; // 18

    internal const int MaxLevel = 100;

    internal static int WallAnglesForLevel(int level)
    {
        // Keep a simple difficulty ramp, but only allow steps that are multiples of 10 degrees.
        // Levels 1..16: 30-degree grid (6 undirected directions)
        // Levels 17..51: 20-degree grid (9 undirected directions)
        // Levels 52..MaxLevel: 10-degree grid (18 undirected directions)
        if (level <= 16)
        {
            return EasyWallAngles;
        }

        if (level <= 51)
        {
            return MediumWallAngles;
        }

        return HardWallAngles;
    }

    internal static float RotationForLevel(int seed, int level, int wallAngleCount)
    {
        if (wallAngleCount <= 0)
        {
            return 0f;
        }

        int mixedSeed = seed;
        mixedSeed = unchecked(mixedSeed * 397) ^ level;
        mixedSeed = unchecked(mixedSeed * 397) ^ wallAngleCount;

        var rand = new Random(mixedSeed);

        // Rotation is restricted to 10-degree increments (and must remain < step to keep angles in [0, pi)).
        double baseStep = Math.PI / AngleStepCountHalfTurn; // 10 degrees
        double step = Math.PI / wallAngleCount;

        int rotationSteps = (int)Math.Floor(step / baseStep);
        if (rotationSteps <= 0)
        {
            return 0f;
        }

        return (float)(rand.Next(rotationSteps) * baseStep);
    }

    internal static SKPoint[] UndirectedDirections(int wallAngleCount, float rotationRadians)
    {
        if (wallAngleCount <= 0)
        {
            return Array.Empty<SKPoint>();
        }

        SKPoint[] directions = new SKPoint[wallAngleCount];
        double step = Math.PI / wallAngleCount;

        for (int i = 0; i < wallAngleCount; i++)
        {
            double angle = rotationRadians + i * step;
            directions[i] = Normalize(new SKPoint((float)Math.Cos(angle), (float)Math.Sin(angle)));
        }

        return directions;
    }

    internal static SKPoint[] DirectedDirections(SKPoint[] undirectedDirections)
    {
        List<SKPoint> directed = new(capacity: undirectedDirections.Length * 2);

        foreach (var dir in undirectedDirections)
        {
            if (dir.X == 0f && dir.Y == 0f)
            {
                continue;
            }

            directed.Add(dir);
            directed.Add(new SKPoint(-dir.X, -dir.Y));
        }

        directed.Sort((a, b) => Math.Atan2(a.Y, a.X).CompareTo(Math.Atan2(b.Y, b.X)));
        return directed.ToArray();
    }

    private static SKPoint Normalize(SKPoint vector)
    {
        float magnitude = MathF.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
        if (magnitude == 0f || float.IsNaN(magnitude) || float.IsInfinity(magnitude))
        {
            return new SKPoint(0f, 0f);
        }

        return new SKPoint(vector.X / magnitude, vector.Y / magnitude);
    }
}
