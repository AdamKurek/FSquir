using Fillsquir.Controls;
using Fillsquir.Interfaces;
using Fillsquir.Visuals;
using SkiaSharp;

internal class Fragment : GeometryElement
{
    private const float GlintDriftCycleSeconds = 9.5f;
    public SKPoint[] PointsP;
    private readonly SKPoint[] touchedScreenPointsCache;
    private readonly SKPoint[] untouchedScreenPointsCache;
    private readonly SKPoint[] visibleWorldPointsCache;
    private bool hasTouchedScreenGeometryCache;
    private bool hasUntouchedScreenGeometryCache;
    private bool hasVisibleWorldPointsCache;
    private SKPoint cachedTouchedScreenPositionS;
    private float cachedTouchedScreenScaleX;
    private float cachedTouchedScreenScaleY;
    private float cachedTouchedScreenGameXOffset;
    private float cachedTouchedScreenGameYOffset;
    private SKRect cachedTouchedScreenBounds;
    private SKPoint cachedUntouchedScreenPositionS;
    private float cachedUntouchedScreenScaleX;
    private float cachedUntouchedScreenScaleY;
    private float cachedUntouchedScreenBottomStripMove;
    private float cachedUntouchedScreenFitScale;
    private SKRect cachedUntouchedScreenBounds;
    private SKPoint cachedVisibleWorldPositionS;
    private float cachedVisibleWorldScaleX;
    private float cachedVisibleWorldScaleY;

    private SKPoint[] UntouchedPointsS
    {
        get => GetUntouchedScreenPoints();
    }

