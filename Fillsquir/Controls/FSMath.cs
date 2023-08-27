using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Clipper2Lib;
using Microsoft.Maui.Graphics;
using Path = System.Collections.Generic.List<Microsoft.Maui.Graphics.PointF>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<Microsoft.Maui.Graphics.PointF>>;

namespace Fillsquir.Controls
{
    internal static class FSMath
    {
        public static bool IsPointInShape(Point point, ICollection<PointF> figure)
        {
            if (figure.Count < 3) // A valid polygon needs at least 3 vertices
            {
                return false;
            }

            PointF outsidePoint = new PointF(-9999, -9999); // Point outside the shape
            int intersectCount = 0;

            PointF prevPoint = new PointF(float.NaN, float.NaN);
            foreach (PointF currentPoint in figure)
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


        public static bool OnSegment(PointF p, PointF q, PointF r)
        {
            return q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) &&
                   q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y);
        }
        public static bool DoSegmentsIntersect(Point p1, PointF q1, PointF p2, PointF q2)
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

        public static float Orientation(Point a, PointF b, PointF c)
        {
            float result = (float)((b.Y - a.Y) * (c.X - b.X) - (b.X - a.X) * (c.Y - b.Y));
            if (result == 0) return 0;  // Collinear
            return (result > 0) ? 1 : -1; // Clockwise or Counterclockwise
        }

        // common area
        public static PointF[] SutherlandHodgman(PointF[] p1, PointF[] p2)
        {
            List<PointF> outputList = new List<PointF>(p1);

            for (int i = 0; i < p2.Length; i++)
            {
                PointF clipEdgeStart = p2[i];
                PointF clipEdgeEnd = p2[(i + 1) % p2.Length];

                List<PointF> inputList = new List<PointF>(outputList);
                outputList.Clear();

                if (inputList.Count == 0)
                {
                    break;
                }

                PointF prevPoint = inputList[inputList.Count - 1];
                foreach (PointF currentPoint in inputList)
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

        /*public static List<PointF[]> CommonArea(PointF[] p1, PointF[] p2)
        {
            List<List<PointF>> subj = new List<List<PointF>>(1), clip = new List<List<PointF>>(1), solution = new List<List<PointF>>();
            subj.Add(new List<PointF>());
            clip.Add(new List<PointF>());

            // Convert PointF to IntPoint (Clipper's integer point) and populate 'subj' and 'clip'
            foreach (PointF p in p1)
            {
                subj[0].Add(new IntPoint((long)(p.X * 1e6), (long)(p.Y * 1e6)));
            }

            foreach (PointF p in p2)
            {
                clip[0].Add(new IntPoint((long)(p.X * 1e6), (long)(p.Y * 1e6)));
            }

            Clipper c = new Clipper();
           // c.AddPolygon(subj, PolyType.ptSubject);
          //  c.AddPolygon(clip, PolyType.ptClip);
            c.Execute(ClipType.ctIntersection, solution, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);

            // Convert the result back to PointF and return
            List<PointF[]> result = new List<PointF[]>();
            foreach (List<IntPoint> path in solution)
            {
                List<PointF> pfList = new List<PointF>();
                foreach (IntPoint ip in path)
                {
                    pfList.Add(new PointF((float)ip.X / 1e6f, (float)ip.Y / 1e6f));
                }
                result.Add(pfList.ToArray());
            }

            return result;
        }*/
        /*public static List<List<PointF[]>> CommonArea2(PointF[] p1, List<PointF[]> p2)
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

            // Convert solutions to a list of lists of PointF arrays
            List<List<PointF[]>> result = new List<List<PointF[]>>();
            foreach (var solution in solutions)
            {
                List<PointF[]> polygons = new List<PointF[]>();
                foreach (var path in solution)
                {
                    PointF[] points = new PointF[path.Count];
                    for (int i = 0; i < path.Count; i++)
                    {
                        points[i] = new PointF((float)path[i].X / 1000, (float)path[i].Y / 1000);
                    }
                    polygons.Add(points);
                }
                result.Add(polygons);
            }

            return result;
        }*/
        private static Path64 PointFArrayToPath64(PointF[] points)
        {
            Path64 path = new Path64();
            foreach (PointF point in points)
            {
                // Clipper's Point64 constructor takes longs, so we need to convert the PointF's floats to longs.
                // We can do this by rounding the float to the nearest integer, since PointF uses single-precision floating point numbers.
                long x = (long)Math.Round(point.X);
                long y = (long)Math.Round(point.Y);
                path.Add(new Point64(x, y));
            }
            return path;
        }
        private static List<PointF[]> Path64ToPointFArrayList(Path64 path)
        {
            List<PointF[]> list = new List<PointF[]>();
            PointF[] points = new PointF[path.Count];
            for (int i = 0; i < path.Count; i++)
            {
                points[i] = new PointF((float)path[i].X, (float)path[i].Y);
            }
            list.Add(points);
            return list;
        }
        public static List<PointF[]> CommonArea(PointF[] p1, List<PointF[]> p2)
        {
            Paths64 subject = new Paths64();
            Paths64 clip = new Paths64();

            // Convert PointF[] to Path64 and add to subject
            subject.Add(PointFArrayToPath64(p1));

            // Convert each PointF[] in the list to Path64 and add to clip
            foreach (var figure in p2)
            {
                clip.Add(PointFArrayToPath64(figure));
            }

            // Find the common area
            Paths64 commonArea = Clipper.Intersect(subject, clip, FillRule.NonZero);

            // Convert the common area from Paths64 to List<List<PointF[]>> for the return value
            List<PointF[]> result = new List<PointF[]>();
            foreach (var path in commonArea)
            {
                result.AddRange(Path64ToPointFArrayList(path));
            }
            return result;
        }
        public static bool IsInside(PointF clipEdgeStart, PointF clipEdgeEnd, PointF point)
        {
            return (clipEdgeEnd.Y - clipEdgeStart.Y) * (point.X - clipEdgeStart.X) -
                   (clipEdgeEnd.X - clipEdgeStart.X) * (point.Y - clipEdgeStart.Y) >= 0;
        }

        public static PointF Intersection(PointF clipEdgeStart, PointF clipEdgeEnd, PointF lineStart, PointF lineEnd)
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

            return new PointF(x, y);
        }
    }
}

