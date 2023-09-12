using Clipper2Lib;
using SkiaSharp;
using System.Numerics;
using FillRule = Clipper2Lib.FillRule;

namespace Fillsquir.Controls
{
    internal static class FSMath
    {
#if DEBUG
        static internal int randit;
#endif
        public static bool IsPointInShape(SKPoint point, ICollection<SKPoint> figure)//incluzive
        {
            if (figure.Count < 3) 
            {
                return false;
            }
            foreach(var f in figure)
            {
                  if (f.X == point.X && f.Y == point.Y)
                {
                    return true;
                }
            }
            SKPoint outsidePoint = new SKPoint(-9999, -9999); 
            int intersectCount = 0;

            SKPoint prevPoint = new SKPoint(float.NaN, float.NaN);
            foreach (SKPoint currentPoint in figure)
            {
                if (!float.IsNaN(prevPoint.X))
                {
                    if (DoSegmentsIntersect(point, outsidePoint, prevPoint, currentPoint))
                    {
                        intersectCount++;
                    }
                }

                prevPoint = currentPoint;
            }

            // Connect last and first points to close the polygon
            var a = figure.GetEnumerator();
            a.MoveNext();
            if (DoSegmentsIntersect(point, outsidePoint, prevPoint, a.Current))
            {
                intersectCount++;
            }

            return intersectCount % 2 != 0;
        }


        public static float CalculateDistance(SKPoint point1, SKPoint point2)
        {
            float dx = point2.X - point1.X;
            float dy = point2.Y - point1.Y;
            //can i return float?
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        public static bool OnSegment(SKPoint p, SKPoint q, SKPoint r)
        {
            return q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) &&
                   q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y);
        }
        public static bool DoSegmentsIntersect(SKPoint p1, SKPoint q1, SKPoint p2, SKPoint q2)
        {
            // Calculate orientation
            float o1 = Orientation(p1, q1, p2);
            float o2 = Orientation(p1, q1, q2);
            float o3 = Orientation(p2, q2, p1);
            float o4 = Orientation(p2, q2, q1);

            // General case: segments intersect if orientations are different for each pair of points
            if (o1 != o2 && o3 != o4)
            {
                return true;
            }

            // Special Cases
            // p1, q1 and p2 are collinear and p2 lies on segment p1q1
            if (o1 == 0 && OnSegment(p1, p2, q1)) return true;

            // p1, q1 and p2 are collinear and q2 lies on segment p1q1
            if (o2 == 0 && OnSegment(p1, q2, q1)) return true;

            // p2, q2 and p1 are collinear and p1 lies on segment p2q2
            if (o3 == 0 && OnSegment(p2, p1, q2)) return true;

            // p2, q2 and q1 are collinear and q1 lies on segment p2q2
            if (o4 == 0 && OnSegment(p2, q1, q2)) return true;
            return false;
        }

        public static float Orientation(SKPoint a, SKPoint b, SKPoint c)
        {
            float result = (float)((b.Y - a.Y) * (c.X - b.X) - (b.X - a.X) * (c.Y - b.Y));
            if (result == 0) return 0;  // Collinear
            return (result > 0) ? 1 : -1; // Clockwise or Counterclockwise
        }

        // common area
        public static SKPoint[] SutherlandHodgman(SKPoint[] p1, SKPoint[] p2)
        {
            List<SKPoint> outputList = new List<SKPoint>(p1);

            for (int i = 0; i < p2.Length; i++)
            {
                SKPoint clipEdgeStart = p2[i];
                SKPoint clipEdgeEnd = p2[(i + 1) % p2.Length];

                List<SKPoint> inputList = new List<SKPoint>(outputList);
                outputList.Clear();

                if (inputList.Count == 0)
                {
                    break;
                }

                SKPoint prevPoint = inputList[inputList.Count - 1];
                foreach (SKPoint currentPoint in inputList)
                {
                    if (IsInside(clipEdgeStart, clipEdgeEnd, currentPoint))
                    {
                        if (!IsInside(clipEdgeStart, clipEdgeEnd, prevPoint))
                        {
                            outputList.Add(Intersection(clipEdgeStart, clipEdgeEnd, prevPoint, currentPoint));
                        }
                        outputList.Add(currentPoint);
                    }
                    else if (IsInside(clipEdgeStart, clipEdgeEnd, prevPoint))
                    {
                        outputList.Add(Intersection(clipEdgeStart, clipEdgeEnd, prevPoint, currentPoint));
                    }
                    prevPoint = currentPoint;
                }
            }

            return outputList.ToArray();
        }

