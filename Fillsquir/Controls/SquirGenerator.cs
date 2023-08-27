using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fillsquir.Controls
{
    internal static class SquirGenerator
    {
        static Random rand = new Random();

        static PointF RandomPoint(float maxX, float maxY)
        {
            return new PointF(rand.NextFloat(0, maxX), rand.NextFloat(0, maxY));
        }

        static PointF Centroid(PointF[] points)
        {
            float centroidX = 0, centroidY = 0;

            foreach (var point in points)
            {
                centroidX += point.X;
                centroidY += point.Y;
            }

            return new PointF(centroidX / points.Length, centroidY / points.Length);
        }

        public static float NextFloat(this Random rand, float min, float max)
        {
            return (float)rand.NextDouble() * (max - min) + min;
        }


        static PointF PointOnCircle(PointF center, float radius, double angle)
        {
            float x = center.X + radius * (float)Math.Cos(angle);
            float y = center.Y + radius * (float)Math.Sin(angle);
            return new PointF(x, y);
        }

        public static PointF[] GenerateOrderedMainShapeOnCircle(PointF center, float minRadius, float maxRadius, int numberOfPoints)
        {
            List<double> angles = new List<double>();
            for (int i = 0; i < numberOfPoints; i++)
            {
                angles.Add(2 * Math.PI * rand.NextDouble());
            }

            // Sort angles
            angles.Sort();

            PointF[] points = new PointF[numberOfPoints];
            for (int i = 0; i < numberOfPoints; i++)
            {
                float randomRadius = minRadius + (maxRadius - minRadius) * (float)rand.NextDouble();
                points[i] = PointOnCircle(center, randomRadius, angles[i]);
            }

            return points;
        }


        public static List<PointF[]> GenerateShapes(int numShapes, PointF[] mainShape)
        {
            PointF center = Centroid(mainShape);
            List<PointF[]> shapes = new List<PointF[]>();

            for (int i = 0; i < numShapes; i++)
            {
                PointF pointStart = mainShape[i];
                PointF pointEnd = mainShape[(i + 1) % mainShape.Length];
                PointF midpointStart = new PointF((pointStart.X + center.X) / 2, (pointStart.Y + center.Y) / 2);
                PointF midpointEnd = new PointF((pointEnd.X + center.X) / 2, (pointEnd.Y + center.Y) / 2);

              //  if (numShapes == mainShape.Length) // same number of shapes as original points, generate quads
                {
                  //  shapes.Add(new PointF[] { center, midpointStart, pointEnd,  });
                }
                //else
                {
                    shapes.Add(new PointF[] { center, pointStart, midpointEnd });
                }
            }

            return shapes;
        }
    }
}