    public SKPoint PositionS;
    public SKPoint PositionP
    {
        get
        {
            float safeScaleX = scaleX == 0f ? 1f : scaleX;
            float safeScaleY = scaleY == 0f ? 1f : scaleY;
            SKPoint ret = new();
            ret.X = PositionS.X / safeScaleX;
            ret.Y = PositionS.Y / safeScaleY;
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

    public SKPoint Centroid => FSMath.Centroid(GetVisibleScreenPoints());

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
        get => GetVisibleScreenPoints();
    }

    public SKPoint[] VisiblePointsP
    {
        get
        {
            EnsureVisibleWorldPoints();
            return visibleWorldPointsCache;
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
        touchedScreenPointsCache = new SKPoint[PointsP.Length];
        untouchedScreenPointsCache = new SKPoint[PointsP.Length];
        visibleWorldPointsCache = new SKPoint[PointsP.Length];
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

        SKPoint[] points = GetVisibleScreenPoints();
        SKRect bounds = GetVisibleScreenBounds();
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
            DrawPieceShadow(canvas, path, bounds, visualSettings, isDragging, elevationMultiplier);
        }

        SKPaint fillPaint = PuzzleMaterialService.GetPieceFillPaint(
            CurrentPuzzleKey,
            visualSettings,
            boardRect,
            bounds,
            forcePieceLocal: !wasTouched);

        canvas.DrawPath(path, fillPaint);

        if (wasTouched && qualityEffects.UseAmbientOcclusion)
        {
            DrawAmbientOcclusion(canvas, path, visualSettings);
        }

        if (wasTouched && qualityEffects.UseBevel)
        {
            SKPaint darkBevel = PuzzleMaterialService.GetPieceBevelPaint(visualSettings, bounds, darkPass: true);
            SKPaint lightBevel = PuzzleMaterialService.GetPieceBevelPaint(visualSettings, bounds, darkPass: false);
            canvas.DrawPath(path, darkBevel);
            canvas.DrawPath(path, lightBevel);
        }

        if (wasTouched && qualityEffects.UseRimHighlight)
        {
            DrawRimHighlight(canvas, path, visualSettings);
        }

        if (wasTouched && qualityEffects.UseGlintOverlay)
        {
            DrawGlintOverlay(canvas, path, bounds, visualSettings);
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
        path.AddPoly(GetVisibleScreenPoints());

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

    private void DrawPieceShadow(SKCanvas canvas, SKPath path, SKRect bounds, VisualSettings settings, bool isDragging, float elevationMultiplier)
    {
        float sizeFactor = Math.Clamp(Math.Max(bounds.Width, bounds.Height) / 220f, 0.65f, 1.45f);
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

    private void DrawGlintOverlay(SKCanvas canvas, SKPath path, SKRect bounds, VisualSettings settings)
    {
        if (bounds.Width < 1f || bounds.Height < 1f)
        {
            return;
        }

        SkinDefinition skin = SkinCatalog.Resolve(settings.SelectedSkinId);
        float driftPhase = SmoothCycle01(gameSettings.RenderTimeSeconds / GlintDriftCycleSeconds);

        float pointerBias = 0f;
        if (gameSettings.HasGlintPointer)
        {
            float normalizedPointerX =
                (gameSettings.GlintPointerPosition.X - bounds.Left) / Math.Max(bounds.Width, 1f);
            pointerBias = (Math.Clamp(normalizedPointerX, 0f, 1f) - 0.5f) * 0.9f;
        }

        float phase = gameSettings.GlintMotionMode switch
        {
            GlintMotionMode.AlwaysDrift => driftPhase,
            GlintMotionMode.MouseDriven => Math.Clamp(0.5f + (pointerBias * 0.82f), 0f, 1f),
            _ => Math.Clamp(driftPhase + (pointerBias * 0.28f), 0f, 1f)
        };

        float startX = bounds.Left - (bounds.Width * 0.35f) + (phase * bounds.Width * 1.55f);
        float endX = startX + (bounds.Width * 0.28f);
        float glintAlpha = 68f + (settings.DepthIntensity * 8f) + (skin.AccentIntensity * 24f);

        using SKShader shader = SKShader.CreateLinearGradient(
            new SKPoint(startX, bounds.Top),
            new SKPoint(endX, bounds.Bottom),
            new[]
            {
                SKColors.Transparent,
                new SKColor(255, 255, 255, (byte)Math.Clamp((int)MathF.Round(glintAlpha), 20, 112)),
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

    private static float SmoothCycle01(float value)
    {
        // Continuous oscillation in [0, 1] with no hard reset/jump at cycle boundaries.
        return 0.5f + (0.5f * MathF.Sin((value * (2f * MathF.PI)) - (MathF.PI * 0.5f)));
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

    private SKPoint[] GetVisibleScreenPoints()
    {
        if (wasTouched)
        {
            EnsureTouchedScreenGeometry();
            return touchedScreenPointsCache;
        }

        EnsureUntouchedScreenGeometry();
        return untouchedScreenPointsCache;
    }

    private SKPoint[] GetUntouchedScreenPoints()
    {
        EnsureUntouchedScreenGeometry();
        return untouchedScreenPointsCache;
    }

    private SKRect GetVisibleScreenBounds()
    {
        if (wasTouched)
        {
            EnsureTouchedScreenGeometry();
            return cachedTouchedScreenBounds;
        }

        EnsureUntouchedScreenGeometry();
        return cachedUntouchedScreenBounds;
    }

    private void EnsureTouchedScreenGeometry()
    {
        float currentScaleX = scaleX;
        float currentScaleY = scaleY;
        float currentGameXOffset = gameSettings.xoffset;
        float currentGameYOffset = gameSettings.yoffset;

        if (hasTouchedScreenGeometryCache
            && cachedTouchedScreenPositionS.X == PositionS.X
            && cachedTouchedScreenPositionS.Y == PositionS.Y
            && cachedTouchedScreenScaleX == currentScaleX
            && cachedTouchedScreenScaleY == currentScaleY
            && cachedTouchedScreenGameXOffset == currentGameXOffset
            && cachedTouchedScreenGameYOffset == currentGameYOffset)
        {
            return;
        }

        float xoffset = PositionS.X - (MoveToFillXP * currentScaleX);
        float yoffset = PositionS.Y - (MoveToFillYP * currentScaleY);
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        for (int i = 0; i < PointsP.Length; i++)
        {
            SKPoint point = new(
                (PointsP[i].X * currentScaleX) + xoffset + currentGameXOffset,
                (PointsP[i].Y * currentScaleY) + yoffset + currentGameYOffset);
            touchedScreenPointsCache[i] = point;
            UpdateBounds(point, ref minX, ref minY, ref maxX, ref maxY);
        }

        cachedTouchedScreenPositionS = PositionS;
        cachedTouchedScreenScaleX = currentScaleX;
        cachedTouchedScreenScaleY = currentScaleY;
        cachedTouchedScreenGameXOffset = currentGameXOffset;
        cachedTouchedScreenGameYOffset = currentGameYOffset;
        cachedTouchedScreenBounds = CreateBounds(minX, minY, maxX, maxY);
        hasTouchedScreenGeometryCache = true;
    }

    private void EnsureUntouchedScreenGeometry()
    {
        float currentScaleX = scaleX;
        float currentScaleY = scaleY;
        float currentBottomStripMove = gameSettings.bottomStripMove;
        float untouchedFitScale = GetUntouchedFitScale();

        if (hasUntouchedScreenGeometryCache
            && cachedUntouchedScreenPositionS.X == PositionS.X
            && cachedUntouchedScreenPositionS.Y == PositionS.Y
            && cachedUntouchedScreenScaleX == currentScaleX
            && cachedUntouchedScreenScaleY == currentScaleY
            && cachedUntouchedScreenBottomStripMove == currentBottomStripMove
            && cachedUntouchedScreenFitScale == untouchedFitScale)
        {
            return;
        }

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        for (int i = 0; i < PointsP.Length; i++)
        {
            float centeredX = (PointsP[i].X - MoveToFillXP) - (0.5f * sizeP.X);
            float centeredY = (PointsP[i].Y - MoveToFillYP) - (0.5f * sizeP.Y);
            SKPoint point = new(
                (centeredX * currentScaleX * untouchedFitScale) + PositionS.X - currentBottomStripMove,
                (centeredY * currentScaleY * untouchedFitScale) + PositionS.Y);
            untouchedScreenPointsCache[i] = point;
            UpdateBounds(point, ref minX, ref minY, ref maxX, ref maxY);
        }

        cachedUntouchedScreenPositionS = PositionS;
        cachedUntouchedScreenScaleX = currentScaleX;
        cachedUntouchedScreenScaleY = currentScaleY;
        cachedUntouchedScreenBottomStripMove = currentBottomStripMove;
        cachedUntouchedScreenFitScale = untouchedFitScale;
        cachedUntouchedScreenBounds = CreateBounds(minX, minY, maxX, maxY);
        hasUntouchedScreenGeometryCache = true;
    }

    private void EnsureVisibleWorldPoints()
    {
        float currentScaleX = scaleX;
        float currentScaleY = scaleY;

        if (hasVisibleWorldPointsCache
            && cachedVisibleWorldPositionS.X == PositionS.X
            && cachedVisibleWorldPositionS.Y == PositionS.Y
            && cachedVisibleWorldScaleX == currentScaleX
            && cachedVisibleWorldScaleY == currentScaleY)
        {
            return;
        }

        float safeScaleX = currentScaleX == 0f ? 1f : currentScaleX;
        float safeScaleY = currentScaleY == 0f ? 1f : currentScaleY;
        float positionWorldX = PositionS.X / safeScaleX;
        float positionWorldY = PositionS.Y / safeScaleY;

        for (int i = 0; i < PointsP.Length; i++)
        {
            visibleWorldPointsCache[i] = new SKPoint(
                PointsP[i].X + positionWorldX - MoveToFillXP,
                PointsP[i].Y + positionWorldY - MoveToFillYP);
        }

        cachedVisibleWorldPositionS = PositionS;
        cachedVisibleWorldScaleX = currentScaleX;
        cachedVisibleWorldScaleY = currentScaleY;
        hasVisibleWorldPointsCache = true;
    }

    private static void UpdateBounds(SKPoint point, ref float minX, ref float minY, ref float maxX, ref float maxY)
    {
        if (point.X < minX) minX = point.X;
        if (point.Y < minY) minY = point.Y;
        if (point.X > maxX) maxX = point.X;
        if (point.Y > maxY) maxY = point.Y;
    }

    private static SKRect CreateBounds(float minX, float minY, float maxX, float maxY)
    {
        if (minX == float.MaxValue || minY == float.MaxValue || maxX == float.MinValue || maxY == float.MinValue)
        {
            return SKRect.Empty;
        }

        return new SKRect(minX, minY, maxX, maxY);
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

    private float GetUntouchedFitScale()
    {
        float visibleCols = Math.Max(1, gameSettings.VisibleRows);
        float stripRows = Math.Max(1, gameSettings.Rows);
        float cellWidth = canvasWidth / visibleCols;
        float stripTop = canvasHeight * (gameSettings.prop1 / gameSettings.prop2);
        float stripHeight = Math.Max(1f, canvasHeight - stripTop);
        float cellHeight = stripHeight / stripRows;

        float targetWidth = Math.Max(1f, cellWidth * 0.78f);
        float targetHeight = Math.Max(1f, cellHeight * 0.72f);
        float baseWidth = Math.Max(1f, sizeP.X * scaleX);
        float baseHeight = Math.Max(1f, sizeP.Y * scaleY);

        float fitScale = MathF.Min(targetWidth / baseWidth, targetHeight / baseHeight);
        return Math.Clamp(fitScale, 0.03f, 1f);
    }

    public float scaleToMiddleX(float from)
    {
        return from * canvasWidth / defaultCanvasWidth;
    }

    public float scaleToMiddleY(float from)
    {
        return from * canvasHeight / defaultCanvasHeight;
    }
}
