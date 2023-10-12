using SkiaSharp;
using System.Linq.Expressions;

namespace Fillsquir.Controls
{
    internal static class SquirGenerator
    {
        static int cringeSum; //delete
        static SKPoint RandomPoint(float minX, float minY, float maxX, float maxY, Random rand)
        {
            return new SKPoint(rand.NextFloat(minX, maxX), rand.NextFloat(minY, maxY));
        }

        static SKPoint RandomPoint(float maxX, float maxY, Random rand)
        {
            return new SKPoint(rand.NextFloat(0, maxX), rand.NextFloat(0, maxY));
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

        public static SKPoint[] GenerateOrderedMainShapeOnCircle(SKPoint center, float minRadius, float maxRadius, int numberOfPoints, Random rand)
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

        public static SKPoint[] GenerateCompletelyRandomShape(int numberOfPoints, float maxX, float maxY, Random rand)
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
        
   
        public static List<SKPoint[]> GenerateShapes(int numShapes, SKPoint[] mainShape)
        {
            SKPoint center = FSMath.Centroid(mainShape);
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

        public static List<SKPoint[]> GenerateCompletelyRandomShapes(int numShapes, SKPoint[] mainShape, Random rand)
        {
            cringeSum = 0;
            SKPoint center = FSMath.Centroid(mainShape);
            List<SKPoint[]> shapes = new List<SKPoint[]>();
            var squirArea = FSMath.CalculateArea(mainShape);
            var minArea = squirArea / 42;
            var maxArea = squirArea / 7;
            HashSet<SKPoint> DirectionVectors = FSMath.GetDirectionVectors(mainShape);
            for (int i = 0; i < numShapes; i++)
            {
                try
                {
#region generate random figure
                    SKPoint[] figure;
                    var rng = 0.97;;
                    cringeSum++;
                    if(cringeSum == 12)
                    {
                        Console.Write("xd");
                    }
                    if (rng < 0.95)
                    {
                        int points = rand.Next(3, 5);
                        figure = FSMath.GenerateFigureUsingDirectionVectors(DirectionVectors, rand, points);
                        FSMath.MovePointTowardsDirectionVectorToMatchBothDirectionVectors(ref figure[figure.Length - 1], FSMath.DirectionVector(figure[figure.Length - 2], figure[figure.Length - 1]), figure[0], DirectionVectors);

                        //foreach (var dv in FSMath.GetDirectionVectors(figure))
                        {
                            //DirectionVectors.Add(dv);
                        }
                        if (!FSMath.EnsureFigureDirection(ref figure))
                        {
#if DEBUG
                            FSMath.randit++;
#endif
                            i--;
                            continue;
                        }
                        // if (FSMath.minBounds(figure).X < 0 || FSMath.minBounds(figure).Y < 0)
                        FSMath.AdjustFigureToRequiredArea(ref figure, minArea, maxArea, rand);



                        if (!FSMath.EnsureFigureDirection(ref figure))
                        {
#if DEBUG
                            FSMath.randit++;
#endif
                            i--;
                            continue;
                        }

                    }
                    else
                    {
                        //take random point from random figure in shapes
                        if (shapes.Count == 0)//==!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                        {
                            //generate random shape that will be very long and thin by creating random point and moving him away from center in 2 sides, then make 2 shapes around these points and combine them into 1 shape
                            var randomPoint = RandomPoint(451, 549, rand);
                            //copy this point and move it away from center
                            var distance = rand.NextFloat(200, 450);
                            var randomPoint1 = FSMath.MovePointAwayFrom(randomPoint, center, distance);
                            var randomPoint2 = FSMath.MovePointAwayFrom(randomPoint, center, -distance);
                            //make SKPoint arrays with 1-3 points and combine them into 1 shape
                            var newDirection = FSMath.GetFurtherstDirectionVector(mainShape, FSMath.DirectionVector(randomPoint1, randomPoint2));
                            //do bitwise operation so i randomize form 1 to 4 and if it's 1 i do stuff if it's 2 i go elsewhere and if it's 4 i go both ways
                            var randomizer = rand.Next(0, 3);
                            List<SKPoint> figureList = new List<SKPoint>
                        {
                            randomPoint1,
                            randomPoint2
                        };
                            if (randomizer <= 1)
                            {
                                figureList.Add(FSMath.MoveTowardsDirectionVector(randomPoint1, newDirection, 250f - (distance / 2f)));
                            }
                            if (randomizer >= 1)
                            {
                                figureList.Add(FSMath.MoveTowardsDirectionVector(randomPoint2, newDirection, 250f - (distance / 2f)));
                            }
                            figure = figureList.ToArray();
                            FSMath.EnsureFigureDirection(ref figure);
                        }
                        else
                        {
                            var randomizer = rand.Next(0, shapes.Count);
                            SKPoint[] randomFigure;
                            if (randomizer == shapes.Count)
                            {
                                randomFigure = mainShape;
                            }
                            else
                            {
                                randomFigure = shapes[randomizer];
                            }
                            var randomPointIndex = rand.Next(0, randomFigure.Length);


                            if (center == randomFigure[randomPointIndex] || center == randomFigure[randomPointIndex + 1 >= randomFigure.Length ? 0 : randomPointIndex + 1])
                            {
                                figure = new SKPoint[] { randomFigure[randomPointIndex], randomFigure[randomPointIndex + 1 >= randomFigure.Length ? 0 : randomPointIndex + 1], RandomPoint(1000, 1000, rand) };
                            }
                            else
                            {
                                figure = new SKPoint[] { randomFigure[randomPointIndex], randomFigure[randomPointIndex + 1 >= randomFigure.Length ? 0 : randomPointIndex + 1], center };

                            }
                            var figureArea = FSMath.CalculateArea(figure);



                            if (figureArea < minArea)
                            {
                                int index = FSMath.GetNearestPoint(figure,FSMath.Centroid(figure));
                                FSMath.MovePointAwayFromPoints(ref figure[index], figure[(index+1)%3], figure[(index + 2) % 3], MathF.Sqrt(minArea - figureArea));
                            }
                            else if (figureArea > maxArea)
                            {
                                var wat = figureArea - maxArea;
                                var non = MathF.Sqrt(wat);
                                FSMath.MovePointTowardsPoints(ref figure[2], figure[0], figure[1], non);
                            }
                            if (!FSMath.EnsureFigureDirection(ref figure))
                            {
#if DEBUG
                                FSMath.randit++;
#endif
                                i--;
                                continue;
                            }
                            FSMath.AdjustFigureToRequiredArea(ref figure, minArea, maxArea, rand);
                            if (!FSMath.EnsureFigureDirection(ref figure))
                            {
#if DEBUG
                                FSMath.randit++;
#endif
                                i--;
                                continue;
                            }
                        }
                    }

                    #endregion
                    shapes.Add(figure);
                }catch(Exception)
                {
#if DEBUG
                    FSMath.randit++;
#endif
                    i--;
                    continue;
                }
            }
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