        public static SKPoint Centroid(SKPoint[] points)
        {
            float centroidX = 0, centroidY = 0;

            foreach (var point in points)
            {
                centroidX += point.X;
                centroidY += point.Y;
            }

            return new SKPoint(centroidX / points.Length, centroidY / points.Length);
        }

        public static void ScaleShape(ref SKPoint[] shape, float width, float height)
        {
            float maxx = 0, maxy = 0,
                        minx = float.MaxValue, miny = float.MaxValue;
            foreach (var point in shape)
            {
                if (point.X > maxx)
                {
                    maxx = point.X;
                }
                if (point.X < minx)
                {
                    minx = point.X;
                }
                if (point.Y > maxy)
                {
                    maxy = point.Y;
                }
                if (point.Y < miny)
                {
                    miny = point.Y;
                }
            }
            float scalex = width / (maxx - minx);
            float scaley = height / (maxy - miny);
            for (int i = 0; i < shape.Length; i++)
            {
                shape[i] = new SKPoint((shape[i].X - minx) * scalex, (shape[i].Y - miny) * scaley);
            }

        }

        public static float CalculateArea(SKPoint[] figure)
        {
#if DEBUG
            if (figure.Length < 3)
            {
                throw new ArgumentException("Figure must have at least 3 points", "figure");
            }
#endif

            float area = 0;

            for (int i = 0; i < figure.Length; i++)
            {
                int j = (i + 1) % figure.Length;  // Wrap around to the start if we're at the last vertex
                area += (figure[i].X * figure[j].Y) - (figure[i].Y * figure[j].X);
            }

            area = MathF.Abs(area) / 2;

            return area;
        }

        public static float CalculateArea(List<SKPoint> figure)
        {
            float area = 0;

            for (int i = 0; i < figure.Count - 1; i++)
            {
                area += (figure[i].X * figure[i + 1].Y) - (figure[i].Y * figure[i + 1].X);
            }

            area += (figure[figure.Count - 1].X * figure[0].Y) - (figure[figure.Count - 1].Y * figure[0].X);

            area = MathF.Abs(area) / 2;

            return area;
        }



        /*public static List<SKPoint[]> CommonArea(SKPoint[] p1, SKPoint[] p2)
        {
            List<List<SKPoint>> subj = new List<List<SKPoint>>(1), clip = new List<List<SKPoint>>(1), solution = new List<List<SKPoint>>();
            subj.Add(new List<SKPoint>());
            clip.Add(new List<SKPoint>());

            // Convert SKPoint to IntPoint (Clipper's integer point) and populate 'subj' and 'clip'
            foreach (SKPoint p in p1)
            {
                subj[0].Add(new IntPoint((long)(p.X * 1e6), (long)(p.Y * 1e6)));
            }

            foreach (SKPoint p in p2)
            {
                clip[0].Add(new IntPoint((long)(p.X * 1e6), (long)(p.Y * 1e6)));
            }

            Clipper c = new Clipper();
           // c.AddPolygon(subj, PolyType.ptSubject);
          //  c.AddPolygon(clip, PolyType.ptClip);
            c.Execute(ClipType.ctIntersection, solution, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);

            // Convert the result back to SKPoint and return
            List<SKPoint[]> result = new List<SKPoint[]>();
            foreach (List<IntPoint> path in solution)
            {
                List<SKPoint> pfList = new List<SKPoint>();
                foreach (IntPoint ip in path)
                {
                    pfList.Add(new SKPoint((float)ip.X / 1e6f, (float)ip.Y / 1e6f));
                }
                result.Add(pfList.ToArray());
            }

            return result;
        }*/
        /*public static List<List<SKPoint[]>> CommonArea2(SKPoint[] p1, List<SKPoint[]> p2)
        {
            // Convert p1 to a Path
            Path subj = new Path(p1.Length);
            foreach (var point in p1)
            {
                subj.Add(new IntPoint((long)(point.X * 1000), (long)(point.Y * 1000)));
            }

            // Convert p2 to a list of Paths
            List<Paths> clips = new List<Paths>();
            foreach (var polygon in p2)
            {
                Paths clip = new Paths(1);
                clip.Add(new Path(polygon.Length));
                foreach (var point in polygon)
                {
                    clip[0].Add(new IntPoint((long)(point.X * 1000), (long)(point.Y * 1000)));
                }
                clips.Add(clip);
            }

            // Perform intersection operations
            List<Paths> solutions = new List<Paths>();
            Clipper c = new();
            c.AddPolygon(subj, PolyType.ptSubject);
            foreach (var clip in clips)
            {
                c.AddPolygons(clip, PolyType.ptClip);
                Paths solution = new Paths();
                c.Execute(ClipType.ctIntersection, solution);
                solutions.Add(solution);
                c.Clear();
                c.AddPolygon(subj, PolyType.ptSubject);
            }

            // Convert solutions to a list of lists of SKPoint arrays
            List<List<SKPoint[]>> result = new List<List<SKPoint[]>>();
            foreach (var solution in solutions)
            {
                List<SKPoint[]> polygons = new List<SKPoint[]>();
                foreach (var path in solution)
                {
                    SKPoint[] points = new SKPoint[path.Count];
                    for (int i = 0; i < path.Count; i++)
                    {
                        points[i] = new SKPoint((float)path[i].X / 1000, (float)path[i].Y / 1000);
                    }
                    polygons.Add(points);
                }
                result.Add(polygons);
            }

            return result;
        }*/
        private static Path64 SKPointArrayToPath64(SKPoint[] points)
        {
            Path64 path = new Path64();
            foreach (SKPoint point in points)
            {
                // Clipper's Point64 constructor takes longs, so we need to convert the SKPoint's floats to longs.
                // We can do this by rounding the float to the nearest integer, since SKPoint uses single-precision floating point numbers.
                long x = (long)Math.Round(point.X * 16384);
                long y = (long)Math.Round(point.Y * 16384);
                path.Add(new Point64(x, y));
            }
            return path;
        }
        private static List<SKPoint[]> Path64ToSKPointArrayList(Path64 path)
        {
            List<SKPoint[]> list = new List<SKPoint[]>();
            SKPoint[] points = new SKPoint[path.Count];
            for (int i = 0; i < path.Count; i++)
            {
                points[i] = new SKPoint((float)path[i].X / 16384, (float)path[i].Y / 16384);
            }
            list.Add(points);
            return list;
        }
        public static List<SKPoint[]> CommonArea(SKPoint[] p1, List<SKPoint[]> p2)
        {
            Paths64 subject = new Paths64();
            Paths64 clip = new Paths64();
            subject.Add(SKPointArrayToPath64(p1));
            foreach (var figure in p2)
            {
                clip.Add(SKPointArrayToPath64(figure));
            }
            Paths64 commonArea = Clipper.Intersect(subject, clip, FillRule.NonZero);
            List<SKPoint[]> result = new List<SKPoint[]>();
            foreach (var path in commonArea)
            {
                result.AddRange(Path64ToSKPointArrayList(path));
            }
            return result;
        }

