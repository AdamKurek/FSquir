using Fillsquir.Controls;
using Fillsquir.Interfaces;
using Fillsquir.Visuals;
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

    internal Squir(float Width, float Height, GameSettings settings)
        : base(settings)
    {
        shapes = new();
        PointsP = SquirGenerator.GenerateMainShape(
            gameSettings.WallDirectionsUndirected,
            gameSettings.rand,
            gameSettings.Level);
        FSMath.FitShapeUniform(ref PointsP, defaultCanvasWidth, defaultCanvasHeight, padding: 40f);

        float squirArea = FSMath.CalculateArea(PointsP);
        var (minDivisor, maxDivisor) = SquirGenerator.FragmentAreaDivisorsForLevel(gameSettings.Level);
        float minArea = squirArea / minDivisor;
        float maxArea = squirArea / maxDivisor;

        foreach (SKPoint[] shape in SquirGenerator.GenerateFragments(
            gameSettings.fragments,
            gameSettings.WallDirectionsDirected,
            gameSettings.Level,
            minArea,
            maxArea,
            gameSettings.rand))
        {
            shapes.Add(shape);
        }

        Resize(Width, Height);
    }

    protected override void DrawMainShape(SKCanvas canvas)
    {
        using SKPath path = new();
        path.AddPoly(VisiblePoints);

        SKRect boardRect = new(
            gameSettings.xoffset,
            gameSettings.yoffset,
            gameSettings.xoffset + (defaultCanvasWidth * scaleX),
            gameSettings.yoffset + (defaultCanvasHeight * scaleY));

        VisualSettings visualSettings = CurrentVisualSettings.Normalize();
        SkinDefinition skin = SkinCatalog.Resolve(visualSettings.SelectedSkinId);

        using SKShader boardShader = PuzzleMaterialService.GetBoardShader(CurrentPuzzleKey, visualSettings, boardRect);
        using SKPaint fillPaint = new()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            Shader = boardShader,
            Color = SKColors.White
        };

        canvas.DrawPath(path, fillPaint);

        DrawBoardInsetShadow(canvas, path, skin, visualSettings);
        DrawBoardLightSweep(canvas, path, skin, visualSettings);

        SKPaint outlinePaint = PuzzleMaterialService.GetOutlinePaint(visualSettings);
        canvas.DrawPath(path, outlinePaint);
    }

    private static void DrawBoardInsetShadow(SKCanvas canvas, SKPath path, SkinDefinition skin, VisualSettings settings)
    {
        byte alpha = (byte)Math.Clamp((int)MathF.Round(92f * settings.DepthIntensity), 26, 110);

        using SKPaint insetPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeWidth = 4.2f + (3.2f * settings.DepthIntensity),
            BlendMode = SKBlendMode.Multiply,
            Color = skin.ShadowColor.WithAlpha(alpha),
            StrokeJoin = SKStrokeJoin.Round
        };

        canvas.DrawPath(path, insetPaint);
    }

    private static void DrawBoardLightSweep(SKCanvas canvas, SKPath path, SkinDefinition skin, VisualSettings settings)
    {
        SKRect bounds = path.Bounds;
        byte alpha = (byte)Math.Clamp((int)MathF.Round(98f * settings.DepthIntensity), 22, 130);

        using SKShader lightShader = SKShader.CreateLinearGradient(
            new SKPoint(bounds.Left, bounds.Top),
            new SKPoint(bounds.Right, bounds.Bottom),
            new[]
            {
                skin.KeyLightColor.WithAlpha(alpha),
                SKColors.Transparent
            },
            new[] { 0f, 0.92f },
            SKShaderTileMode.Clamp);

        using SKPaint lightPaint = new()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            BlendMode = SKBlendMode.SoftLight,
            Shader = lightShader
        };

        canvas.DrawPath(path, lightPaint);
    }

    public List<SKPoint[]> SplitSquir()
    {
        return shapes;
    }
}
