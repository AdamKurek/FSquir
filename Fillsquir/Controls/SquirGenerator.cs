using SkiaSharp;

namespace Fillsquir.Controls
{
    internal static class SquirGenerator
    {
        static Random rand = new Random();

        static SKPoint RandomPoint(float maxX, float maxY)
        {
            return new SKPoint(rand.NextFloat(0, maxX), rand.NextFloat(0, maxY));
        }

        static SKPoint Centroid(SKPoint[] points)
        {
            float centroidX = 0, centroidY = 0;

            foreach (var point in points)
            {
                centroidX += point.X;
                centroidY += point.Y;
            }

            return new SKPoint(centroidX / points.Length, centroidY / points.Length);
        }

        public static float NextFloat(this Random rand, float min, float max)
        {
            return (float)rand.NextDouble() * (max - min) + min;
        }


        static SKPoint PointOnCircle(SKPoint center, float radius, double angle)
        {
            float x = center.X + radius * (float)Math.Cos(angle);
            float y = center.Y + radius * (float)Math.Sin(angle);
            return new SKPoint(x, y);
        }

        public static SKPoint[] GenerateOrderedMainShapeOnCircle(SKPoint center, float minRadius, float maxRadius, int numberOfPoints)
        {
            List<double> angles = new List<double>();
            for (int i = 0; i < numberOfPoints; i++)
            {
                angles.Add(2 * Math.PI * rand.NextDouble());
            }

            // Sort angles
            angles.Sort();

            SKPoint[] points = new SKPoint[numberOfPoints];
            for (int i = 0; i < numberOfPoints; i++)
            {
                float randomRadius = minRadius + (maxRadius - minRadius) * (float)rand.NextDouble();
                points[i] = PointOnCircle(center, randomRadius, angles[i]);
            }

            return points;
        }

        public static SKPoint[] GenerateCompletelyRandomShape(int numberOfPoints, float maxX, float maxY)
        {
            SKPoint[] points = new SKPoint[numberOfPoints];
            for (int i = 0; i < numberOfPoints; i++)
            {
                points[i] = RandomPoint(maxX, maxY);

            }
            //get max x and y from points
            float maxx = 0, maxy = 0,
            minx = float.MaxValue, miny = float.MaxValue;
            foreach (var point in points)
            {
                if (point.X > maxx)
                {
                    maxx = point.X;
                }
                else if (point.X < minx)
                {
                    minx = point.X;
                }
                if (point.Y > maxy)
                {
                    maxy = point.Y;
                }
                else if (point.Y < miny)
                {
                    miny = point.Y;
                }
            }
            //now i want to scale all these points to fit in the 1000 Y and X
            float scalex = 1000 / (maxx - minx);
            float scaley = 1000 / (maxy - miny);
            for (int i = 0; i < numberOfPoints; i++)
            {
                points[i] = new SKPoint((points[i].X - minx) * scalex, (points[i].Y - miny) * scaley);
            }

            FSMath.EnsureFigureDirection(ref points);


            //while ((points[i].X < 30.0f && points[i].Y < 30.0f) || points[i])
            {
                //   points[i] = RandomPoint(maxX, maxY);
            }

            return points;
        }

        public static List<SKPoint[]> GenerateShapes(int numShapes, SKPoint[] mainShape)
        {
            SKPoint center = Centroid(mainShape);
            List<SKPoint[]> shapes = new List<SKPoint[]>();

            for (int i = 0; i < numShapes; i++)
            {
                SKPoint pointStart = mainShape[i];
                SKPoint pointEnd = mainShape[(i + 1) % mainShape.Length];
                SKPoint midpointStart = new SKPoint((pointStart.X + center.X) / 2, (pointStart.Y + center.Y) / 2);
                SKPoint midpointEnd = new SKPoint((pointEnd.X + center.X) / 2, (pointEnd.Y + center.Y) / 2);

                //  if (numShapes == mainShape.Length) // same number of shapes as original points, generate quads
                {
                    //  shapes.Add(new SKPoint[] { center, midpointStart, pointEnd,  });
                }
                //else
                {

                    var triangle = new SKPoint[] { center, pointStart, midpointEnd };
                    FSMath.EnsureTriangleDirection(ref triangle);
                    //make sure triangle is higher than 30 units
                    //EnsureTriangleShape(ref triangle);
                    shapes.Add(triangle);
                }
            }
            return shapes;
        }

