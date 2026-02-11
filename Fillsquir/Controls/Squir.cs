using Fillsquir.Controls;
using Fillsquir.Interfaces;
using SkiaSharp;

internal class Squir : GeometryElement 
{
    public SKPoint[] PointsP;
    public List<SKPoint[]> shapes;
    public SKPoint[] VisiblePoints
    {
        get
        {
            var pts = new SKPoint[PointsP.Length];
            for (int i = 0; i < PointsP.Length; i++)
            {
                pts[i] = new((PointsP[i].X * scaleX) + gameSettings.xoffset, PointsP[i].Y * scaleY + gameSettings.yoffset);
            }
            return pts;
        }
    }
  
    internal Squir(float Width, float Height, GameSettings settings) : base(settings)
    {
        shapes = new();
        {
            PointsP = SquirGenerator.GenerateMainShape(gameSettings.WallDirectionsUndirected, gameSettings.rand);
            FSMath.FitShapeUniform(ref PointsP, defaultCanvasWidth, defaultCanvasHeight, padding: 40f);

            float squirArea = FSMath.CalculateArea(PointsP);
            float minArea = squirArea / 42f;
            float maxArea = squirArea / 7f;

            foreach (var shape in SquirGenerator.GenerateFragments(gameSettings.fragments, gameSettings.WallDirectionsDirected, gameSettings.Level, minArea, maxArea, gameSettings.rand))
            {
                shapes.Add(shape);
            }
        }
        Resize(Width, Height);
    }
    
    protected override void DrawMainShape(SKCanvas canvas)
    {
        SKPath path = new();
        SKPaint paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Aqua,
            StrokeWidth = 3f,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };
        path.AddPoly(VisiblePoints);
        canvas.DrawPath(path, paint);
    }

    public List<SKPoint[]> SplitSquir()
    {
        //return new List<SKPoint[]> { new SKPoint[] { new SKPoint(0f, 0f), new SKPoint(1000f, 0f), new SKPoint(1000f, 1000f), new SKPoint(0f, 1000f) }, };
        return shapes;
    }
}
