using Fillsquir.Controls;
using Fillsquir.Interfaces;
using Fillsquir.Visuals;
using SkiaSharp;

internal class Fragment : GeometryElement
{
    public SKPoint[] PointsP;

    private SKPoint[] UntouchedPointsS
    {
        get
        {
            var up = new SKPoint[PointsP.Length];
            for (int i = 0; i < PointsP.Length; i++)
            {
                up[i].X = scaleToMiddleX((PointsP[i].X - MoveToFillXP) - (0.5f * sizeP.X)) * scaleX / 8f + PositionS.X - gameSettings.bottomStripMove;
                up[i].Y = scaleToMiddleY((PointsP[i].Y - MoveToFillYP) - (0.5f * sizeP.Y)) * scaleY / 8f + PositionS.Y;
            }

            return up;
        }
    }

    public SKPoint PositionS;
    public SKPoint PositionP
    {
        get
        {
            SKPoint ret = new();
            ret.X = PositionS.X / scaleX;
            ret.Y = PositionS.Y / scaleY;
            return ret;
        }
    }

    public SKPoint sizeP;
    private float MoveToFillXP;
    private float MoveToFillYP;
    private DateTimeOffset releaseBoostUntilUtc;

    public SKPoint MidpointS
    {
        get
        {
            SKPoint midpoint = new();
            if (wasTouched)
            {
                midpoint.X = PositionS.X + (sizeP.X / 2 * scaleX) + gameSettings.xoffset;
                midpoint.Y = PositionS.Y + (sizeP.Y / 2 * scaleY) + gameSettings.yoffset;
                return midpoint;
            }

            midpoint.X = PositionS.X;
            midpoint.Y = PositionS.Y;

            return midpoint;
        }
    }

    public SKPoint Centroid => FSMath.Centroid(VisiblePointsS);

#if DebugVisuals
    public float RadiusS
    {
        get
        {
            return Math.Max(sizeP.X * scaleX, sizeP.Y * scaleY) / 2;
        }
    }
#endif

    public bool wasTouched = false;

    protected override void ResizePrecize(float Width, float Height)
    {
        PositionS.X = PositionS.X * (Width / canvasWidth);
        PositionS.Y = PositionS.Y * (Height / canvasHeight);
    }

    public int IndexX;
    public int IndexY;

    private float Xoffset => PositionS.X - (MoveToFillXP * scaleX);
    private float Yoffset => PositionS.Y - (MoveToFillYP * scaleY);

    public SKPoint[] VisiblePointsS
    {
        get
        {
            if (!wasTouched)
            {
                return UntouchedPointsS;
            }

            var pts = new SKPoint[PointsP.Length];
            for (int i = 0; i < PointsP.Length; i++)
            {
                pts[i] = new SKPoint((PointsP[i].X * scaleX) + Xoffset + gameSettings.xoffset, (PointsP[i].Y * scaleY) + Yoffset + gameSettings.yoffset);
            }

            return pts;
        }
    }

    public SKPoint[] VisiblePointsP
    {
        get
        {
            var pts = new SKPoint[PointsP.Length];
            for (int i = 0; i < PointsP.Length; i++)
            {
                pts[i] = new SKPoint(PointsP[i].X + PositionP.X - MoveToFillXP, PointsP[i].Y + PositionP.Y - MoveToFillYP);
            }

            return pts;
        }
    }

    internal Fragment(SKPoint[] Points, int indexX, int indexY, GameSettings settings)
        : base(settings)
    {
        float xMin = float.MaxValue;
        float yMin = float.MaxValue;
        float xMax = 0;
        float yMax = 0;
        PointsP = Points;

        foreach (SKPoint point in PointsP)
        {
            if (point.X < xMin)
            {
                xMin = point.X;
            }

            if (point.X > xMax)
            {
                xMax = point.X;
            }

            if (point.Y < yMin)
            {
                yMin = point.Y;
            }

            if (point.Y > yMax)
            {
                yMax = point.Y;
            }
        }

        sizeP = new SKPoint((xMax - xMin), (yMax - yMin));
        MoveToFillXP = xMin;
        MoveToFillYP = yMin;
        IndexX = indexX;
        IndexY = indexY;
    }

    internal void TriggerReleaseSettle()
    {
        releaseBoostUntilUtc = DateTimeOffset.UtcNow.AddMilliseconds(170);
    }

