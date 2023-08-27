using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;

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
        public static PointF[] SutherlandHodgman(PointF[] subjectPolygon, PointF[] clipPolygon)
        {
            List<PointF> outputList = new List<PointF>(subjectPolygon);

            for (int i = 0; i < clipPolygon.Length; i++)
            {
                PointF clipEdgeStart = clipPolygon[i];
                PointF clipEdgeEnd = clipPolygon[(i + 1) % clipPolygon.Length];

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

