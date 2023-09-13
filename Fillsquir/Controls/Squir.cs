using Fillsquir.Controls;
using Fillsquir.Interfaces;
using SkiaSharp;

public class Squir : GeometryElement 
{
    GameSettings gameSettings;
    public SKPoint[] PointsP;
    public List<SKPoint[]> shapes;
    float Xoffset => (canvasWidth - ((prop1 / prop2) * canvasWidth)) / 2;

    //private SKPoint[] visiblePoints;
    public SKPoint[] VisiblePoints
    {
        get
        {
            var pts = new SKPoint[PointsP.Length];
            for (int i = 0; i < PointsP.Length; i++)
            {
                pts[i] = new((PointsP[i].X * scaleX) + Xoffset, PointsP[i].Y * scaleY);
            }
            return pts;
        }
    }
  
    internal Squir(float Width, float Height, GameSettings settings)
	{
        gameSettings = settings;
        //Points = new SKPoint[]{
        //    new SKPoint(10, 100),
        //    new SKPoint(800, 100),
        //    new SKPoint(1000, 700),
        //    new SKPoint(750, 850),
        //    new SKPoint(350, 1000),
        //    new SKPoint(500, 750),
        //    new SKPoint(750, 400),
        //    new SKPoint(0, 590),
        //};

        var centre = new SKPoint(500, 500);
        
        
        shapes = new();
        {
            PointsP = SquirGenerator.GenerateCompletelyRandomShape(80, 10000, 10000, gameSettings.rand);
            FSMath.ScaleShape(ref PointsP, canvasHeight, canvasWidth);

            foreach (var shape in SquirGenerator.GenerateCompletelyRandomShapes(16, PointsP, gameSettings.rand))
            {
                shapes.Add(shape);
            }
        }
        //Midpoints = new SKPoint[]
        //{
        //    new SKPoint(850, 150),
        //    new SKPoint(700, 200),


        //};
        //Midpoints = SKPoints;

        Resize(Width, Height);

    }

    
    protected override void DrawMainShape(SKCanvas canvas)
    {
        //todo draw using path not lines
        
        SKPath path = new();
        SKPaint paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Aqua,
            StrokeWidth = 3f,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };
        var VP = VisiblePoints;
        path.AddPoly(VisiblePoints);
        
        canvas.DrawPath(path, paint);


        //PathF path = new PathF(Points[0]);
        //for (int i = 1; i < Points.Length; i++)
        //{
        //    path.MoveTo(Points[i]);
        //}
        // canvas.DrawPath(path);

    }

    public List<SKPoint[]> SplitSquir()//TODO MAKE IT RANDOM
    {
        return shapes;
            
            /*new List<SKPoint[]>
        {
            new SKPoint[]
            {
                Points[0], Points[1], Midpoints[0]
            },
            new SKPoint[]
            {
                Points[1], Points[2],Points[6], Midpoints[0]
            },
            new SKPoint[]
            {
                Points[2], Points[5] ,Points[6],
            },
            new SKPoint[]
            {
                Points[2], Points[3], Points[5],
            },
             new SKPoint[]
            {
                Points[3], Midpoints[0], Midpoints[1],
            },
             new SKPoint[]
            {
                Points[0],Midpoints[0], Midpoints[1], Points[6],
            },new SKPoint[]
            {
                Points[6], Points[1], Points[7],
            }
        };*/


    }
}