    protected override void DrawMainShape(SKCanvas canvas)
    {
        if (!wasTouched)
        {
            var cellWidth = canvasWidth / gameSettings.VisibleRows;
            PositionS.X = (cellWidth * IndexX) + (cellWidth / 2f);
            var sqHeight = canvasHeight * (gameSettings.prop1 / gameSettings.prop2);
            var movePerCell = (canvasHeight - sqHeight) / gameSettings.Rows;
            var rowOffset = 0.5f * movePerCell;
            PositionS.Y = sqHeight + (IndexY * movePerCell) + rowOffset;
        }

        SKPoint[] points = wasTouched ? VisiblePointsS : UntouchedPointsS;
        using SKPath path = new();
        path.AddPoly(points);

        SKRect boardRect = new(
            gameSettings.xoffset,
            gameSettings.yoffset,
            gameSettings.xoffset + (defaultCanvasWidth * scaleX),
            gameSettings.yoffset + (defaultCanvasHeight * scaleY));

        VisualSettings visualSettings = CurrentVisualSettings.Normalize();
        MaterialEffectFlags qualityEffects = PuzzleMaterialService.GetQualityEffects(visualSettings.QualityTier);

        bool isDragging = wasTouched && ReferenceEquals(gameSettings.ActiveDraggedFragment, this);
        bool isHoverTarget = ReferenceEquals(gameSettings.HoveredFragment, this);
        float settleBoost = GetReleaseSettleBoost();
        float elevationMultiplier = 1f + (isDragging ? 0.85f : 0f) + (settleBoost * 0.45f);

        if (wasTouched && qualityEffects.UseShadow)
        {
            DrawPieceShadow(canvas, path, visualSettings, isDragging, elevationMultiplier);
        }

        SKPaint fillPaint = PuzzleMaterialService.GetPieceFillPaint(
            CurrentPuzzleKey,
            visualSettings,
            boardRect,
            path.Bounds,
            forcePieceLocal: !wasTouched);

        canvas.DrawPath(path, fillPaint);

        if (wasTouched && qualityEffects.UseAmbientOcclusion)
        {
            DrawAmbientOcclusion(canvas, path, visualSettings);
        }

        if (wasTouched && qualityEffects.UseBevel)
        {
            SKPaint darkBevel = PuzzleMaterialService.GetPieceBevelPaint(visualSettings, path.Bounds, darkPass: true);
            SKPaint lightBevel = PuzzleMaterialService.GetPieceBevelPaint(visualSettings, path.Bounds, darkPass: false);
            canvas.DrawPath(path, darkBevel);
            canvas.DrawPath(path, lightBevel);
        }

        if (wasTouched && qualityEffects.UseRimHighlight)
        {
            DrawRimHighlight(canvas, path, visualSettings);
        }

        if (wasTouched && qualityEffects.UseGlintOverlay)
        {
            DrawGlintOverlay(canvas, path, visualSettings);
        }

        SKPaint outlinePaint = PuzzleMaterialService.GetOutlinePaint(visualSettings);
        canvas.DrawPath(path, outlinePaint);

        if (isHoverTarget && !isDragging)
        {
            DrawHoverCue(canvas, path, visualSettings, wasTouched);
        }
    }

    public void DrawVertices(SKCanvas canvas)
    {
        using SKPath path = new();

        if (!wasTouched)
        {
            path.AddPoly(UntouchedPointsS);
        }
        else
        {
            path.AddPoly(VisiblePointsS);
        }

        SKPaint outlinePaint = PuzzleMaterialService.GetOutlinePaint(CurrentVisualSettings.Normalize());
        canvas.DrawPath(path, outlinePaint);

#if DebugVisuals
        SKPaint sKPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            IsAntialias = true,
            Color = SKColors.BlueViolet
        };
        canvas.DrawCircle(Centroid.X, Centroid.Y, 3, sKPaint);
        sKPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            IsAntialias = true,
            Color = SKColors.BlueViolet
        };
        foreach (SKPoint pt in VisiblePointsS)
        {
            canvas.DrawCircle(pt.X, pt.Y, 5, sKPaint);
        }
        sKPaint.Color = SKColors.BurlyWood;
        canvas.DrawCircle(MidpointS.X, MidpointS.Y, RadiusS, sKPaint);
        sKPaint.Color = SKColors.IndianRed;
        canvas.DrawCircle(Centroid.X, Centroid.Y, RadiusS, sKPaint);
        sKPaint.Color = SKColors.DarkViolet;
        canvas.DrawRect(PositionS.X + gameSettings.xoffset, PositionS.Y + gameSettings.yoffset, sizeP.X * scaleX, sizeP.Y * scaleY, sKPaint);
