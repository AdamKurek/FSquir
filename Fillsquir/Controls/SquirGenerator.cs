﻿using System;
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

                    var triangle = new PointF[] { center, pointStart, midpointEnd };
                    FSMath.EnsureTriangleDirection(ref triangle);
                    //make sure triangle is higher than 30 units
                    //EnsureTriangleShape(ref triangle);
                    shapes.Add(triangle);
                }
            }

            //    shapes = new List<PointF[]>();
            //shapes.Add(new PointF[] {  new PointF((float)519.32, (float)730.7), new PointF((float)419.2, (float)501.60), new PointF((float)463.8, (float)546.3) });
            //shapes.Add(new PointF[] { new PointF((float)800.2, (float)501.60), new PointF((float)519.32, (float)730.7), new PointF((float)463.8, (float)546.3) });
            //shapes.Add(new PointF[] { new PointF((float)0.0, (float)0), new PointF((float)150, (float)0), new PointF((float)100, (float)-10) });

            // why all the shapes in a loop don't count as shapes and only the last one is counted?
            // can you run trough entire code and see if you can find the problem?



            //shapes[0] = new PointF[]{ new PointF { X =0,Y = 0 }, new PointF { X = 1000, Y = 0 }, new PointF { X = 1000, Y = 10000 } };
            return shapes;
        }

        /*
        public static void ExtendToParallel(PointF[] trianglePoints, PointF[] parallelPoints)
        {
            // Calculate the angles of the triangle
            double angleA = CalculateAngle(trianglePoints[0], trianglePoints[1], trianglePoints[2]);
            double angleB = CalculateAngle(trianglePoints[1], trianglePoints[2], trianglePoints[0]);
            double angleC = CalculateAngle(trianglePoints[2], trianglePoints[0], trianglePoints[1]);

            // Check if any angle is less than 1/2 of a radian
            if (angleA < Math.PI / 2 || angleB < Math.PI / 2 || angleC < Math.PI / 2)
            {
                // Find the longest line in the parallelPoints array
                double maxLength = 0;
                int maxIndex = 0;
                for (int i = 0; i < parallelPoints.Length - 1; i++)
                {
                    double length = CalculateDistance(parallelPoints[i], parallelPoints[i + 1]);
                    if (length > maxLength)
                    {
                        maxLength = length;
                        maxIndex = i;
                    }
                }

                // Calculate the parallel line from the middle point of the triangle
                PointF middlePoint = CalculateMiddlePoint(trianglePoints);
                PointF startPoint = parallelPoints[maxIndex];
                PointF endPoint = parallelPoints[maxIndex + 1];
                double lineAngle = CalculateAngle(startPoint, endPoint);
                double extendLength = maxLength - CalculateDistance(startPoint, endPoint);
                PointF extendedPoint = ExtendLine(middlePoint, lineAngle, extendLength);

                // Update the middle point with the extended point
                trianglePoints[1] = extendedPoint;
            }
        }

        private static double CalculateAngle(PointF pointA, PointF pointB, PointF pointC)
        {
            double a = CalculateDistance(pointB, pointC);
            double b = CalculateDistance(pointA, pointC);
            double c = CalculateDistance(pointA, pointB);

            return Math.Acos((Math.Pow(a, 2) + Math.Pow(b, 2) - Math.Pow(c, 2)) / (2 * a * b));
        }

        private static double CalculateDistance(PointF pointA, PointF pointB)
        {
            double dx = pointA.X - pointB.X;
            double dy = pointA.Y - pointB.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static PointF CalculateMiddlePoint(PointF[] points)
        {
            PointF middlePoint = new PointF();
            middlePoint.X = (points[0].X + points[1].X + points[2].X) / 3;
            middlePoint.Y = (points[0].Y + points[1].Y + points[2].Y) / 3;
            return middlePoint;
        }

        private static PointF ExtendLine(PointF startPoint, double angle, double length)
        {
            double dx = Math.Cos(angle) * length;
            double dy = Math.Sin(angle) * length;
            return new PointF(startPoint.X + (float)dx, startPoint.Y + (float)dy);
        }
        */
    }
}
