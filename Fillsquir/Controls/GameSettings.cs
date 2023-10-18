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
            untouchedFragments = new Fragment[Cols,Rows];
            rand = new Random(seed);
        }
        internal int Cols;
        internal int Rows;
        internal int VisibleRows {get{ return Cols < 5? Cols:5;} }
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
        internal float bottomStripRise = 0;
        internal float bottomStripMove = 0;
        internal Fragment[,] untouchedFragments;
        internal List<Fragment> touchedFragments;

        internal List<Fragment> CenterFragments = new();
        internal List<Fragment> TooLeftFragments = new();
        internal List<Fragment> TooRightFragments = new();
        internal List<Fragment> TooTopFragments = new();
        internal List<Fragment> TooBottomFragments = new();

        public static void MoveFragmentsBetweenLists(List<Fragment> sourceList, List<Fragment> destinationList, Func<Fragment, bool> condition)
        {
            List<int> indexesToMove = new List<int>();

            for (int i = 0; i < sourceList.Count; i++)
            {
                var fragment = sourceList[i];

                if (condition(fragment))
                {
                    indexesToMove.Add(i);
                }
            }

            for (int i = indexesToMove.Count - 1; i >= 0; i--)
            {
                int index = indexesToMove[i];
                var item = sourceList[index];
                destinationList.Add(item);
                sourceList.RemoveAt(index);
            }
        }

        internal Fragment touchFragment(int row,int col)
        {
            var curr = untouchedFragments[row, col];
            untouchedFragments[row, col] = null;
            return curr;
        }
        internal bool untouchFragment(Fragment untouchedman, int row,int col)
        {
            if (untouchedFragments[row, col] != null) 
                return false;
            untouchedFragments[row, col] = untouchedman;
            return true;
        }
        
        public float prop1 = 3;
        public float prop2 = 4;
        private void DetermineDimensions(int n)
        {
            if (n < 4)
            {
                Rows = 1;
                Cols = n;
                return;
            }
            Rows = 2;
            Cols = n/2;
        return;

            int start = (int)Math.Sqrt(n);
            while (true) 
            {
                for (int r = start; r > 0; r--)
                {
                    int c = n / r;
                    if (r / (float) c >= 2.5f && r / (float) c <= 3.5f)
                    {
                        Cols = r;
                        Rows = c;
                        return;
                    }
                    if (c / (float) r >= 2.5f && c / (float) r <= 3.5f)
                    {
                        Cols = c;
                        Rows = r;
                        return;
                    }
                }
                start++;
            }
        }
    }
}