#endif
    }

    private void DrawPieceShadow(SKCanvas canvas, SKPath path, VisualSettings settings, bool isDragging, float elevationMultiplier)
    {
        float sizeFactor = Math.Clamp(Math.Max(path.Bounds.Width, path.Bounds.Height) / 220f, 0.65f, 1.45f);
        float shadowX = (2.1f + (sizeFactor * 1.4f)) * settings.DepthIntensity * elevationMultiplier;
        float shadowY = (2.9f + (sizeFactor * 1.6f)) * settings.DepthIntensity * elevationMultiplier;

        SKPaint shadowPaint = PuzzleMaterialService.GetPieceShadowPaint(settings, isDragging, elevationMultiplier);
        canvas.Save();
        canvas.Translate(shadowX, shadowY);
        canvas.DrawPath(path, shadowPaint);
        canvas.Restore();
    }

    private static void DrawAmbientOcclusion(SKCanvas canvas, SKPath path, VisualSettings settings)
    {
        SkinDefinition skin = SkinCatalog.Resolve(settings.SelectedSkinId);

        using SKPaint aoPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeWidth = 0.95f + (1.55f * settings.DepthIntensity),
            BlendMode = SKBlendMode.Multiply,
            Color = skin.ShadowColor.WithAlpha((byte)Math.Clamp((int)MathF.Round(52f * settings.DepthIntensity), 18, 82))
        };

        canvas.DrawPath(path, aoPaint);
    }

    private static void DrawRimHighlight(SKCanvas canvas, SKPath path, VisualSettings settings)
    {
        SkinDefinition skin = SkinCatalog.Resolve(settings.SelectedSkinId);

        using SKPaint rimPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeWidth = 0.8f + (1.1f * settings.DepthIntensity),
            BlendMode = SKBlendMode.Screen,
            Color = skin.KeyLightColor.WithAlpha((byte)Math.Clamp((int)MathF.Round(70f * settings.DepthIntensity), 24, 94))
        };

        canvas.DrawPath(path, rimPaint);
    }

    private static void DrawGlintOverlay(SKCanvas canvas, SKPath path, VisualSettings settings)
    {
        SKRect bounds = path.Bounds;
        if (bounds.Width < 1f || bounds.Height < 1f)
        {
            return;
        }

        double cycle = 1900d;
        double now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        float phase = (float)((now % cycle) / cycle);

        float startX = bounds.Left - (bounds.Width * 0.35f) + (phase * bounds.Width * 1.55f);
        float endX = startX + (bounds.Width * 0.28f);

        using SKShader shader = SKShader.CreateLinearGradient(
            new SKPoint(startX, bounds.Top),
            new SKPoint(endX, bounds.Bottom),
            new[]
            {
                SKColors.Transparent,
                new SKColor(255, 255, 255, (byte)Math.Clamp((int)MathF.Round(72f * settings.DepthIntensity), 20, 96)),
                SKColors.Transparent
            },
            new[] { 0f, 0.52f, 1f },
            SKShaderTileMode.Clamp);

        using SKPaint glintPaint = new()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            BlendMode = SKBlendMode.Screen,
            Shader = shader
        };

        canvas.DrawPath(path, glintPaint);
    }

    private static void DrawHoverCue(SKCanvas canvas, SKPath path, VisualSettings settings, bool isTouched)
    {
        SkinDefinition skin = SkinCatalog.Resolve(settings.SelectedSkinId);

        byte fillAlpha = (byte)Math.Clamp((int)MathF.Round((isTouched ? 36f : 58f) * settings.DepthIntensity), 14, 92);
        byte strokeAlpha = (byte)Math.Clamp((int)MathF.Round((isTouched ? 92f : 126f) * settings.DepthIntensity), 30, 178);
        float strokeWidth = (isTouched ? 1.8f : 2.5f) + (0.9f * settings.DepthIntensity);

        using SKPaint glowFill = new()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            BlendMode = SKBlendMode.Screen,
            Color = skin.FillLightColor.WithAlpha(fillAlpha)
        };

        using SKPaint glowStroke = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeWidth = strokeWidth,
            StrokeJoin = SKStrokeJoin.Round,
            StrokeCap = SKStrokeCap.Round,
            BlendMode = SKBlendMode.Screen,
            Color = skin.KeyLightColor.WithAlpha(strokeAlpha)
        };

        canvas.DrawPath(path, glowFill);
        canvas.DrawPath(path, glowStroke);
    }

    private float GetReleaseSettleBoost()
    {
        if (releaseBoostUntilUtc <= DateTimeOffset.UtcNow)
        {
            return 0f;
        }

        TimeSpan remaining = releaseBoostUntilUtc - DateTimeOffset.UtcNow;
        const float settleDurationMs = 170f;
        return Math.Clamp((float)remaining.TotalMilliseconds / settleDurationMs, 0f, 1f);
    }

    public void SetPositionToPointLocation(SKPoint VisiblePointToAdjust, int finalIndex)
    {
        PositionS.X = VisiblePointToAdjust.X - (PointsP[finalIndex].X * scaleX) + (MoveToFillXP * scaleX) - gameSettings.xoffset;
        PositionS.Y = VisiblePointToAdjust.Y - (PointsP[finalIndex].Y * scaleY) + (MoveToFillYP * scaleY) - gameSettings.yoffset;
    }

    internal float Distance(SKPoint mousePosition)
    {
        return FSMath.CalculateDistance(mousePosition, Centroid);
    }

    public float scaleToMiddleX(float from)
    {
        return from * canvasWidth / defaultCanvasWidth;
    }

    public float scaleToMiddleY(float from)
    {
        return from * canvasWidth / defaultCanvasWidth;
    }
}
