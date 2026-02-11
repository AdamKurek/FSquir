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
        internal GameSettings(int seed, int level)
        {
            Level = level;
            fragments = level;
            WallAngleCount = WallAngleSet.WallAnglesForLevel(level);
            WallRotationRadians = WallAngleSet.RotationForLevel(seed, level, WallAngleCount);
            WallDirectionsUndirected = WallAngleSet.UndirectedDirections(WallAngleCount, WallRotationRadians);
            WallDirectionsDirected = WallAngleSet.DirectedDirections(WallDirectionsUndirected);

            DetermineDimensions(fragments);
            untouchedFragments = new Fragment[Cols,Rows];
            rand = new Random(seed);
        }

        internal int Level;
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
        internal int WallAngleCount;
        internal float WallRotationRadians;
        internal SkiaSharp.SKPoint[] WallDirectionsUndirected;
        internal SkiaSharp.SKPoint[] WallDirectionsDirected;
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
            var (rows, cols) = LevelLayout.FragmentGrid(n);
            Rows = rows;
            Cols = cols;
        }
    }
}
