using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fillsquir.Interfaces;

namespace Fillsquir.Controls
{
    public class CommonArea : GeometryElement
    {
        private float big = 1000;
        private float small = 0;


        public List<PointF[]> FiguresP = new();
        float Xoffset => (canvasWidth - ((prop1 / prop2) * canvasWidth)) / 2;

        public List<PointF[]> VisibleFiguresS
        {
            get
            {
                var fgs = new List<PointF[]>();
                foreach (var f in FiguresP)
                {
                    var visibleFigure = new PointF[f.Count()];
                    for (int i = 0; i < f.Length; i++)
                    {
                        visibleFigure[i] = new PointF((f[i].X * scaleX) + Xoffset, f[i].Y * scaleY);
                    }
                    fgs.Add(visibleFigure);
                }
                return fgs;

            }
        }

        public CommonArea()
        {
        }


        protected override void DrawMainShape(ICanvas canvas, RectF dirtyRect)
        {
            //todo draw using path not lines

            canvas.StrokeColor = Colors.DarkOrange;
                canvas.FillColor = Colors.LightGoldenrodYellow;
            foreach(var shape in  VisibleFiguresS) { 
                for (int i = 0; i < shape.Length -1; i++){
                    canvas.DrawLine(shape[i], shape[i + 1]);
                }canvas.DrawLine(shape[0], shape[shape.Count() - 1]);
            }
        }

    }
}