        public static List<SKPoint[]> SKCommonArea(SKPoint[] p1, List<SKPoint[]> p2)
        {
            List<SKPoint[]> result = new List<SKPoint[]>();
            SKPath subject = new SKPath();
            subject.AddPoly(p1, false);

            foreach (var figure in p2)
            {
                SKPath clip = new SKPath();
                clip.AddPoly(figure, false);
                SKPath commonArea = subject.Op(clip, SKPathOp.Intersect);
                if(commonArea == null)
                {
                    continue;
                }
                SKPath.RawIterator iterator = commonArea.CreateRawIterator();
                SKPathVerb verb;
                List<SKPoint> points = new List<SKPoint>();
                SKPoint[] segmentPoints = new SKPoint[4];
                while ((verb = iterator.Next(segmentPoints)) != SKPathVerb.Done)
                {
                    switch (verb)
                    {
                        case SKPathVerb.Move:
                        case SKPathVerb.Line:
                            points.Add(segmentPoints[0]);
                            break;
                        case SKPathVerb.Quad:
                            points.Add(segmentPoints[1]);
                            break;
                        case SKPathVerb.Cubic:
                            points.Add(segmentPoints[2]);
                            break;
                        case SKPathVerb.Close:
                            result.Add(points.ToArray());
                            points = new List<SKPoint>();
                            break;
                    }
                }
            }

            return result;
        }
        internal static SKPoint GetFurtherstDirectionVector(SKPoint[] Shape, SKPoint DirectionVector)
        {
            SKPoint mostDifferentDirection = new SKPoint();
            double closestToOrthogonal = double.MaxValue;  // initialized to the largest possible value

            SKPoint normalizedDirectionVector = DirectionVector;

            for (int i = 0; i < Shape.Length; i++)
            {
                SKPoint nextPoint = (i == Shape.Length - 1) ? Shape[0] : Shape[i + 1];
                // Get the direction between current point and next point
                SKPoint normalizedCurrentDirection = FSMath.DirectionVector(Shape[i], nextPoint);
                var dotProduct = DotProduct(normalizedDirectionVector, normalizedCurrentDirection);
                //is dot product always positive?
                var distanceFromOrthogonal = MathF.Abs(dotProduct);

                if (distanceFromOrthogonal < closestToOrthogonal)
                {
                    closestToOrthogonal = distanceFromOrthogonal;
                    mostDifferentDirection = normalizedCurrentDirection;
                }
            }
            return mostDifferentDirection;
        }

