﻿using Fillsquir.Interfaces;
using SkiaSharp;

namespace Fillsquir.Controls
{
    internal class DrawableStack : GeometryElement
    {
        private float screenWidth = 1000;
        private float screenHeight = 1000;
        public List<GeometryElement> drawables = new();
        public GeometryElement cover;
        internal DrawableStack(GameSettings settings) : base(settings)
        {
        }
        internal GeometryElement Gui { get; set; }
        internal HashSet<SKPoint> allActivePoints(int ignoreIndex)//todo make  update on move
        {
            var set = new HashSet<SKPoint>();
            Squir sq = (Squir)drawables[0];
            foreach (var pt in sq.VisiblePoints)
            {
                set.Add(pt);
            }
            for(int i = 1;i< drawables.Count;i++)
            {
                var drawable = drawables[i] as Fragment;
                if (!drawable.wasTouched) { continue; }
                if (i==ignoreIndex) { continue; }
                foreach (var pt in drawable.VisiblePointsS)
                {
                    set.Add(pt);
                }
            }
            return set;
        }
        internal GeometryElement this[int i]
        {
            get { return drawables[i]; }
            set { drawables[i] = value; }
        }
        internal void AddDrawable(GeometryElement drawable)
        {
            drawables.Add(drawable);
            drawable.Resize(screenWidth, screenHeight);
            //(drawable as GeometryElement).Resize()
        }
        internal void AddCover(GeometryElement drawable)
        {
            cover = drawable;
        }

#if DebugClickingLines
        public class Line
        {
            public Point p;
            public Point q;
        }
        public Line testLine;
        public bool isCrossing;


#endif

#if DebugClicking
        public struct Drawpoint
        {
            public SKPoint point;
            public bool inBounds;
        }
        public List<Drawpoint> clickPoints = new List<Drawpoint> { };
        public void AddDot(SKPoint point, bool inBounds = false)
        {
            Drawpoint drawpoint = new Drawpoint();
            drawpoint.point.X = (float)point.X;
            drawpoint.point.Y = (float)point.Y;
            drawpoint.inBounds = inBounds;
            clickPoints.Add(drawpoint);
        }
#endif
        internal SKCanvas DrawPreZoom(SKCanvas canvas)
        {
            //foreach (Fragment drawable in drawables.Skip(1))
            foreach (Fragment drawable in gameSettings.CenterFragments)
            {
                if (drawable.wasTouched)
                {
                    drawable.Draw(canvas);
                }
            }
            this[0].Draw(canvas);
            cover?.Draw(canvas);

#if DebugClickingLines
            if(isCrossing)
            {
                canvas.StrokeColor= Colors.Magenta;
            }else
            {
                canvas.StrokeColor= Colors.Yellow;  
            }
            if(testLine is not null)
            canvas.DrawLine(testLine.q, testLine.p);
#endif
            return canvas;
        }
        internal SKCanvas DrawPastZoom(SKCanvas canvas)
        {
            //how do i cover 33% of the bottom screen in black colour
            //var wtf = new SKRectl(0f, screenHeight /, screenWidth, screenHeight);
            var rectl = new SKRectI(0, (int)(screenHeight * gameSettings.prop1 / gameSettings.prop2) , (int)screenWidth, (int)screenHeight);
            canvas.DrawRegion(new SKRegion(rectl), new SKPaint() { Color = SKColors.Black });

            //foreach (Fragment drawable in drawables.Skip(1))
            ////foreach (Fragment drawable in gameSettings.CenterFragments)
            //    {
            //        if (!drawable.wasTouched)
            //    {
            //        drawable.Draw(canvas);
            //    }
            //}
            var cols = gameSettings.Cols;
            if (cols > gameSettings.VisibleRows) { cols = gameSettings.VisibleRows; }
            int colsmove = (int)(gameSettings.bottomStripMove / (screenWidth / gameSettings.VisibleRows));
            cols += colsmove + 1;
            //colsmove++;
            //cols++;
            if(cols >= gameSettings.untouchedFragments.Length / gameSettings.Rows)
            {
                cols = gameSettings.untouchedFragments.Length / gameSettings.Rows;
            }
            for (int j = colsmove; j < cols; j++)
            {
                for (int i = 0; i < gameSettings.Rows; i++)
                {
                    gameSettings.untouchedFragments[j, i]?.Draw(canvas);
                }
            }

            //tuteraz

            Gui?.Draw(canvas);
            return canvas;
        }

        internal void DrawFragmentsoutlines(SKCanvas canvas)
        {
           // foreach (Fragment drawable in drawables.Skip(1))
            foreach (Fragment drawable in gameSettings.CenterFragments)

            {
#if DebugVisuals
    drawable.DrawVertices(canvas);
#endif
                    if (drawable.wasTouched)
                {
                    drawable.DrawVertices(canvas);
                }
            }


#if DebugClicking
            foreach (var circle in clickPoints)
            {
                var pt = new SKPaint();
                pt.Color = SKColors.Green;
                if (!circle.inBounds)
                    pt.Color = SKColors.Red;
                canvas.DrawCircle(circle.point.X, circle.point.Y, 1, pt);

            }
#endif
        }

        protected override void ResizePrecize(float width, float height)
        {
            screenWidth = width;
            screenHeight = height;
            foreach(var drawable in drawables)
            {
                drawable.Resize(width, height);
            }
            cover?.Resize(width, height);
        }

        internal Fragment getNearestFragment(SKPoint mousePosition)
        {
            float nearest = float.MaxValue;
            int index = 0;
            for (int i = 1; i < drawables.Count; i++)
            {
                var f = (drawables[i] as Fragment);
                if (f.wasTouched)
                {
                    float a = f.Distance(mousePosition);
                    if (a < nearest)
                    {
                        nearest = a;
                        index = i;
                    }
                }
            }
            return drawables[index] as Fragment;
        }

        internal Fragment SelectFragmentOnClick(SKPoint mousePosition)
        {
            List<Fragment> fragments = new();
            foreach(Fragment drawable in drawables.Skip(1)) {
                if (FSMath.IsPointInShape(mousePosition, drawable.VisiblePointsS))
                    fragments.Add(drawable);
            }
            float nearest = float.MaxValue;
            Fragment ret = null;
            foreach (var clickedFr in fragments)
            {
                if(clickedFr.wasTouched) {
                    if (clickedFr.Distance(mousePosition) < nearest)
                    {
                        ret = clickedFr;
                    }
                }
            }
            if(ret is not null)
            {
                return ret;
            }
            return null;
            //return getNearestFragment(mousePosition);
        }

        protected override void DrawMainShape(SKCanvas canvas)
        {
            throw new NotImplementedException();
        }
    }
}
