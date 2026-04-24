using Clipper2Lib;
using SkiaSharp;

namespace Fillsquir.Controls;

public static class ScoreProofVerifier
{
    public const decimal CoverageTolerancePercent = 0.01m;
    private const float MaxWorldCoordinateMagnitude = 10000f;

    public static ScoreProofVerificationResult VerifyCoverage(
        int level,
        int seed,
        decimal claimedCoveragePercent,
        IReadOnlyList<ScoreProofFragment> placedFragments)
    {
        if (!TryComputeCoveragePercent(level, seed, placedFragments, out decimal verifiedCoveragePercent, out string? failureReason))
        {
            return ScoreProofVerificationResult.Invalid(failureReason ?? "Invalid score proof.");
        }

        decimal delta = Math.Abs(verifiedCoveragePercent - claimedCoveragePercent);
        if (delta > CoverageTolerancePercent)
        {
            return ScoreProofVerificationResult.Invalid(
                $"Claimed coverage differs from verified coverage by {delta:F4}%.",
                verifiedCoveragePercent);
        }

        return ScoreProofVerificationResult.Valid(verifiedCoveragePercent);
    }

    public static bool TryComputeCoveragePercent(
        int level,
        int seed,
        IReadOnlyList<ScoreProofFragment> placedFragments,
        out decimal coveragePercent,
        out string? failureReason)
    {
        coveragePercent = 0m;
        failureReason = null;

        if (level < 1 || level > WallAngleSet.MaxLevel)
        {
            failureReason = "Invalid level.";
            return false;
        }

        placedFragments ??= Array.Empty<ScoreProofFragment>();

        try
        {
            PuzzleGeometry geometry = GeneratePuzzleGeometry(level, seed);
            if (placedFragments.Count > geometry.Fragments.Count)
            {
                failureReason = "Too many placed fragments.";
                return false;
            }

            HashSet<int> usedFragmentIndexes = new();
            List<SKPoint[]> placedShapes = new(capacity: placedFragments.Count);

            foreach (ScoreProofFragment placed in placedFragments)
            {
                if (!placed.WasTouched)
                {
                    continue;
                }

                if (placed.FragmentIndex < 0 || placed.FragmentIndex >= geometry.Fragments.Count)
                {
                    failureReason = "Fragment index is out of range.";
                    return false;
                }

                if (!usedFragmentIndexes.Add(placed.FragmentIndex))
                {
                    failureReason = "Duplicate fragment index.";
                    return false;
                }

                if (!IsFiniteCoordinate(placed.PositionXWorld) || !IsFiniteCoordinate(placed.PositionYWorld))
                {
                    failureReason = "Fragment position is invalid.";
                    return false;
                }

                placedShapes.Add(PlaceFragment(geometry.Fragments[placed.FragmentIndex], placed.PositionXWorld, placed.PositionYWorld));
            }

            double coveredArea = CalculateCoveredArea(geometry.Board, placedShapes);
            coveragePercent = ScoreMath.ComputeCoveragePercent(coveredArea, geometry.BoardArea);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or OverflowException)
        {
            failureReason = "Score proof could not be verified.";
            return false;
        }
    }

    private static PuzzleGeometry GeneratePuzzleGeometry(int level, int seed)
    {
        int wallAngleCount = WallAngleSet.WallAnglesForLevel(level);
        float wallRotationRadians = WallAngleSet.RotationForLevel(seed, level, wallAngleCount);
        SKPoint[] wallDirectionsUndirected = WallAngleSet.UndirectedDirections(wallAngleCount, wallRotationRadians);
        SKPoint[] wallDirectionsDirected = WallAngleSet.DirectedDirections(wallDirectionsUndirected);

        Random rand = new(seed);
        SKPoint[] board = SquirGenerator.GenerateMainShape(wallDirectionsUndirected, rand, level);
        FSMath.FitShapeUniform(ref board, GeometryElementDefaults.CanvasWidth, GeometryElementDefaults.CanvasHeight, padding: 40f);

        float boardArea = FSMath.CalculateArea(board);
        var (minDivisor, maxDivisor) = SquirGenerator.FragmentAreaDivisorsForLevel(level);
        float minArea = boardArea / minDivisor;
        float maxArea = boardArea / maxDivisor;
        List<SKPoint[]> fragments = SquirGenerator.GenerateFragments(
            Math.Max(1, level),
            wallDirectionsDirected,
            level,
            minArea,
            maxArea,
            rand);

        return new PuzzleGeometry(board, boardArea, fragments);
    }

    private static SKPoint[] PlaceFragment(SKPoint[] fragment, float positionXWorld, float positionYWorld)
    {
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        foreach (SKPoint point in fragment)
        {
            minX = Math.Min(minX, point.X);
            minY = Math.Min(minY, point.Y);
        }

        SKPoint[] placed = new SKPoint[fragment.Length];
        for (int i = 0; i < fragment.Length; i++)
        {
            placed[i] = new SKPoint(
                fragment[i].X + positionXWorld - minX,
                fragment[i].Y + positionYWorld - minY);
        }

        return placed;
    }

    private static double CalculateCoveredArea(SKPoint[] board, IReadOnlyList<SKPoint[]> placedShapes)
    {
        if (placedShapes.Count == 0)
        {
            return 0d;
        }

        Paths64 subject = new() { FSMath.SKPointArrayToPath64(board) };
        Paths64 clip = new();
        foreach (SKPoint[] shape in placedShapes)
        {
            clip.Add(FSMath.SKPointArrayToPath64(shape));
        }

        Paths64 commonArea = Clipper.Intersect(subject, clip, FillRule.NonZero);
        List<SKPoint[]> figures = new();
        foreach (Path64 path in commonArea)
        {
            figures.AddRange(FSMath.Path64ToSKPointArrayList(path));
        }

        return CalculateCompositeArea(figures);
    }

    private static double CalculateCompositeArea(IReadOnlyList<SKPoint[]> figures)
    {
        double area = 0d;
        foreach (SKPoint[] shape in figures)
        {
            int parents = 0;
            foreach (SKPoint[] parent in figures)
            {
                bool inParent = true;
                foreach (SKPoint point in shape)
                {
                    if (!FSMath.IsPointInShape(point, parent))
                    {
                        inParent = false;
                        break;
                    }
                }

                if (inParent)
                {
                    parents++;
                }
            }

            if (parents % 2 == 0)
            {
                area -= FSMath.CalculateArea(shape);
            }
            else
            {
                area += FSMath.CalculateArea(shape);
            }
        }

        return area;
    }

    private static bool IsFiniteCoordinate(float value)
    {
        return float.IsFinite(value)
            && MathF.Abs(value) <= MaxWorldCoordinateMagnitude;
    }

    private sealed record PuzzleGeometry(SKPoint[] Board, double BoardArea, List<SKPoint[]> Fragments);
}