        private static SKPoint Normalize(SKPoint vector)
        {
            float magnitude = MathF.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
            return new SKPoint(vector.X / magnitude, vector.Y / magnitude);
        }

        private static float DotProduct(SKPoint v1, SKPoint v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y;
        }


        public static bool CheckAngle(SKPoint[] figure, double maxAllowedAngleRadian)//returns true if angle fneeds a fix
        {
            for (int i = 0; i < figure.Length; i++)
            {
                SKPoint current = figure[i];
                SKPoint next = (i == figure.Length - 1) ? figure[0] : figure[i + 1];
                SKPoint prev = (i == 0) ? figure[figure.Length - 1] : figure[i - 1];

                SKPoint a = new SKPoint(current.X - prev.X, current.Y - prev.Y);
                SKPoint b = new SKPoint(next.X - current.X, next.Y - current.Y);

                double angleRadian = AngleBetweenVectors(a, b);

                if (angleRadian > maxAllowedAngleRadian)
                {
                    return false;
                }
            }

            return true;
        }

        private static float AngleBetweenVectors(SKPoint v1, SKPoint v2)
        {
            float dotProduct = DotProduct(v1, v2);
            float magnitudeV1 = Magnitude(v1);
            float magnitudeV2 = Magnitude(v2);
            float cosTheta = dotProduct / (magnitudeV1 * magnitudeV2);

            float angleRadian = MathF.Acos(MathF.Abs(cosTheta));

            return angleRadian;
        }

        private static float AngleBetweenVectorsAbs(SKPoint v1, SKPoint v2)
        {
            float dotProduct = DotProduct(v1, v2);
            float magnitudeV1 = Magnitude(v1);
            float magnitudeV2 = Magnitude(v2);

            float denominator = magnitudeV1 * magnitudeV2;
            if (denominator == 0f) return 0f;  // or you might want to handle this case differently
            float cosTheta = dotProduct / denominator;
            cosTheta = MathF.Max(-1.0f, MathF.Min(1.0f, cosTheta));

            float angleRadian = MathF.Acos(cosTheta);
            if (angleRadian > MathF.PI / 2)
            {
                angleRadian = MathF.PI - angleRadian;
            }
            return angleRadian;
        }
        private static float Magnitude(SKPoint v)
        {
            return MathF.Sqrt(v.X * v.X + v.Y * v.Y);
        }
        public static (List<SKPoint[]>, double) CommonArea2(SKPoint[] p1, List<SKPoint[]> p2)
        {
            Paths64 subject = new Paths64();
            Paths64 clip = new Paths64();

            // Convert SKPoint[] to Path64 and add to subject
            subject.Add(SKPointArrayToPath64(p1));

            // Convert each SKPoint[] in the list to Path64 and add to clip
            foreach (var figure in p2)
            {
                clip.Add(SKPointArrayToPath64(figure));
            }

            // Find the common area
            Paths64 commonArea = Clipper.Intersect(subject, clip, FillRule.Positive);//cosider EvenOdd rule

            double area = 0;
            // Convert the common area from Paths64 to List<List<SKPoint[]>> for the return value
            List<SKPoint[]> result = new List<SKPoint[]>();
            foreach (var path in commonArea)
            {
                result.AddRange(Path64ToSKPointArrayList(path));
                area += Math.Abs(Clipper.Area(path));
            }
            return (result, area);
        }
        public static bool IsInside(SKPoint clipEdgeStart, SKPoint clipEdgeEnd, SKPoint point)
        {
            return (clipEdgeEnd.Y - clipEdgeStart.Y) * (point.X - clipEdgeStart.X) -
                   (clipEdgeEnd.X - clipEdgeStart.X) * (point.Y - clipEdgeStart.Y) >= 0;
        }

