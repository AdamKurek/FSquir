using Clipper2Lib;
using SkiaSharp;

namespace Fillsquir.Controls
{
    internal static class FSMath
    {
        
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

        public static double CalculateArea(SKPoint[] figure)
        {
#if DEBUG
            if (figure.Length < 3)
            {
                throw new ArgumentException("Figure must have at least 3 points", "figure");
            }
#endif
            double area = 0;

            for (int i = 0; i < figure.Length - 1; i++)
            {
                area += (figure[i].X * figure[i + 1].Y) - (figure[i].Y * figure[i + 1].X);
            }

            area += (figure[figure.Length - 1].X * figure[0].Y) - (figure[figure.Length - 1].Y * figure[0].X);

            area = Math.Abs(area) / 2;

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
        //can you add function that does the same thing but actually works?
        //I think the problem is that the clipper library is not working properly
        //better alternative is to use the polygon class
        //https://docs.microsoft.com/en-us/dotnet/api/system.windows.media.pathgeometry?view=net-5.0
        //here is example code
       
            



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


        internal static void EnsureFigureDirection(ref SKPoint[] figure)
        {
            if (figure == null || figure.Length < 3)
            {
                throw new ArgumentException("Figure must have at least 3 points", "figure");
            }

            float centroidX = 0, centroidY = 0;
            foreach (var point in figure)
            {
                centroidX += point.X;
                centroidY += point.Y;
            }
            centroidX /= figure.Length;
            centroidY /= figure.Length;

            figure = figure.OrderBy(point =>
            {
                float dx = point.X - centroidX;
                float dy = point.Y - centroidY;
                return Math.Atan2(dy, dx);
            }).ToArray();
        }

      

    }
}

