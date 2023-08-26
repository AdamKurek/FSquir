#define nDebugClicking

using Fillsquir.Interfaces;
using Microsoft.Maui.Graphics;

namespace Fillsquir.Controls
{
    public class DrawableStack : IDrawable
    {

        private float screenWidth = 1000;
        private float screenHeight = 1000;
        public List<GeometryElement> drawables = new();
        public HashSet<PointF> allActivePoints(int ignoreIndex)//todo make  update on move
        {
            var set = new HashSet<PointF>();
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
#if DebugClicking
        public struct Drawpoint
        {
            public PointF point;
            public bool inBounds;
        }
        public List<Drawpoint> clickPoints = new List<Drawpoint> { };
        public void AddDot(Point point, bool inBounds)
        {
            Drawpoint drawpoint = new Drawpoint();
            drawpoint.point.X = (float)point.X;
            drawpoint.point.Y = (float)point.Y;
            drawpoint.inBounds = inBounds;
            clickPoints.Add(drawpoint);
        }
#endif
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            foreach (var drawable in drawables.Skip(1))
            {
                drawable.Draw(canvas, dirtyRect);
            }
            this[0].Draw(canvas, dirtyRect);

#if DebugClicking
            foreach (var circle in clickPoints)
            {
                if(!circle.inBounds)
                    canvas.StrokeColor = Colors.IndianRed;

                canvas.DrawCircle(circle.point.X, circle.point.Y, 10);
            }
#endif
        }

        public void Resize(float width, float height)
        {
            screenWidth = width;
            screenHeight = height;
            foreach(var drawable in drawables)
            {
                drawable.Resize(width, height);
            }
        }

        internal Fragment getNearestFragment(Point mousePosition)
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

        public static double CalculateDistance(PointF point1, PointF point2)
        {
            float dx = point2.X - point1.X;
            float dy = point2.Y - point1.Y;

            return Math.Sqrt(dx * dx + dy * dy);
        }


    }
}