        public static SKPoint Intersection(SKPoint clipEdgeStart, SKPoint clipEdgeEnd, SKPoint lineStart, SKPoint lineEnd)
        {
            float A1 = lineEnd.Y - lineStart.Y;
            float B1 = lineStart.X - lineEnd.X;
            float C1 = A1 * lineStart.X + B1 * lineStart.Y;

            float A2 = clipEdgeEnd.Y - clipEdgeStart.Y;
            float B2 = clipEdgeStart.X - clipEdgeEnd.X;
            float C2 = A2 * clipEdgeStart.X + B2 * clipEdgeStart.Y;

            float det = A1 * B2 - A2 * B1;
            float x = (B2 * C1 - B1 * C2) / det;
            float y = (A1 * C2 - A2 * C1) / det;

            return new SKPoint(x, y);
        }

#if YourDumb
        internal static void EnsureTriangleIsWrongDirection(ref SKPoint[] triangle)
        {
#if DEBUG
            if (triangle.Length != 3)
            {
                throw new ArgumentException("Triangle must have 3 points", "triangle");
            }
#endif
            var p1 = triangle[0];
            var p2 = triangle[1];
            var p3 = triangle[2];

            var crossProduct = (p2.X - p1.X) * (p3.Y - p1.Y) - (p2.Y - p1.Y) * (p3.X - p1.X);

            if (crossProduct < 0)
            {
                // The points are in a clockwise direction, so no need to change
                return;
            }

            // If the points are in a counter-clockwise direction, swap two points to make it clockwise
            var temp = triangle[1];
            triangle[1] = triangle[2];
            triangle[2] = temp;
        }
#endif

        internal static void EnsureTriangleDirection(ref SKPoint[] triangle)
        {
#if DEBUG
            if (triangle.Length != 3)
            {
                throw new ArgumentException("Triangle must have 3 points", "triangle");
            }
#endif
            var p1 = triangle[0];
            var p2 = triangle[1];
            var p3 = triangle[2];

            var crossProduct = (p2.X - p1.X) * (p3.Y - p1.Y) - (p2.Y - p1.Y) * (p3.X - p1.X);

            if (crossProduct > 0)
            {
                // The points are in a counter-clockwise direction, so no need to change
                return;
            }

            // If the points are in a clockwise direction, swap two points to make it counter-clockwise
            var temp = triangle[1];
            triangle[1] = triangle[2];
            triangle[2] = temp;
        }


        internal static bool EnsureFigureDirection(ref SKPoint[] figure)
        {
            if (figure == null || figure.Length < 3)
            {
                throw new ArgumentException("Figure must have at least 3 points", "figure");
            }

            // Find convex hull using Gift Wrapping algorithm
            List<SKPoint> convexHull = GetConvexHull(figure.ToList());
            if(convexHull == null)
            {
                return false;
            }              
            List<SKPoint> remainingPoints = figure.Except(convexHull).ToList();

            foreach (var point in remainingPoints)
            {
                double bestPerimeterIncrease = double.MaxValue;
                int bestInsertionIndex = -1;

                for (int i = 0; i < convexHull.Count; i++)
                {
                    var current = convexHull[i];
                    var next = convexHull[(i + 1) % convexHull.Count];

                    double currentPerimeter = GetDistance(current, next);
                    double newPerimeter = GetDistance(current, point) + GetDistance(point, next);

                    double perimeterIncrease = newPerimeter - currentPerimeter;
                    if (perimeterIncrease < bestPerimeterIncrease)
                    {
                        bestPerimeterIncrease = perimeterIncrease;
                        bestInsertionIndex = i + 1;
                    }
                }

                if (bestInsertionIndex != -1)
                {
                    convexHull.Insert(bestInsertionIndex, point);
                }
            }
            //set figure to convex hull without last point
            //convexHull.RemoveAt(convexHull.Count - 1);
            figure = convexHull.ToArray();
            return true;
        }

        private static double GetDistance(SKPoint a, SKPoint b)
        {
            return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        }

        private static List<SKPoint> GetConvexHull(List<SKPoint> points)
        {
            if (points == null || points.Count < 3)
                throw new ArgumentException("At least 3 distinct points are required to compute a convex hull.");

            List<SKPoint> convexHull = new List<SKPoint>();

            // Step 1: Find the leftmost point (If there are multiple, take the bottom one).
            SKPoint startPoint = points[0];
            foreach (var point in points)
            {
                if (point.X < startPoint.X || (point.X == startPoint.X && point.Y < startPoint.Y))
                {
                    startPoint = point;
                }
            }

            convexHull.Add(startPoint);
            SKPoint currentPoint = startPoint;
            SKPoint nextPoint;

            // Infinite loop protection for malformed datasets
            int infiniteLoopProtection = 0;
            const double epsilon = 1e-10; // For floating point precision checks

            // Step 2: Keep wrapping
            do
            {
                nextPoint = points[0] == currentPoint ? points[1] : points[0];

                for (int i = 0; i < points.Count; i++)
                {
                    if (points[i] == currentPoint)
                    {
                        continue;
                    }

                    double value = (points[i].X - currentPoint.X) * (nextPoint.Y - currentPoint.Y) -
                                   (points[i].Y - currentPoint.Y) * (nextPoint.X - currentPoint.X);

                    if (value > epsilon ||
                        (Math.Abs(value) < epsilon && GetDistance(currentPoint, points[i]) > GetDistance(currentPoint, nextPoint)))
                    {
                        nextPoint = points[i];
                    }
                }

                // Avoid adding the same point again, which would cause an infinite loop.
                if (convexHull.Contains(nextPoint))
                {
                    break;
                }

                convexHull.Add(nextPoint);
                currentPoint = nextPoint;

                infiniteLoopProtection++;
                if (infiniteLoopProtection > points.Count)
                {
                    return null;
                }
            } while (currentPoint != startPoint);
            return convexHull;
        }




