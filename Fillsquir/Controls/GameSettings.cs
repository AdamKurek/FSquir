using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fillsquir.Controls
{
    internal class GameSettings
    {
        public GameSettings(int Fragments, int seed)
        {
            Rows = Fragments;
            Cols = Fragments;
            Random random = new Random(seed);
        }
        internal int Rows;
        internal int Cols;
        internal double percentageRequired;
        internal Random random;
        private void DetermineDimensions(int n)
        {
            // Start at the square root and find closest factors
            int start = (int)Math.Sqrt(n);

            for (int r = start; r > 0; r--)
            {
                int c = n / r;

                // Check if it's roughly 3:1 proportion
                if (r / (double)c >= 2.5 && r / (double)c <= 3.5)
                {
                    Rows = r;
                    Cols = c;
                    return;
                }

                // You can also check for the inverse proportion, c to r.
                if (c / (double)r >= 2.5 && c / (double)r <= 3.5)
                {
                    Rows = c;
                    Cols = r;
                    return;
                }
            }

            // Default if no suitable factors found
            Rows = n;
            Cols = 1;
        }
    }

}
