using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fillsquir.Controls
{
    internal class GameSettings
    {
        public GameSettings(int seed ,int fragments, int vertices)
        {
            DetermineDimensions(fragments);
            rand = new Random(seed);
            this.fragments = fragments;
            this.vertices = vertices;
        }
        internal int Rows;
        internal int Cols;
        internal double AreaFilled;
        internal double percentageRequired = 100;
        internal double percentageFilled
        { get { return AreaFilled / MaxArea; } }
        internal double MaxArea;
        internal Random rand;
        internal float zoomFactor = 1.0f;
        internal float xoffset = 0.5f;
        internal float yoffset = 0;
        internal int fragments;
        internal int vertices;
        private void DetermineDimensions(int n)
        {
            int start = (int)Math.Sqrt(n);
            while (true) 
            {
                for (int r = start; r > 0; r--)
                {
                    int c = n / r;
                    if (r / (float) c >= 2.5f && r / (float) c <= 3.5f)
                    {
                        Rows = r;
                        Cols = c;
                        return;
                    }
                    if (c / (float) r >= 2.5f && c / (float) r <= 3.5f)
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