        internal static SKPoint ShapeSize(SKPoint[] figure)
        {
            // Find the minimum and maximum X and Y values and subtract them to get the width and height
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var point in figure)
            {
                if (point.X < minX)
                {
                    minX = point.X;
                }
                if (point.X > maxX)
                {
                    maxX = point.X;
                }
                if (point.Y < minY)
                {
                    minY = point.Y;
                }
                if (point.Y > maxY)
                {
                    maxY = point.Y;
                }
            }
            float width = maxX - minX;
            float height = maxY - minY;
            return new SKPoint(width, height);
        }

        internal static SKRect ShapeBounds(SKPoint[] figure)
        {
            // Find the minimum and maximum X and Y values and subtract them to get the width and height
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach(var point in figure)
            {
                if(point.X < minX)
                {
                    minX = point.X;
                }
                if(point.X > maxX)
                {
                    maxX = point.X;
                }
                if(point.Y < minY)
                { 
                    minY = point.Y;
                }
                if(point.Y > maxY)
                { 
                    maxY = point.Y;
                }
            }
            return new SKRect(minX, minY, maxX, maxY);
        }

        internal static SKPoint minBounds(SKPoint[] figure)
        {
            float minX = float.MaxValue, minY = float.MaxValue;
            foreach (var point in figure)
            {
                if (point.X < minX)
                {
                    minX = point.X;
                }
                if (point.Y < minY)
                {
                    minY = point.Y;
                }
            }
            return new SKPoint(minX, minY);
        }

        internal static SKPoint DirectionVector(SKPoint p1, SKPoint p2)
        {
            var dx = p2.X - p1.X;
            var dy = p2.Y - p1.Y;
            return Normalize(new SKPoint(dx, dy));
        }
        internal static bool AreDirectionVectorsSimilar(SKPoint v1, SKPoint v2, out bool rotate)
        {
            var dotProduct = v1.X * v2.X + v1.Y * v2.Y;
            var v1Length = Math.Sqrt(v1.X * v1.X + v1.Y * v1.Y);
            var v2Length = Math.Sqrt(v2.X * v2.X + v2.Y * v2.Y);
            var angle = Math.Acos(dotProduct / (v1Length * v2Length));
            if (angle < Math.PI / 16)
            {
                rotate = false;
                return true;
            }
            //check if the angle is close to 180 degrees
            if (angle > Math.PI - Math.PI / 16)
            {
                rotate = true;
                return true;
            }
            rotate = false;
            return false;
        }
        const double epsilon = 1e-6;

        public static bool AreVectorsSimilar(SKPoint a, SKPoint b)
        {
            double dot = DotProduct(a, b);
            return Math.Abs(dot - 1) < epsilon;
        }

        internal static void AdjustPointToMatchDirectionVector(SKPoint p1, ref SKPoint AdjustPoint, SKPoint DirectionVector)
        {
            var dx = AdjustPoint.X - p1.X;
            var dy = AdjustPoint.Y - p1.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            var newAngle = Math.Atan2(DirectionVector.Y, DirectionVector.X);
            var newDx = length * Math.Cos(newAngle);
            var newDy = length * Math.Sin(newAngle);
            AdjustPoint.X = (float)(p1.X + newDx);
            AdjustPoint.Y = (float)(p1.Y + newDy);
        }

        internal static int FindMinDistance(SKPoint[] figure)
        {
            // Find the minimum distance between any two points
            //and return the index of the first point
            int minIndex = 0;
            float minDistance = float.MaxValue;
            for (int i = 0; i < figure.Length; i++)
            {
                var p1 = figure[i];
                for (int j = i + 1; j < figure.Length; j++)
                {
                    var p2 = figure[j];
                    var dx = p2.X - p1.X;
                    var dy = p2.Y - p1.Y;
                    var distance = dx * dx + dy * dy;
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        minIndex = i;
                    }
                }
            } 
            return minIndex;
        }

        public static void EnsureFigureIsNotFlat(ref SKPoint[] figure, Random rand)
        {
            {
                float mindiff = 10f, minval = float.MaxValue, maxval = 0f;
                bool goodshape = false;
                foreach (var point in figure)
                {
                    if(point.X < minval)
                    {
                        minval = point.X;
                    }
                    if(point.X > maxval)
                    {
                        maxval = point.X;
                    }
                    if(maxval - minval > mindiff)
                    {
                        goodshape = true;
                        break;
                    }
                }
                if(!goodshape)
                {
                    figure[rand.Next(0,figure.Length)].X = maxval + mindiff;
                }

            }

            {    
                float mindiff = 10f, minval = float.MaxValue, maxval = 0f;
                bool goodshape = false;
                foreach (var point in figure)
                {
                    if(point.Y < minval)
                    {
                        minval = point.Y;
                    }
                    if(point.Y > maxval)
                    {
                        maxval = point.Y;
                    }
                    if(maxval - minval > mindiff)
                    {
                        goodshape = true;
                        break;
                    }
                }
                if(!goodshape)
                {
                    figure[rand.Next(0,figure.Length)].Y = maxval + mindiff * 2;
                }   

            }
        }

        internal static SKPoint MovePointAwayFrom(SKPoint moved, SKPoint constant, float v)
        {
            //move the point away from the second point
            var directionVector = DirectionVector(moved, constant);
            var dx = directionVector.X;
            var dy = directionVector.Y;
            var length = MathF.Sqrt(dx * dx + dy * dy);
            var newDx = dx / length * v;
            var newDy = dy / length * v;
            //or i want to return new point
            return new SKPoint(moved.X + newDx, moved.Y + newDy);
        }
        internal static SKPoint MoveTowardsDirectionVector(SKPoint sKPoint, SKPoint DirectionVector, float far)
        {
            var length = MathF.Sqrt(DirectionVector.X * DirectionVector.X + DirectionVector.Y * DirectionVector.Y);
            var newDx = DirectionVector.X / length * far;
            var newDy = DirectionVector.Y / length * far;
            return new SKPoint(sKPoint.X + newDx, sKPoint.Y + newDy);
        }

        internal static void MovePointAwayFromPoints(ref SKPoint sKPoint1, SKPoint sKPoint2, SKPoint sKPoint3, float v)
        {
            // Calculate the direction vector from sKPoint2 to sKPoint3
            SKPoint direction = new SKPoint(sKPoint3.X - sKPoint2.X, sKPoint3.Y - sKPoint2.Y);

            // Find the vector perpendicular to this direction (for a 2D space we can just swap x and y and negate one of them to get a perpendicular vector)
            SKPoint perpendicular = new SKPoint(direction.Y, -direction.X);

            // Normalize the perpendicular vector
            float length = (float)Math.Sqrt(perpendicular.X * perpendicular.X + perpendicular.Y * perpendicular.Y);
            SKPoint normalizedPerpendicular = new SKPoint(perpendicular.X / length, perpendicular.Y / length);

            // Scale the normalized perpendicular vector by v
            SKPoint scaledVector = new SKPoint(normalizedPerpendicular.X * v, normalizedPerpendicular.Y * v);

            // Update sKPoint1 by adding the scaled vector
            sKPoint1.X += scaledVector.X;
            sKPoint1.Y += scaledVector.Y;
        }

      
        internal static void MovePointTowardsPoints(ref SKPoint sKPoint1, SKPoint sKPoint2, SKPoint sKPoint3, float v)
        {
            // Calculate the direction vector from sKPoint2 to sKPoint3
            SKPoint direction = new SKPoint(sKPoint3.X - sKPoint2.X, sKPoint3.Y - sKPoint2.Y);

            // Find the vector perpendicular to this direction (for a 2D space we can just swap x and y and negate one of them to get a perpendicular vector)
            SKPoint perpendicular = new SKPoint(direction.Y, -direction.X);

            // Normalize the perpendicular vector
            float length = (float)Math.Sqrt(perpendicular.X * perpendicular.X + perpendicular.Y * perpendicular.Y);
            SKPoint normalizedPerpendicular = new SKPoint(perpendicular.X / length, perpendicular.Y / length);

            // Scale the normalized perpendicular vector by v
            SKPoint scaledVector = new SKPoint(normalizedPerpendicular.X * v, normalizedPerpendicular.Y * v);

            // Update sKPoint1 by adding the scaled vector
            sKPoint1.X -= scaledVector.X;
            sKPoint1.Y -= scaledVector.Y;
        }

        internal static HashSet<SKPoint> GetDirectionVectors(SKPoint[] mainShape)
        {
            HashSet<SKPoint> directionVectors = new HashSet<SKPoint>();
            for(int i = 0; i < mainShape.Length-1; i++)
            {
                directionVectors.Add(DirectionVector(mainShape[i], mainShape[i+1]));
            }
            foreach(var directionVector in directionVectors.ToArray())
            {
                directionVectors.Add(new SKPoint(-directionVector.X, -directionVector.Y));
            }
            return directionVectors;
        }

        internal static SKPoint[] GenerateFigureUsingDirectionVectors(HashSet<SKPoint> directionVectors, Random rand, int points)
        {
            List<SKPoint> figureVertices = new List<SKPoint>();

            // Start with a random point as the origin.
            SKPoint currentPoint = new SKPoint(rand.Next(100, 200), rand.Next(100, 200)); // Adjust as needed
            figureVertices.Add(currentPoint);
            var index = rand.Next(directionVectors.Count);
            var directionVector = directionVectors.ElementAt(index);
            for (int i = 1; i < points; i++)
            {
                do
                {
                    index = rand.Next(directionVectors.Count);
                }while (AngleBetweenVectorsAbs(directionVector, directionVectors.ElementAt(index)) < 0.25f);
                directionVector = directionVectors.ElementAt(index);
                SKPoint chosenDirection = directionVectors.ElementAt(index);
                float scale = rand.Next(3, 10); // Adjust scaling as needed
                SKPoint scaledVector = new SKPoint(chosenDirection.X * scale, chosenDirection.Y * scale);

                // Compute the next point.
                currentPoint = new SKPoint(currentPoint.X + scaledVector.X, currentPoint.Y + scaledVector.Y);
                figureVertices.Add(currentPoint);
            }
            return figureVertices.ToArray();
        }

        internal static void MovePointTowardsDirectionVectorToMatchBothDirectionVectors(ref SKPoint MovedPoint, SKPoint DirectionVector, SKPoint TowardsPoint, HashSet<SKPoint> DirectionVectors)
        {
            DirectionVector = Normalize(DirectionVector);

            SKPoint closestIntersection = new SKPoint(float.MaxValue, float.MaxValue);
            float minDistance = float.MaxValue;

            foreach (SKPoint dir in DirectionVectors)
            {
                SKPoint normalizedDir = Normalize(dir);
                var intersection = FindRayIntersection(MovedPoint, DirectionVector, TowardsPoint, normalizedDir);

                if (intersection != null)
                {
                    var intersect = (SKPoint)intersection;
                    float distance = DistanceSquared(MovedPoint, intersect);
                    if (distance < minDistance&& distance>0&&distance<25000)
                    {
                        {
                            minDistance = distance;
                            closestIntersection = intersect;
                        }
                    }
                }
            }
            if (closestIntersection != new SKPoint(float.MaxValue, float.MaxValue))
            {
                MovedPoint = closestIntersection;
            }
        }

        public static Nullable<SKPoint> FindRayIntersection(SKPoint p1, SKPoint directionVector1, SKPoint p2, SKPoint directionVector2)
        {
            float det = directionVector1.X * directionVector2.Y - directionVector1.Y * directionVector2.X;
            if (MathF.Abs(det) < 1e-6) // Parallel rays
                return null;

            float u = ((p2.X - p1.X) * directionVector2.Y - (p2.Y - p1.Y) * directionVector2.X) / det;
            float v = ((p2.X - p1.X) * directionVector1.Y - (p2.Y - p1.Y) * directionVector1.X) / det;

            if (u < 0 || v < 0) // Intersection point is behind one of the starting points
                return null;

            return new SKPoint(p1.X + u * directionVector1.X, p1.Y + u * directionVector1.Y);
        }


        public static float DistanceSquared(SKPoint a, SKPoint b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        public static SKPoint Multiply(SKPoint point, float scalar)
        {
            return new SKPoint(point.X * scalar, point.Y * scalar);
        }

        internal static void AdjustFigureToRequiredArea(ref SKPoint[] figure, float minArea, float maxArea, Random rand)
        {
            float area = FSMath.CalculateArea(figure);
            
            if (area < minArea || area > maxArea)
            {
                while (area == 0)//once in 40 000 
                {
                    figure = SquirGenerator.GenerateCompletelyRandomShape(9, 100, 100, rand);
                    area = FSMath.CalculateArea(figure);
                    
                }

                var size = FSMath.ShapeSize(figure);

                //get proportion of size and area, and x to y ratio and then scale shape to keep the ratios but change the size, use FSMath.ScaleShape(figure, widgh,height)
                var xtoy = size.X / size.Y;
                var sizetoarea = size.X * size.Y / area;
                var newarea = rand.NextFloat((int)minArea, (int)maxArea) * sizetoarea;// ;
                var newWidth = (float)Math.Sqrt(newarea * xtoy);
                var newHeight = (float)Math.Sqrt(newarea / xtoy);
                FSMath.ScaleShape(ref figure, newWidth, newHeight);
            }
        }
    }
}
