using Clipper2Lib;
using SkiaSharp;

namespace Fillsquir.Controls;

internal static class SquirGenerator
{
    private const double ClipperScale = 16384.0;

    private static SKPoint RandomPoint(float maxX, float maxY, Random rand)
    {
        return new SKPoint(rand.NextFloat(0, maxX), rand.NextFloat(0, maxY));
    }

    public static float NextFloat(this Random rand, float min, float max)
    {
        return (float)rand.NextDouble() * (max - min) + min;
    }

    private static int PickEdgeCount(Random rand, int min, int max)
    {
        if (min >= max)
        {
            return min;
        }

        float t = (float)rand.NextDouble();
        switch (rand.Next(3))
        {
            case 0:
                t *= t;
                break;
            case 1:
                t = 1f - ((1f - t) * (1f - t));
                break;
        }

        int value = min + (int)MathF.Round(t * (max - min));
        return Math.Clamp(value, min, max);
    }

    private static int PickNotchCount(Random rand, int min, int max)
    {
        if (min <= 0 || max <= 0)
        {
            return 0;
        }

        if (max < min)
        {
            max = min;
        }

        int count = rand.Next(min, max + 1);
        if (rand.NextDouble() < 0.35)
        {
            count += rand.Next(0, 2);
        }

        if (rand.NextDouble() < 0.20)
        {
            count -= rand.Next(0, 2);
        }

        int floor = Math.Max(1, min);
        int cap = max + 2;
        return Math.Clamp(count, floor, cap);
    }

    private static (float DepthMin, float DepthMax, float WidthMin, float WidthMax) PickNotchProfile(Random rand, bool mainShape)
    {
        float depthMin = mainShape
            ? rand.NextFloat(0.12f, 0.24f)
            : rand.NextFloat(0.10f, 0.20f);

        float depthMax = mainShape
            ? rand.NextFloat(0.40f, 0.62f)
            : rand.NextFloat(0.30f, 0.52f);

        float widthMin = mainShape
            ? rand.NextFloat(0.12f, 0.24f)
            : rand.NextFloat(0.10f, 0.22f);

        float widthMax = mainShape
            ? rand.NextFloat(0.34f, 0.58f)
            : rand.NextFloat(0.30f, 0.60f);

        depthMax = MathF.Max(depthMax, depthMin + 0.05f);
        widthMax = MathF.Max(widthMax, widthMin + 0.05f);

        return (depthMin, depthMax, widthMin, widthMax);
    }

    private static (int MinEdges, int MaxEdges) PickMainEdgeRange(int wallAngles, int directedCount, Random rand)
    {
        int minEdges = 3;
        int maxEdges = Math.Min(10, directedCount);

        if (rand.NextDouble() < 0.55)
        {
            maxEdges = Math.Min(5, maxEdges);
        }

        if (wallAngles >= WallAngleSet.HardWallAngles && rand.NextDouble() < 0.70)
        {
            minEdges = Math.Min(10, directedCount);
            maxEdges = Math.Min(30, directedCount);
        }

        minEdges = Math.Min(minEdges, maxEdges);
        return (minEdges, maxEdges);
    }

    private static (int MinEdges, int MaxEdges, bool LargeTier) PickFragmentEdgeRange(int level, int directedCount, Random rand)
    {
        double tierChance = Math.Clamp((level - 20) / 60.0, 0.0, 0.55);
        bool largeTier = rand.NextDouble() < tierChance;

        if (largeTier)
        {
            int minEdges = Math.Min(10, directedCount);
            int maxEdges = Math.Min(30, directedCount);
            minEdges = Math.Min(minEdges, maxEdges);
            return (minEdges, maxEdges, true);
        }

        int minSmall = 3;
        int maxSmall = Math.Min(10, directedCount);
        if (rand.NextDouble() < 0.60)
        {
            maxSmall = Math.Min(5, maxSmall);
        }

        minSmall = Math.Min(minSmall, maxSmall);
        return (minSmall, maxSmall, false);
    }

    internal static SKPoint[] GenerateCompletelyRandomShape(int numberOfPoints, float maxX, float maxY, Random rand)
    {
        while (true)
        {
            SKPoint[] points = new SKPoint[numberOfPoints];
            for (int i = 0; i < numberOfPoints; i++)
            {
                points[i] = RandomPoint(maxX, maxY, rand);
            }

            if (FSMath.EnsureFigureDirection(ref points))
            {
                return points;
            }
        }
    }

    internal static SKPoint[] GenerateMainShape(SKPoint[] wallDirectionsUndirected, Random rand, float minEdgeLength = 80f, float maxEdgeLength = 420f)
    {
        if (wallDirectionsUndirected.Length < 2)
        {
            throw new ArgumentException("At least 2 wall directions are required.", nameof(wallDirectionsUndirected));
        }

        var wallDirectionsDirected = WallAngleSet.DirectedDirections(wallDirectionsUndirected);
        if (wallDirectionsDirected.Length < 6)
        {
            throw new ArgumentException("At least 6 directed wall directions are required.", nameof(wallDirectionsUndirected));
        }

        int wallAngles = wallDirectionsUndirected.Length;
        var (minEdges, maxEdges) = PickMainEdgeRange(wallAngles, wallDirectionsDirected.Length, rand);

        const int maxAttempts = 500;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int localMinEdges = minEdges;
            int localMaxEdges = Math.Min(wallDirectionsDirected.Length, maxEdges);
            localMinEdges = Math.Min(localMinEdges, localMaxEdges);

            int edgeCount = PickEdgeCount(rand, localMinEdges, localMaxEdges);
            edgeCount = Math.Clamp(edgeCount, 3, wallDirectionsDirected.Length);

            float lengthMin = minEdgeLength * rand.NextFloat(0.75f, 1.15f);
            float lengthMax = maxEdgeLength * rand.NextFloat(0.85f, 1.35f);
            if (lengthMax <= lengthMin + 1f)
            {
                lengthMax = lengthMin + 1f;
            }

            SKPoint[] basePolygon;
            try
            {
                basePolygon = GenerateWallWalkPolygonWithAllowedDirections(
                    wallDirectionsDirected,
                    edgeCount,
                    rand,
                    minEdgeLength: lengthMin,
                    maxEdgeLength: lengthMax);
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            if (basePolygon.Length < 3)
            {
                continue;
            }

            if (!FSMath.IsSimplePolygon(basePolygon))
            {
                continue;
            }

            float area = FSMath.CalculateArea(basePolygon);
            if (!float.IsFinite(area) || area <= 1e-3f)
            {
                continue;
            }

            var bounds = FSMath.ShapeBounds(basePolygon);
            float width = bounds.Right - bounds.Left;
            float height = bounds.Bottom - bounds.Top;
            if (!float.IsFinite(width) || !float.IsFinite(height) || width <= 0f || height <= 0f)
            {
                continue;
            }

            float minDim = MathF.Min(width, height);
            float minEdgeLen = Math.Clamp(minDim * 0.04f, 10f, 28f);
            float minThickness = Math.Clamp(minDim * 0.07f, 16f, 40f);

            if (!HasMinimumEdgeLength(basePolygon, minEdgeLen))
            {
                continue;
            }

            if (!HasMinimumThickness(basePolygon, minThickness))
            {
                continue;
            }

            return basePolygon;
        }

        // Fallback: use the same wall-walk generator with a fixed edge count.
        int fallbackEdges = Math.Clamp(maxEdges, 3, wallDirectionsDirected.Length);
        return GenerateWallWalkPolygonWithAllowedDirections(
            wallDirectionsDirected,
            fallbackEdges,
            rand,
            minEdgeLength,
            maxEdgeLength);
    }

