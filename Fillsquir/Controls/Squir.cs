using Fillsquir.Controls;
using Fillsquir.Interfaces;
using Microsoft.Maui.Graphics;

public class Squir : GeometryElement 
{

    public PointF[] PointsP;
    public List<PointF[]> shapes;
    float Xoffset => (canvasWidth - ((prop1 / prop2) * canvasWidth)) / 2;

    private PointF[] visiblePoints;
    public PointF[] VisiblePoints
    {
        get
        {
            var pts = new PointF[PointsP.Length];
            for (int i = 0; i < PointsP.Length; i++)
            {
                pts[i] = new((PointsP[i].X * scaleX) + Xoffset, PointsP[i].Y * scaleY);
            }
            return pts;
        }
    }
  
    public Squir(float Width, float Height)
	{
        //Points = new PointF[]{
        //    new PointF(10, 100),
        //    new PointF(800, 100),
        //    new PointF(1000, 700),
        //    new PointF(750, 850),
        //    new PointF(350, 1000),
        //    new PointF(500, 750),
        //    new PointF(750, 400),
        //    new PointF(0, 590),
        //};

        var centre = new PointF(500, 500);

        PointsP = SquirGenerator.GenerateOrderedMainShapeOnCircle(centre, 100, 500, 8);
        shapes = new();
        { 
            foreach (var shape in SquirGenerator.GenerateShapes(8, PointsP))
            {
                shapes.Add(shape);
            }
        }
        //Midpoints = new PointF[]
        //{
        //    new PointF(850, 150),
        //    new PointF(700, 200),


        //};
        //Midpoints = pointFs;

        Resize(Width, Height);

    }

    
    protected override void DrawMainShape(ICanvas canvas,RectF dirtyRect)
    {
        //todo draw using path not lines
        
        canvas.StrokeColor = Colors.Aqua;
        canvas.FillColor = Colors.Aqua;
        for (int i = 0; i < PointsP.Length - 1; i++)
        {
            canvas.DrawLine(VisiblePoints[i], VisiblePoints[i+1]);
        }
        canvas.DrawLine(VisiblePoints[0], VisiblePoints[PointsP.Count()-1]);

        //PathF path = new PathF(Points[0]);
        //for (int i = 1; i < Points.Length; i++)
        //{
        //    path.MoveTo(Points[i]);
        //}
        // canvas.DrawPath(path);

    }

    public List<PointF[]> SplitSquir()//TODO MAKE IT RANDOM
    {
        return shapes;
            
            /*new List<PointF[]>
        {
            new PointF[]
            {
                Points[0], Points[1], Midpoints[0]
            },
            new PointF[]
            {
                Points[1], Points[2],Points[6], Midpoints[0]
            },
            new PointF[]
            {
                Points[2], Points[5] ,Points[6],
            },
            new PointF[]
            {
                Points[2], Points[3], Points[5],
            },
             new PointF[]
            {
                Points[3], Midpoints[0], Midpoints[1],
            },
             new PointF[]
            {
                Points[0],Midpoints[0], Midpoints[1], Points[6],
            },new PointF[]
            {
                Points[6], Points[1], Points[7],
            }
        };*/


    }
}
