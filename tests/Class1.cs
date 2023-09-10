using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace tests
{
    internal class Class1
    {

        internal struct SKPoint { 
            public float X { get; set; }
            public float Y { get; set; }
            public SKPoint(float x, float y)
            {
                X = x;
                Y = y;
            }
        }
        internal static SKPoint GetFurtherstDirectionVector(SKPoint[] Shape, SKPoint DirectionVector)
        {
            SKPoint mostDifferentDirection = new SKPoint();
            double closestToOrthogonal = double.MaxValue;  // initialized to the largest possible value

            SKPoint normalizedDirectionVector = Normalize(DirectionVector);

            for (int i = 0; i < Shape.Length; i++)
            {
                SKPoint nextPoint = (i == Shape.Length - 1) ? Shape[0] : Shape[i + 1];
                // Get the direction between current point and next point
                SKPoint currentDirection = new SKPoint(nextPoint.X - Shape[i].X, nextPoint.Y - Shape[i].Y);
                SKPoint normalizedCurrentDirection = Normalize(currentDirection);
                var dotProduct = DotProduct(normalizedDirectionVector, normalizedCurrentDirection);
                //is dot product always positive?
                var distanceFromOrthogonal = MathF.Abs(dotProduct);  

                if (distanceFromOrthogonal < closestToOrthogonal)
                {
                    closestToOrthogonal = distanceFromOrthogonal;
                    mostDifferentDirection = currentDirection;
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

    }
}
