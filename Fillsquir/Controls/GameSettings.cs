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
        internal GameSettings(int seed, int fragments, int vertices)
        {
            this.fragments = fragments;
            this.vertices = vertices;
            DetermineDimensions(fragments);
            untouchedFragments = new Fragment[Rows,Cols];
            rand = new Random(seed);
        }
        internal int Rows;
        internal int Cols;
        internal int VisibleRows {get{ return Rows < 5? Rows:5;} }
        internal double AreaFilled;
        internal double percentageRequired = 100;
        internal double percentageFilled
        { get { return AreaFilled / MaxArea; } }
        internal double MaxArea;
        internal Random rand;
        internal float zoomFactor = 1.5f;
        internal float xoffset = 0.5f;
        internal float yoffset = 0;
        internal int fragments;
        internal int vertices;
        internal float bottomStripMove = 0;
        internal Fragment[,] untouchedFragments;
        
        public float prop1 = 3;
        public float prop2 = 4;
        private void DetermineDimensions(int n)
        {
            if (n < 4)
            {
                Cols = 1;
                Rows = n;
            }
            Cols = 2;
            Rows = n/2;
        return;

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