        public static List<SKPoint[]> GenerateCompletelyRandomShapes(int numShapes, SKPoint[] mainShape)
        {
            SKPoint center = Centroid(mainShape);
            List<SKPoint[]> shapes = new List<SKPoint[]>();

            for (int i = 0; i < numShapes; i++)
            {
                SKPoint[] figure;

                var rng = rand.NextDouble();
                if (rng > 0.6)
                {

                    SKPoint pointStart = mainShape[i];
                    SKPoint pointEnd = mainShape[(i + 1) % mainShape.Length];
                    SKPoint midpointStart = new SKPoint((pointStart.X + center.X) / 2, (pointStart.Y + center.Y) / 2);
                    SKPoint midpointEnd = new SKPoint((pointEnd.X + center.X) / 2, (pointEnd.Y + center.Y) / 2);
                    if (rng > 0.6)
                    {
                        figure = new SKPoint[] { center, pointStart, midpointEnd };
                    }
                    else
                    {
                        figure = new SKPoint[] { center, midpointStart, pointEnd, midpointEnd };
                    }
                }
                else if (rng > 0.9)
                {
                    //take random point from random figure in shapes
                    if (shapes.Count == 0)
                    {
                        if (rand.NextDouble() > 0.5)
                        {
                            figure = GenerateCompletelyRandomShape(rand.Next(3, 4), rand.Next(700, 1000), rand.Next(30, 100));
                        }
                        else
                        {
                            figure = GenerateCompletelyRandomShape(rand.Next(3, 4), rand.Next(30, 100), rand.Next(700, 1000));
                        }
                    }
                    else
                    {
                        var randomFigure = shapes[rand.Next(0, shapes.Count)];
                        var randomPointIndex = rand.Next(0, randomFigure.Length-1);

                        //what should i put in this point?
                        if (center == randomFigure[randomPointIndex] || center == randomFigure[randomPointIndex + 1])
                        {
                            figure = new SKPoint[] { randomFigure[randomPointIndex], randomFigure[randomPointIndex], center };
                        }
                        else
                        {
                            figure = new SKPoint[] { randomFigure[randomPointIndex], randomFigure[randomPointIndex], RandomPoint(1000,1000) };
                        }
                    }
                }
                else
                {
                    figure = GenerateCompletelyRandomShape(rand.Next(3, 7), rand.Next(30, 500), rand.Next(30, 500));
                }
                FSMath.EnsureFigureDirection(ref figure);
                while (FSMath.CalculateArea(figure) > 50000)
                {
                    int randomPointIndex = rand.Next(0,figure.Length);
                    var difference = new SKPoint(center.X - figure[randomPointIndex].X, center.Y - figure[randomPointIndex].Y);
                    figure[randomPointIndex].X += difference.X;
                    figure[randomPointIndex].Y += difference.Y;
                    FSMath.EnsureFigureDirection(ref figure);
                }
                while (FSMath.CalculateArea(figure) < 2000)
                {
                    //take point that is the closest to the center and move it away from the center
                    float mindist = float.MaxValue;
                    int closestPointIndex = new();
                    //use for loop instead 
                    for(int j = 0; j < figure.Length; j++)
                    {
                        var dist = FSMath.CalculateDistance(figure[j], center);
                        if(dist < mindist)
                        {
                            mindist = dist;
                            closestPointIndex = j;
                        }
                    }
                    var difference = new SKPoint(center.X - figure[closestPointIndex].X, center.Y - figure[closestPointIndex].Y);
                    figure[closestPointIndex].X += difference.X;
                    figure[closestPointIndex].Y += difference.Y;
                    FSMath.EnsureFigureDirection(ref figure);
                }
                    
                shapes.Add(figure);
            }

            
            var temp = shapes[0];
            shapes[0] = shapes[rand.Next(1, shapes.Count)];
            shapes[rand.Next(1, shapes.Count)] = temp;

            return shapes;
        }


        /*
        public static void ExtendToParallel(SKPoint[] trianglePoints, SKPoint[] parallelPoints)
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
                SKPoint middlePoint = CalculateMiddlePoint(trianglePoints);
                SKPoint startPoint = parallelPoints[maxIndex];
                SKPoint endPoint = parallelPoints[maxIndex + 1];
                double lineAngle = CalculateAngle(startPoint, endPoint);
                double extendLength = maxLength - CalculateDistance(startPoint, endPoint);
                SKPoint extendedPoint = ExtendLine(middlePoint, lineAngle, extendLength);

                // Update the middle point with the extended point
                trianglePoints[1] = extendedPoint;
            }
        }

        private static double CalculateAngle(SKPoint pointA, SKPoint pointB, SKPoint pointC)
        {
            double a = CalculateDistance(pointB, pointC);
            double b = CalculateDistance(pointA, pointC);
            double c = CalculateDistance(pointA, pointB);

            return Math.Acos((Math.Pow(a, 2) + Math.Pow(b, 2) - Math.Pow(c, 2)) / (2 * a * b));
        }

        private static double CalculateDistance(SKPoint pointA, SKPoint pointB)
        {
            double dx = pointA.X - pointB.X;
            double dy = pointA.Y - pointB.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static SKPoint CalculateMiddlePoint(SKPoint[] points)
        {
            SKPoint middlePoint = new SKPoint();
            middlePoint.X = (points[0].X + points[1].X + points[2].X) / 3;
            middlePoint.Y = (points[0].Y + points[1].Y + points[2].Y) / 3;
            return middlePoint;
        }

        private static SKPoint ExtendLine(SKPoint startPoint, double angle, double length)
        {
            double dx = Math.Cos(angle) * length;
            double dy = Math.Sin(angle) * length;
            return new SKPoint(startPoint.X + (float)dx, startPoint.Y + (float)dy);
        }
        */
    }
}
