using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fillsquir.Controls
{
    internal class GameSettings
    {
        public GameSettings(int seed ,int fragments)
        {
            DetermineDimensions(fragments);
            rand = new Random(seed);
        }
        internal int Rows;
        internal int Cols;
        internal double percentageRequired;
        internal Random rand;
        private void DetermineDimensions(int n)
        {
            int start = (int)Math.Sqrt(n);
            while (true) 
            {
                for (int r = start; r > 0; r--)
                {
                    int c = n / r;
                    if (r / (float)c >= 2.5f && r / (float)c <= 3.5f)
                    {
                        Rows = r;
                        Cols = c;
                        return;
                    }
                    if (c / (float)r >= 2.5f && c / (float)r <= 3.5f)
                    {
                        Rows = c;
                        Cols = r;
                        return;
                    }
                }
                start++;
            }
        }
    }
}