    internal static List<SKPoint[]> GenerateFragments(
        int fragmentCount,
        SKPoint[] wallDirectionsDirected,
        int level,
        float minArea,
        float maxArea,
        Random rand)
    {
        if (fragmentCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fragmentCount));
        }

        if (wallDirectionsDirected.Length < 6)
        {
            throw new ArgumentException("At least 6 directed wall directions are required.", nameof(wallDirectionsDirected));
        }

        if (minArea <= 0f || maxArea <= 0f || minArea > maxArea)
        {
            throw new ArgumentOutOfRangeException(nameof(minArea), "Area bounds must be positive and minArea <= maxArea.");
        }

        List<SKPoint[]> shapes = new(capacity: fragmentCount);

        for (int i = 0; i < fragmentCount; i++)
        {
            var (tierMinEdges, tierMaxEdges, largeTier) = PickFragmentEdgeRange(level, wallDirectionsDirected.Length, rand);
            float targetArea;
            if (largeTier)
            {
                float floor = minArea + ((maxArea - minArea) * 0.65f);
                targetArea = rand.NextFloat(floor, maxArea);
            }
            else
            {
                float ceiling = minArea + ((maxArea - minArea) * 0.80f);
                targetArea = rand.NextFloat(minArea, ceiling);
            }

            SKPoint[] finalPoly = Array.Empty<SKPoint>();
            SKPoint[]? softFallback = null;

            const int maxPieceAttempts = 90;
            for (int attempt = 0; attempt < maxPieceAttempts; attempt++)
            {
                int localMinEdges = tierMinEdges;
                int localMaxEdges = Math.Min(wallDirectionsDirected.Length, tierMaxEdges);
                localMinEdges = Math.Min(localMinEdges, localMaxEdges);

                int edgeCount = PickEdgeCount(rand, localMinEdges, localMaxEdges);
                edgeCount = Math.Min(edgeCount, wallDirectionsDirected.Length);

                float edgeMin = 30f * rand.NextFloat(0.70f, 1.20f);
                float edgeMax = 120f * rand.NextFloat(0.80f, 1.45f);
                if (edgeMax <= edgeMin + 1f)
                {
                    edgeMax = edgeMin + 1f;
                }

                SKPoint[] basePoly;
                try
                {
                    basePoly = GenerateWallWalkPolygonWithAllowedDirections(
                        wallDirectionsDirected,
                        edgeCount,
                        rand,
                        minEdgeLength: edgeMin,
                        maxEdgeLength: edgeMax);
                }
                catch (InvalidOperationException)
                {
                    continue;
                }

                var scaled = basePoly.ToArray();
                ScaleToArea(ref scaled, targetArea);
                scaled = CleanupPolygon(scaled);
                if (scaled.Length < 3 || !FSMath.IsSimplePolygon(scaled))
                {
                    continue;
                }

                FSMath.TranslateToPositive(ref scaled);
                scaled = CleanupPolygon(scaled);
                if (scaled.Length < 3 || !FSMath.IsSimplePolygon(scaled))
                {
                    continue;
                }

                var bounds = FSMath.ShapeBounds(scaled);
                float width = bounds.Right - bounds.Left;
                float height = bounds.Bottom - bounds.Top;
                if (!float.IsFinite(width) || !float.IsFinite(height) || width <= 0f || height <= 0f)
                {
                    continue;
                }

                float minDim = MathF.Min(width, height);
                float minEdgeLen = Math.Clamp(minDim * 0.03f, 2.5f, 10f);
                float minThickness = Math.Clamp(minDim * 0.07f, 4f, 22f);

                softFallback ??= scaled;

                if (!HasMinimumEdgeLength(scaled, minEdgeLen))
                {
                    continue;
                }

                if (!HasMinimumThickness(scaled, minThickness))
                {
                    continue;
                }

                finalPoly = scaled;
                break;
            }

            if (finalPoly.Length == 0)
            {
                finalPoly = softFallback ?? throw new InvalidOperationException("Failed to generate a valid fragment polygon.");
            }

            shapes.Add(finalPoly);
        }

        return shapes;
    }

    private static bool TryCarveNotches(
        SKPoint[] polygon,
        SKPoint[] allowedDirections,
        Random rand,
        int notchCount,
        float depthFracMin,
        float depthFracMax,
        float widthFracMin,
        float widthFracMax,
        out SKPoint[] carved)
    {
        carved = polygon;
        if (polygon is null || polygon.Length < 3)
        {
            return false;
        }

        if (allowedDirections is null || allowedDirections.Length < 3)
        {
            return false;
        }

        if (notchCount <= 0)
        {
            return false;
        }

        var current = polygon;
        var (startConcave, _) = ConcavityMetrics(current, deepAngleRadians: DegreesToRadians(210f));

        for (int i = 0; i < notchCount; i++)
        {
            bool applied = false;
            const int notchAttempts = 36;
            for (int attempt = 0; attempt < notchAttempts; attempt++)
            {
                if (!TryBuildNotch(current, allowedDirections, rand, depthFracMin, depthFracMax, widthFracMin, widthFracMax, out var notch))
                {
                    continue;
                }

                var subject = new Paths64 { FSMath.SKPointArrayToPath64(current) };
                var clip = new Paths64 { FSMath.SKPointArrayToPath64(notch) };
                Paths64 diff = Clipper.Difference(subject, clip, FillRule.EvenOdd);
                if (diff.Count != 1)
                {
                    continue;
                }

                var cleaned = Clipper.TrimCollinear(Clipper.StripDuplicates(diff[0], isClosedPath: true), isOpen: false);
                if (cleaned.Count < 3)
                {
                    continue;
                }

                var next = FSMath.Path64ToSKPointArrayList(cleaned)[0];
                next = CleanupPolygon(next);
                if (next.Length < 3)
                {
                    continue;
                }

                if (!FSMath.IsSimplePolygon(next))
                {
                    continue;
                }

                float area = FSMath.CalculateArea(next);
                if (!float.IsFinite(area) || area <= 1e-3f)
                {
                    continue;
                }

                var (concaveCount, _) = ConcavityMetrics(next, deepAngleRadians: DegreesToRadians(210f));
                if (concaveCount <= startConcave)
                {
                    continue;
                }

                current = next;
                startConcave = concaveCount;
                applied = true;
                break;
            }

            if (!applied)
            {
                break;
            }
        }

        carved = current;
        return carved.Length >= 3 && FSMath.IsSimplePolygon(carved);
    }

    private static bool HasMinimumEdgeLength(SKPoint[] polygon, float minEdgeLength)
    {
        if (polygon is null || polygon.Length < 3)
        {
            return false;
        }

        if (!float.IsFinite(minEdgeLength) || minEdgeLength <= 0f)
        {
            return true;
        }

        float minSq = minEdgeLength * minEdgeLength;
        for (int i = 0; i < polygon.Length; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Length];
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            float lenSq = (dx * dx) + (dy * dy);
            if (!float.IsFinite(lenSq) || lenSq < minSq)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasMinimumThickness(SKPoint[] polygon, float minThickness)
    {
        if (polygon is null || polygon.Length < 3)
        {
            return false;
        }

        if (!float.IsFinite(minThickness) || minThickness <= 0f)
        {
            return true;
        }

        var bounds = FSMath.ShapeBounds(polygon);
        float width = bounds.Right - bounds.Left;
        float height = bounds.Bottom - bounds.Top;
        if (!float.IsFinite(width) || !float.IsFinite(height) || width <= 0f || height <= 0f)
        {
            return false;
        }

        if (minThickness >= MathF.Min(width, height))
        {
            return false;
        }

        double delta = -(double)(minThickness * 0.5f) * ClipperScale;
        var paths = new Paths64 { FSMath.SKPointArrayToPath64(polygon) };
        var eroded = Clipper.InflatePaths(
            paths,
            delta,
            JoinType.Round,
            EndType.Polygon,
            miterLimit: 2.0,
            arcTolerance: 0.0);

        if (eroded.Count != 1 || eroded[0].Count < 3)
        {
            return false;
        }

        return Math.Abs(Clipper.Area(eroded[0])) > 0.0;
    }

    private static bool TryBuildNotch(
        SKPoint[] polygon,
        SKPoint[] allowedDirections,
        Random rand,
        float depthFracMin,
        float depthFracMax,
        float widthFracMin,
        float widthFracMax,
        out SKPoint[] notch)
    {
        notch = Array.Empty<SKPoint>();
        if (polygon.Length < 3)
        {
            return false;
        }

        var bounds = FSMath.ShapeBounds(polygon);
        float width = bounds.Right - bounds.Left;
        float height = bounds.Bottom - bounds.Top;
        if (!float.IsFinite(width) || !float.IsFinite(height) || width <= 0f || height <= 0f)
        {
            return false;
        }

        float minDim = MathF.Min(width, height);
        float depth = minDim * rand.NextFloat(depthFracMin, depthFracMax);
        float notchWidth = minDim * rand.NextFloat(widthFracMin, widthFracMax);

        if (!float.IsFinite(depth) || !float.IsFinite(notchWidth) || depth <= 0f || notchWidth <= 0f)
        {
            return false;
        }

        int n = polygon.Length;
        int edgeIndex = rand.Next(n);
        var a = polygon[edgeIndex];
        var b = polygon[(edgeIndex + 1) % n];

        var edge = new SKPoint(b.X - a.X, b.Y - a.Y);
        float edgeLen = MathF.Sqrt(edge.X * edge.X + edge.Y * edge.Y);
        if (!float.IsFinite(edgeLen) || edgeLen <= 1e-3f)
        {
            return false;
        }

        float halfWidth = MathF.Min(notchWidth * 0.5f, edgeLen * 0.45f);
        if (!float.IsFinite(halfWidth) || halfWidth <= 1e-3f)
        {
            return false;
        }

        if ((halfWidth * 2f) < (depth * 0.6f))
        {
            return false;
        }

        float signedArea = SignedArea(polygon);
        int orientation = signedArea >= 0f ? 1 : -1; // +1 CCW, -1 CW

        SKPoint u = NearestDirection(edge, allowedDirections);

        // Inward normal for a CCW polygon is to the left of the edge; for CW it's to the right.
        var inwardNormal = orientation > 0
            ? new SKPoint(-edge.Y, edge.X)
            : new SKPoint(edge.Y, -edge.X);
        inwardNormal = Normalize(inwardNormal);
        if (inwardNormal.X == 0f && inwardNormal.Y == 0f)
        {
            return false;
        }

        var desired = Rotate(inwardNormal, rand.NextFloat(-0.6f, 0.6f));
        if (!TryPickDirection(desired, allowedDirections, u, rand, out SKPoint v)
            && !TryPickDirection(inwardNormal, allowedDirections, u, rand, out v))
        {
            return false;
        }

        float outer = depth * rand.NextFloat(0.20f, 0.55f);
        if (!float.IsFinite(outer) || outer <= 0f)
        {
            return false;
        }

        float t = rand.NextFloat(0.20f, 0.80f);
        var p = new SKPoint(a.X + (edge.X * t), a.Y + (edge.Y * t));

        float leftWidth = halfWidth * rand.NextFloat(0.55f, 1.0f);
        float rightWidth = halfWidth * rand.NextFloat(0.55f, 1.0f);
        if ((leftWidth + rightWidth) < (depth * 0.6f))
        {
            return false;
        }

        notch = new[]
        {
            new SKPoint(p.X - (u.X * leftWidth) - (v.X * outer), p.Y - (u.Y * leftWidth) - (v.Y * outer)),
            new SKPoint(p.X + (u.X * rightWidth) - (v.X * outer), p.Y + (u.Y * rightWidth) - (v.Y * outer)),
            new SKPoint(p.X + (u.X * rightWidth) + (v.X * depth), p.Y + (u.Y * rightWidth) + (v.Y * depth)),
            new SKPoint(p.X - (u.X * leftWidth) + (v.X * depth), p.Y - (u.Y * leftWidth) + (v.Y * depth)),
        };

        return notch.Length >= 3;
    }

    private static bool TryPickDirection(SKPoint desired, SKPoint[] allowedDirections, SKPoint disallowParallelTo, Random rand, out SKPoint picked)
    {
        picked = default;
        desired = Normalize(desired);
        if (desired.X == 0f && desired.Y == 0f)
        {
            return false;
        }

        const float parallelThreshold = 0.92f;
        float best = float.NegativeInfinity;
        SKPoint bestDir = default;
        List<(SKPoint Dir, float Score)> candidates = new();

        foreach (var dir in allowedDirections)
        {
            float parallel = MathF.Abs((dir.X * disallowParallelTo.X) + (dir.Y * disallowParallelTo.Y));
            if (parallel >= parallelThreshold)
            {
                continue;
            }

            float score = (dir.X * desired.X) + (dir.Y * desired.Y);
            if (score > best)
            {
                best = score;
                bestDir = dir;
            }

            if (score > 0.05f)
            {
                candidates.Add((dir, score));
            }
        }

        if (candidates.Count == 0)
        {
            if (best > float.NegativeInfinity)
            {
                picked = bestDir;
                return true;
            }

            return false;
        }

        candidates.Sort((a, b) => b.Score.CompareTo(a.Score));
        int topCount = Math.Min(4, candidates.Count);
        double roll = rand.NextDouble();
        int index;
        if (roll < 0.65)
        {
            index = rand.Next(topCount);
        }
        else if (roll < 0.90)
        {
            index = rand.Next(candidates.Count);
        }
        else
        {
            index = candidates.Count - 1;
        }

        picked = candidates[index].Dir;
        return true;
    }

    private static SKPoint NearestDirection(SKPoint vector, SKPoint[] allowedDirections)
    {
        vector = Normalize(vector);
        if (vector.X == 0f && vector.Y == 0f)
        {
            return allowedDirections[0];
        }

        float best = float.NegativeInfinity;
        SKPoint picked = allowedDirections[0];

        foreach (var dir in allowedDirections)
        {
            float dot = (dir.X * vector.X) + (dir.Y * vector.Y);
            if (dot > best)
            {
                best = dot;
                picked = dir;
            }
        }

        return picked;
    }

    private static SKPoint Normalize(SKPoint vector)
    {
        float len = MathF.Sqrt((vector.X * vector.X) + (vector.Y * vector.Y));
        if (!float.IsFinite(len) || len <= 1e-6f)
        {
            return new SKPoint(0f, 0f);
        }

        return new SKPoint(vector.X / len, vector.Y / len);
    }

    private static SKPoint Rotate(SKPoint vector, float radians)
    {
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);
        return new SKPoint((vector.X * cos) - (vector.Y * sin), (vector.X * sin) + (vector.Y * cos));
    }

    private static float SignedArea(SKPoint[] polygon)
    {
        double sum = 0.0;
        for (int i = 0; i < polygon.Length; i++)
        {
            int j = (i + 1) % polygon.Length;
            sum += (double)polygon[i].X * polygon[j].Y - (double)polygon[i].Y * polygon[j].X;
        }

        return (float)(0.5 * sum);
    }

    private static (int ConcaveVertices, int DeepConcaveVertices) ConcavityMetrics(SKPoint[] polygon, float deepAngleRadians)
    {
        if (polygon is null || polygon.Length < 3)
        {
            return (0, 0);
        }

        float signedArea = SignedArea(polygon);
        int orientation = signedArea >= 0f ? 1 : -1;

        int concave = 0;
        int deep = 0;

        int n = polygon.Length;
        for (int i = 0; i < n; i++)
        {
            var prev = polygon[(i - 1 + n) % n];
            var curr = polygon[i];
            var next = polygon[(i + 1) % n];

            var e1 = new SKPoint(curr.X - prev.X, curr.Y - prev.Y);
            var e2 = new SKPoint(next.X - curr.X, next.Y - curr.Y);

            float dot = (e1.X * e2.X) + (e1.Y * e2.Y);
            float cross = (e1.X * e2.Y) - (e1.Y * e2.X);

            if (!float.IsFinite(dot) || !float.IsFinite(cross))
            {
                continue;
            }

            float turn = MathF.Atan2(cross, dot);
            float interior = MathF.PI - (orientation * turn);
            if (!float.IsFinite(interior))
            {
                continue;
            }

            if ((orientation * turn) < -1e-4f)
            {
                concave++;
                if (interior >= deepAngleRadians)
                {
                    deep++;
                }
            }
        }

        return (concave, deep);
    }

    private static SKPoint[] CleanupPolygon(SKPoint[] polygon)
    {
        if (polygon is null || polygon.Length < 3)
        {
            return polygon ?? Array.Empty<SKPoint>();
        }

        List<SKPoint> points = new(capacity: polygon.Length);
        foreach (var p in polygon)
        {
            if (points.Count == 0 || points[^1] != p)
            {
                points.Add(p);
            }
        }

        if (points.Count > 1 && points[0] == points[^1])
        {
            points.RemoveAt(points.Count - 1);
        }

        if (points.Count < 3)
        {
            return points.ToArray();
        }

        const float eps = 1e-6f;
        bool removed;
        do
        {
            removed = false;
            int n = points.Count;
            for (int i = 0; i < n; i++)
            {
                var prev = points[(i - 1 + n) % n];
                var curr = points[i];
                var next = points[(i + 1) % n];

                var v1 = new SKPoint(curr.X - prev.X, curr.Y - prev.Y);
                var v2 = new SKPoint(next.X - curr.X, next.Y - curr.Y);

                float cross = (v1.X * v2.Y) - (v1.Y * v2.X);
                float dot = (v1.X * v2.X) + (v1.Y * v2.Y);

                if (MathF.Abs(cross) <= eps && dot >= 0f)
                {
                    points.RemoveAt(i);
                    removed = true;
                    break;
                }
            }
        } while (removed && points.Count >= 3);

        return points.ToArray();
    }

    private static float DegreesToRadians(float degrees) => degrees * (MathF.PI / 180f);

    private static SKPoint[] GenerateWallWalkPolygonWithAllowedDirections(
        SKPoint[] allowedDirections,
        int edgeCount,
        Random rand,
        float minEdgeLength,
        float maxEdgeLength)
    {
        if (edgeCount < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(edgeCount), "At least 3 walls are required.");
        }

        if (allowedDirections is null || allowedDirections.Length < 3)
        {
            throw new ArgumentException("At least 3 directed wall directions are required.", nameof(allowedDirections));
        }

        if (minEdgeLength <= 0f || maxEdgeLength <= 0f || minEdgeLength >= maxEdgeLength)
        {
            throw new ArgumentOutOfRangeException(nameof(minEdgeLength), "Edge length bounds must be positive and min < max.");
        }

        // Walk random walls and reject intersections.
        // For the penultimate wall, solve length so the closing wall is also an allowed direction.
        const int maxAttempts = 700;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (TryGenerateWallWalkPolygon(allowedDirections, edgeCount, rand, minEdgeLength, maxEdgeLength, out var polygon))
            {
                return polygon;
            }
        }

        throw new InvalidOperationException("Failed to generate a valid polygon from wall directions.");
    }

    private static bool TryGenerateWallWalkPolygon(
        SKPoint[] allowedDirections,
        int edgeCount,
        Random rand,
        float minEdgeLength,
        float maxEdgeLength,
        out SKPoint[] polygon)
    {
        polygon = Array.Empty<SKPoint>();

        var directions = allowedDirections
            .Where(v => !(v.X == 0f && v.Y == 0f))
            .ToArray();

        if (directions.Length < 3 || edgeCount < 3)
        {
            return false;
        }

        List<SKPoint> vertices = new(capacity: edgeCount) { new SKPoint(0f, 0f) };
        List<SKPoint> edgeDirections = new(capacity: edgeCount);

        if (!TryAddFirstWall(vertices, edgeDirections, directions, rand, minEdgeLength, maxEdgeLength))
        {
            return false;
        }

        for (int edgeIndex = 1; edgeIndex < edgeCount - 1; edgeIndex++)
        {
            bool added;
            bool isPenultimate = edgeIndex == edgeCount - 2;
            if (isPenultimate)
            {
                added = TryAddPenultimateWallForClosure(
                    vertices,
                    edgeDirections,
                    directions,
                    rand,
                    minEdgeLength,
                    maxEdgeLength);
            }
            else
            {
                added = TryAddRandomWall(
                    vertices,
                    edgeDirections,
                    directions,
                    rand,
                    minEdgeLength,
                    maxEdgeLength);
            }

            if (!added)
            {
                return false;
            }
        }

        if (vertices.Count != edgeCount)
        {
            return false;
        }

        polygon = vertices.ToArray();
        if (!FSMath.IsSimplePolygon(polygon))
        {
            return false;
        }

        float area = FSMath.CalculateArea(polygon);
        return float.IsFinite(area) && area > 1e-3f;
    }

    private static bool TryAddFirstWall(
        List<SKPoint> vertices,
        List<SKPoint> edgeDirections,
        SKPoint[] directions,
        Random rand,
        float minEdgeLength,
        float maxEdgeLength)
    {
        const int maxTrials = 64;
        SKPoint start = vertices[0];
        for (int trial = 0; trial < maxTrials; trial++)
        {
            var dir = directions[rand.Next(directions.Length)];
            float length = NextEdgeLength(rand, minEdgeLength, maxEdgeLength);
            if (!float.IsFinite(length) || length <= 0f)
            {
                continue;
            }

            var end = new SKPoint(start.X + (dir.X * length), start.Y + (dir.Y * length));
            if (!float.IsFinite(end.X) || !float.IsFinite(end.Y))
            {
                continue;
            }

            vertices.Add(end);
            edgeDirections.Add(dir);
            return true;
        }

        return false;
    }

    private static bool TryAddRandomWall(
        List<SKPoint> vertices,
        List<SKPoint> edgeDirections,
        SKPoint[] directions,
        Random rand,
        float minEdgeLength,
        float maxEdgeLength)
    {
        const int maxTrials = 140;
        SKPoint current = vertices[^1];
        SKPoint previousDirection = edgeDirections[^1];
        float minPointGap = MathF.Max(1f, minEdgeLength * 0.18f);
        float minPointGapSq = minPointGap * minPointGap;

        for (int trial = 0; trial < maxTrials; trial++)
        {
            var dir = directions[rand.Next(directions.Length)];
            if (!IsValidTurn(previousDirection, dir))
            {
                continue;
            }

            float length = NextEdgeLength(rand, minEdgeLength, maxEdgeLength);
            if (!float.IsFinite(length) || length <= 0f)
            {
                continue;
            }

            var end = new SKPoint(current.X + (dir.X * length), current.Y + (dir.Y * length));
            if (!float.IsFinite(end.X) || !float.IsFinite(end.Y))
            {
                continue;
            }

            // Avoid touching previously visited vertices.
            if (IsNearExistingVertex(vertices, end, minPointGapSq, ignoreLastVertex: true))
            {
                continue;
            }

            int previousEdgeIndex = vertices.Count - 2;
            if (DoesIntersectOpenChain(current, end, vertices, ignoreEdgeIndex: previousEdgeIndex))
            {
                continue;
            }

            vertices.Add(end);
            edgeDirections.Add(dir);
            return true;
        }

        return false;
    }

    private static bool TryAddPenultimateWallForClosure(
        List<SKPoint> vertices,
        List<SKPoint> edgeDirections,
        SKPoint[] directions,
        Random rand,
        float minEdgeLength,
        float maxEdgeLength)
    {
        const int outerTrials = 120;
        const int innerTrials = 120;

        SKPoint start = vertices[0];
        SKPoint current = vertices[^1];
        SKPoint previousDirection = edgeDirections[^1];

        float stretchMin = minEdgeLength * 0.35f;
        float stretchMax = maxEdgeLength * 2.00f;
        float minPointGap = MathF.Max(1f, minEdgeLength * 0.12f);
        float minPointGapSq = minPointGap * minPointGap;

        for (int i = 0; i < outerTrials; i++)
        {
            var penultimateDirection = directions[rand.Next(directions.Length)];
            if (!IsValidTurn(previousDirection, penultimateDirection))
            {
                continue;
            }

            for (int j = 0; j < innerTrials; j++)
            {
                var closingDirection = directions[rand.Next(directions.Length)];
                if (!IsValidTurn(penultimateDirection, closingDirection))
                {
                    continue;
                }

                float det = Cross(penultimateDirection, closingDirection);
                if (MathF.Abs(det) < 1e-4f)
                {
                    continue;
                }

                var toStart = new SKPoint(start.X - current.X, start.Y - current.Y);

                float penultimateLength = ((toStart.X * closingDirection.Y) - (toStart.Y * closingDirection.X)) / det;
                float closingLength = ((penultimateDirection.X * toStart.Y) - (penultimateDirection.Y * toStart.X)) / det;

                if (!float.IsFinite(penultimateLength) || !float.IsFinite(closingLength))
                {
                    continue;
                }

                if (penultimateLength <= 0f || closingLength <= 0f)
                {
                    continue;
                }

                if (penultimateLength < stretchMin || penultimateLength > stretchMax)
                {
                    continue;
                }

                if (closingLength < stretchMin || closingLength > stretchMax)
                {
                    continue;
                }

                var penultimateEnd = new SKPoint(
                    current.X + (penultimateDirection.X * penultimateLength),
                    current.Y + (penultimateDirection.Y * penultimateLength));

                if (!float.IsFinite(penultimateEnd.X) || !float.IsFinite(penultimateEnd.Y))
                {
                    continue;
                }

                if (IsNearExistingVertex(vertices, penultimateEnd, minPointGapSq, ignoreLastVertex: true))
                {
                    continue;
                }

                int previousEdgeIndex = vertices.Count - 2;
                if (DoesIntersectOpenChain(current, penultimateEnd, vertices, ignoreEdgeIndex: previousEdgeIndex))
                {
                    continue;
                }

                // Closing edge is penultimateEnd -> start. Ignore edge 0 because it is adjacent at start.
                if (DoesIntersectOpenChain(penultimateEnd, start, vertices, ignoreEdgeIndex: 0))
                {
                    continue;
                }

                // Final verification catches any touching cases we intentionally allowed above.
                var candidate = new SKPoint[vertices.Count + 1];
                for (int idx = 0; idx < vertices.Count; idx++)
                {
                    candidate[idx] = vertices[idx];
                }
                candidate[^1] = penultimateEnd;

                if (!FSMath.IsSimplePolygon(candidate))
                {
                    continue;
                }

                float area = FSMath.CalculateArea(candidate);
                if (!float.IsFinite(area) || area <= 1e-3f)
                {
                    continue;
                }

                vertices.Add(penultimateEnd);
                edgeDirections.Add(penultimateDirection);
                return true;
            }
        }

        return false;
    }

    private static bool IsNearExistingVertex(
        List<SKPoint> vertices,
        SKPoint point,
        float minDistanceSquared,
        bool ignoreLastVertex)
    {
        int maxIndex = ignoreLastVertex ? vertices.Count - 1 : vertices.Count;
        for (int i = 0; i < maxIndex; i++)
        {
            float dx = point.X - vertices[i].X;
            float dy = point.Y - vertices[i].Y;
            float dstSq = (dx * dx) + (dy * dy);
            if (dstSq < minDistanceSquared)
            {
                return true;
            }
        }

        return false;
    }

    private static bool DoesIntersectOpenChain(SKPoint a, SKPoint b, List<SKPoint> vertices, int ignoreEdgeIndex)
    {
        for (int i = 0; i < vertices.Count - 1; i++)
        {
            if (i == ignoreEdgeIndex)
            {
                continue;
            }

            if (FSMath.DoSegmentsIntersect(a, b, vertices[i], vertices[i + 1]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsValidTurn(SKPoint previousDirection, SKPoint nextDirection)
    {
        // Reject straight/anti-straight transitions so we don't create collapsible edges.
        float dot = (previousDirection.X * nextDirection.X) + (previousDirection.Y * nextDirection.Y);
        return MathF.Abs(dot) < 0.9999f;
    }

    private static float Cross(SKPoint a, SKPoint b) => (a.X * b.Y) - (a.Y * b.X);

    private static List<SKPoint>? PickDirections(SKPoint[] allowedDirections, int edgeCount, Random rand)
    {
        if (edgeCount < 3 || edgeCount > allowedDirections.Length)
        {
            return null;
        }

        int maxUndirected = Math.Max(1, allowedDirections.Length / 2);
        float minUniqueRatio = rand.NextFloat(0.55f, 0.80f);
        int minUnique = MinUniqueUndirectedDirections(edgeCount, maxUndirected, minUniqueRatio, floor: 5);

        List<SKPoint>? best = null;
        int bestUnique = -1;

        const int maxAttempts = 48;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            HashSet<int> indices = new();
            while (indices.Count < edgeCount)
            {
                indices.Add(rand.Next(allowedDirections.Length));
            }

            List<SKPoint> chosen = indices.Select(i => allowedDirections[i]).ToList();
            chosen.Sort((a, b) => Math.Atan2(a.Y, a.X).CompareTo(Math.Atan2(b.Y, b.X)));

            if (!CoversFullCircle(chosen))
            {
                continue;
            }

            int unique = CountUndirectedDirections(chosen);
            if (unique > bestUnique)
            {
                best = chosen;
                bestUnique = unique;
            }

            if (unique >= minUnique)
            {
                return chosen;
            }
        }

        return best;
    }

    private static List<SKPoint>? PickDirectionsForMainShape(SKPoint[] allowedDirections, int edgeCount, Random rand)
    {
        if (edgeCount < 3 || edgeCount > allowedDirections.Length)
        {
            return null;
        }

        int m = allowedDirections.Length;
        if (m < 6)
        {
            return null;
        }

        int maxGap = (m / 2) - 1; // gap < π
        if (maxGap <= 0)
        {
            return null;
        }

        const int maxAttempts = 200;
        int maxUndirected = Math.Max(1, m / 2);
        float minUniqueRatio = rand.NextFloat(0.65f, 0.90f);
        int minUnique = MinUniqueUndirectedDirections(edgeCount, maxUndirected, minUniqueRatio, floor: 5);

        List<SKPoint>? best = null;
        int bestUnique = -1;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var indices = GenerateUnevenDirectionIndices(edgeCount, m, maxGap, rand);
            if (indices is null)
            {
                continue;
            }

            List<SKPoint> chosen = new(edgeCount);
            foreach (var idx in indices)
            {
                chosen.Add(allowedDirections[idx]);
            }

            chosen.Sort((a, b) => Math.Atan2(a.Y, a.X).CompareTo(Math.Atan2(b.Y, b.X)));
            if (!CoversFullCircle(chosen))
            {
                continue;
            }

            int unique = CountUndirectedDirections(chosen);
            if (unique > bestUnique)
            {
                best = chosen;
                bestUnique = unique;
            }

            if (unique >= minUnique)
            {
                return chosen;
            }
        }

        return best ?? PickDirections(allowedDirections, edgeCount, rand);
    }

    private static int CountUndirectedDirections(List<SKPoint> directions)
    {
        if (directions is null || directions.Count == 0)
        {
            return 0;
        }

        HashSet<int> keys = new();
        foreach (var dir in directions)
        {
            keys.Add(DirectionKey(dir));
        }

        return keys.Count;
    }

    private static int DirectionKey(SKPoint dir)
    {
        double angle = NormalizeAngle(Math.Atan2(dir.Y, dir.X));
        if (angle >= Math.PI)
        {
            angle -= Math.PI;
        }

        double degrees = angle * (180.0 / Math.PI);
        double step = WallAngleSet.AngleStepDegrees;
        int key = (int)Math.Round(degrees / step);
        int max = 180 / WallAngleSet.AngleStepDegrees;
        if (max > 0)
        {
            key %= max;
            if (key < 0)
            {
                key += max;
            }
        }

        return key;
    }

    private static int MinUniqueUndirectedDirections(int edgeCount, int undirectedCount, float ratio, int floor)
    {
        if (edgeCount <= 0 || undirectedCount <= 0)
        {
            return 0;
        }

        ratio = Math.Clamp(ratio, 0f, 1f);
        int baseTarget = (int)MathF.Round(edgeCount * ratio);
        int target = Math.Max(floor, baseTarget);
        target = Math.Min(target, edgeCount);
        return Math.Min(target, undirectedCount);
    }

    private static int[]? GenerateUnevenDirectionIndices(int count, int total, int maxGap, Random rand)
    {
        if (count < 3 || total < count || maxGap < 1)
        {
            return null;
        }

        int remaining = total - count;
        if (remaining < 0)
        {
            return null;
        }

        int[] gaps = new int[count];
        Array.Fill(gaps, 1);

        // Make at least one noticeably large angular gap to avoid "circle-like" polygons.
        int bigIndex = rand.Next(count);
        int bigExtraMax = Math.Min(maxGap - gaps[bigIndex], remaining);
        if (bigExtraMax > 0)
        {
            int bigExtraMin = Math.Min(bigExtraMax, Math.Max(0, remaining / 3));
            int bigExtra = bigExtraMin < bigExtraMax ? rand.Next(bigExtraMin, bigExtraMax + 1) : bigExtraMax;
            gaps[bigIndex] += bigExtra;
            remaining -= bigExtra;
        }

        // Optionally add a second large gap for even more unevenness.
        if (remaining > 0 && count >= 7)
        {
            int secondIndex = rand.Next(count);
            int secondExtraMax = Math.Min(maxGap - gaps[secondIndex], remaining);
            if (secondExtraMax > 0)
            {
                int secondExtraMin = Math.Min(secondExtraMax, Math.Max(0, remaining / 5));
                int secondExtra = secondExtraMin < secondExtraMax ? rand.Next(secondExtraMin, secondExtraMax + 1) : secondExtraMax;
                gaps[secondIndex] += secondExtra;
                remaining -= secondExtra;
            }
        }

        int guard = 0;
        while (remaining > 0 && guard++ < total * 50)
        {
            int i = rand.Next(count);
            if (gaps[i] < maxGap)
            {
                gaps[i]++;
                remaining--;
            }
        }

        if (remaining != 0)
        {
            return null;
        }

        int start = rand.Next(total);
        int[] indices = new int[count];
        int pos = start;
        indices[0] = pos;

        for (int i = 1; i < count; i++)
        {
            pos = (pos + gaps[i - 1]) % total;
            indices[i] = pos;
        }

        Array.Sort(indices);
        for (int i = 1; i < indices.Length; i++)
        {
            if (indices[i] == indices[i - 1])
            {
                return null;
            }
        }

        return indices;
    }

    private static bool CoversFullCircle(List<SKPoint> directions)
    {
        if (directions.Count < 3)
        {
            return false;
        }

        var angles = directions.Select(d => NormalizeAngle(Math.Atan2(d.Y, d.X))).OrderBy(a => a).ToArray();

        double maxGap = 0;
        for (int i = 0; i < angles.Length; i++)
        {
            double a1 = angles[i];
            double a2 = angles[(i + 1) % angles.Length];
            double gap = i == angles.Length - 1 ? (2 * Math.PI - a1) + a2 : a2 - a1;
            maxGap = Math.Max(maxGap, gap);
        }

        return maxGap < Math.PI - 1e-6;
    }

    private static double NormalizeAngle(double angle)
    {
        double twoPi = 2 * Math.PI;
        angle %= twoPi;
        return angle < 0 ? angle + twoPi : angle;
    }

    private static bool TrySolveLengthsMainShape(
        List<SKPoint> directions,
        Random rand,
        float minEdgeLength,
        float maxEdgeLength,
        out float[] lengths)
    {
        lengths = new float[directions.Count];

        int n = directions.Count;
        if (n < 3)
        {
            return false;
        }

        if (minEdgeLength <= 0f || maxEdgeLength <= 0f || minEdgeLength >= maxEdgeLength)
        {
            return false;
        }

        SKPoint sum = new(0, 0);
        for (int i = 0; i < n - 2; i++)
        {
            float len = NextEdgeLength(rand, minEdgeLength, maxEdgeLength);
            lengths[i] = len;
            sum.X += directions[i].X * len;
            sum.Y += directions[i].Y * len;
        }

        var d1 = directions[n - 2];
        var d2 = directions[n - 1];

        float det = d1.X * d2.Y - d1.Y * d2.X;
        if (MathF.Abs(det) < 1e-3f)
        {
            return false;
        }

        float bX = -sum.X;
        float bY = -sum.Y;
        float l1 = (bX * d2.Y - bY * d2.X) / det;
        float l2 = (d1.X * bY - d1.Y * bX) / det;

        if (!float.IsFinite(l1) || !float.IsFinite(l2) || l1 <= 0f || l2 <= 0f)
        {
            return false;
        }

        lengths[n - 2] = l1;
        lengths[n - 1] = l2;
        return true;
    }

    private static bool TrySolveLengths(
        List<SKPoint> directions,
        Random rand,
        float minEdgeLength,
        float maxEdgeLength,
        out float[] lengths)
    {
        lengths = new float[directions.Count];

        int n = directions.Count;
        if (n < 3)
        {
            return false;
        }

        // Choose random lengths for all but the last 2 edges, then solve for closure.
        SKPoint sum = new(0, 0);
        for (int i = 0; i < n - 2; i++)
        {
            float len = NextEdgeLength(rand, minEdgeLength, maxEdgeLength);
            lengths[i] = len;
            sum.X += directions[i].X * len;
            sum.Y += directions[i].Y * len;
        }

        var d1 = directions[n - 2];
        var d2 = directions[n - 1];

        float det = d1.X * d2.Y - d1.Y * d2.X;
        if (MathF.Abs(det) < 1e-3f)
        {
            return false;
        }

        // Solve: d1*l1 + d2*l2 = -sum
        float bX = -sum.X;
        float bY = -sum.Y;
        float l1 = (bX * d2.Y - bY * d2.X) / det;
        float l2 = (d1.X * bY - d1.Y * bX) / det;

        if (!float.IsFinite(l1) || !float.IsFinite(l2) || l1 <= 0f || l2 <= 0f)
        {
            return false;
        }

        lengths[n - 2] = l1;
        lengths[n - 1] = l2;
        return true;
    }

    private static float NextLogUniform(Random rand, float min, float max)
    {
        if (min <= 0f || max <= 0f || min >= max)
        {
            return min;
        }

        float logMin = MathF.Log(min);
        float logMax = MathF.Log(max);
        float t = (float)rand.NextDouble();
        float log = logMin + (logMax - logMin) * t;
        float value = MathF.Exp(log);
        return float.IsFinite(value) ? value : min;
    }

    private static float NextEdgeLength(Random rand, float min, float max)
    {
        if (min <= 0f || max <= 0f || min >= max)
        {
            return min;
        }

        int mode = rand.Next(3);
        if (mode == 0)
        {
            return rand.NextFloat(min, max);
        }

        if (mode == 1)
        {
            return NextLogUniform(rand, min, max);
        }

        float t = (float)rand.NextDouble();
        if (rand.NextDouble() < 0.5)
        {
            t *= t;
        }
        else
        {
            t = 1f - ((1f - t) * (1f - t));
        }

        float value = min + (max - min) * t;
        return float.IsFinite(value) ? value : min;
    }

    private static SKPoint[] BuildPolygonFromEdges(List<SKPoint> directions, float[] lengths)
    {
        int n = directions.Count;
        SKPoint[] polygon = new SKPoint[n];
        SKPoint pos = new(0, 0);

        for (int i = 0; i < n; i++)
        {
            polygon[i] = pos;
            pos.X += directions[i].X * lengths[i];
            pos.Y += directions[i].Y * lengths[i];
        }

        return polygon;
    }

    private static void ScaleToArea(ref SKPoint[] polygon, float targetArea)
    {
        float currentArea = FSMath.CalculateArea(polygon);
        if (!float.IsFinite(currentArea) || currentArea <= 0f)
        {
            return;
        }

        if (!float.IsFinite(targetArea) || targetArea <= 0f)
        {
            return;
        }

        float scale = MathF.Sqrt(targetArea / currentArea);
        if (!float.IsFinite(scale) || scale <= 0f)
        {
            return;
        }

        for (int i = 0; i < polygon.Length; i++)
        {
            polygon[i].X *= scale;
            polygon[i].Y *= scale;
        }
    }

    private static bool IsMainShapeInteresting(SKPoint[] polygon)
    {
        int n = polygon.Length;
        if (n < 3)
        {
            return false;
        }

        float area = FSMath.CalculateArea(polygon);
        if (!float.IsFinite(area) || area <= 0f)
        {
            return false;
        }

        float perimeter = 0f;
        float sumEdge = 0f;
        float sumEdgeSq = 0f;

        for (int i = 0; i < n; i++)
        {
            float len = FSMath.CalculateDistance(polygon[i], polygon[(i + 1) % n]);
            if (!float.IsFinite(len) || len <= 0f)
            {
                return false;
            }

            perimeter += len;
            sumEdge += len;
            sumEdgeSq += len * len;
        }

        if (!float.IsFinite(perimeter) || perimeter <= 0f)
        {
            return false;
        }

        float mean = sumEdge / n;
        float variance = (sumEdgeSq / n) - (mean * mean);
        float edgeCv = mean > 0f && variance > 0f ? MathF.Sqrt(variance) / mean : 0f;

        float circularity = (4f * MathF.PI * area) / (perimeter * perimeter);

        var bounds = FSMath.ShapeBounds(polygon);
        float width = bounds.Right - bounds.Left;
        float height = bounds.Bottom - bounds.Top;
        if (!float.IsFinite(width) || !float.IsFinite(height) || width <= 0f || height <= 0f)
        {
            return false;
        }

        float aspect = MathF.Max(width, height) / MathF.Min(width, height);
        if (!float.IsFinite(aspect) || aspect <= 0f)
        {
            return false;
        }

        var center = FSMath.Centroid(polygon);
        float minRSq = float.MaxValue;
        float maxRSq = 0f;
        float sumR = 0f;
        float sumRSq = 0f;
        int radialCount = 0;

        for (int i = 0; i < n; i++)
        {
            float dx = polygon[i].X - center.X;
            float dy = polygon[i].Y - center.Y;
            float rSq = (dx * dx) + (dy * dy);
            if (!float.IsFinite(rSq) || rSq <= 0f)
            {
                continue;
            }

            minRSq = Math.Min(minRSq, rSq);
            maxRSq = Math.Max(maxRSq, rSq);
            float r = MathF.Sqrt(rSq);
            if (float.IsFinite(r) && r > 0f)
            {
                sumR += r;
                sumRSq += rSq;
                radialCount++;
            }
        }

        if (!float.IsFinite(minRSq) || !float.IsFinite(maxRSq) || minRSq <= 1e-3f || maxRSq <= 0f || radialCount < 3)
        {
            return false;
        }

        float radialRatio = MathF.Sqrt(maxRSq / minRSq);
        if (!float.IsFinite(radialRatio) || radialRatio <= 0f)
        {
            return false;
        }

        float meanR = sumR / radialCount;
        float varianceR = (sumRSq / radialCount) - (meanR * meanR);
        float radialCv = meanR > 0f && varianceR > 0f ? MathF.Sqrt(varianceR) / meanR : 0f;

        if (!float.IsFinite(radialCv))
        {
            return false;
        }

        float t = Math.Clamp((n - 6f) / 6f, 0f, 1f);
        float minEdgeCv = 0.22f + (0.30f - 0.22f) * t;
        float maxCircularity = 0.90f + (0.82f - 0.90f) * t;
        float minRadialRatio = 1.25f + (1.55f - 1.25f) * t;
        float minRadialCv = 0.14f + (0.26f - 0.14f) * t;

        // Reject "too round" main shapes. Thresholds scale with edge count: more edges => stricter.
        return edgeCv >= minEdgeCv
            && circularity <= maxCircularity
            && radialRatio >= minRadialRatio
            && radialCv >= minRadialCv
            && aspect <= 3.20f;
    }
}
