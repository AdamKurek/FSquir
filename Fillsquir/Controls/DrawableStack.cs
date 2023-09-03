#define nDebugClicking
#define nDebugClickingLines

using Fillsquir.Interfaces;
using Microsoft.Maui.Graphics;
using SkiaSharp;

namespace Fillsquir.Controls
{
    public class DrawableStack : SKDrawable
    {

        private float screenWidth = 1000;
        private float screenHeight = 1000;
        public List<GeometryElement> drawables = new();
        public GeometryElement cover;
        public GeometryElement Gui { get; set; }
        public HashSet<SKPoint> allActivePoints(int ignoreIndex)//todo make  update on move
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

        public GeometryElement this[int i]
        {
            get { return drawables[i]; }
            set { drawables[i] = value; }
        }
        public void AddDrawable(GeometryElement drawable)
        {
            drawables.Add(drawable);
            drawable.Resize(screenWidth, screenHeight);
            //(drawable as GeometryElement).Resize()
        }
        public void AddCover(GeometryElement drawable)
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
        public SKCanvas Draw(SKCanvas canvas)
        {
            foreach (var drawable in drawables.Skip(1))
            {
                drawable.Draw(canvas);
            }
            this[0].Draw(canvas);

            cover?.Draw(canvas);
            Gui?.Draw(canvas);
#if DebugClicking
            foreach (var circle in clickPoints)
            {
                var pt = new SKPaint();
                pt.Color = SKColors.Green;
                if (!circle.inBounds)
                    pt.Color = SKColors.Red;
                canvas.DrawCircle(circle.point.X, circle.point.Y, 1,pt);

            }
#endif

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

        public void Resize(float width, float height)
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
            for(int i = 1; i < drawables.Count; i++)
            {
                float a = (drawables[i] as Fragment).Distance(mousePosition);
                if ( a < nearest)
                {
                    nearest = a;
                    index = i;
                }
            }
            return drawables[index] as Fragment;
        }

        public static double CalculateDistance(SKPoint point1, SKPoint point2)
        {
            float dx = point2.X - point1.X;
            float dy = point2.Y - point1.Y;

            return Math.Sqrt(dx * dx + dy * dy);
        }


    }
}
