using Fillsquir.Domain;
using SkiaSharp;

namespace Fillsquir.Visuals;

public sealed class PuzzleMaterialService : IPuzzleMaterialService, IDisposable
{
    private readonly WorldTextureProvider worldTextureProvider;

    private readonly Dictionary<string, SKPaint> outlinePaintCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SKPaint> shadowPaintCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SKPaint> dividerPaintCache = new(StringComparer.Ordinal);

    private readonly SKPaint pieceFillPaint = new()
    {
        Style = SKPaintStyle.Fill,
        IsAntialias = true
    };

    private readonly SKPaint bevelLightPaint = new()
    {
        Style = SKPaintStyle.Stroke,
        IsAntialias = true,
        StrokeCap = SKStrokeCap.Round,
        StrokeJoin = SKStrokeJoin.Round,
        BlendMode = SKBlendMode.Screen
    };

    private readonly SKPaint bevelDarkPaint = new()
    {
        Style = SKPaintStyle.Stroke,
        IsAntialias = true,
        StrokeCap = SKStrokeCap.Round,
        StrokeJoin = SKStrokeJoin.Round,
        BlendMode = SKBlendMode.Multiply
    };

    private readonly SKPaint stripBackgroundPaint = new()
    {
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
        BlendMode = SKBlendMode.SrcOver
    };

    private SKShader? pieceShader;
    private SKShader? bevelLightShader;
    private SKShader? bevelDarkShader;
    private SKShader? stripBackgroundShader;

    public PuzzleMaterialService(WorldTextureProvider worldTextureProvider)
    {
        this.worldTextureProvider = worldTextureProvider;
    }

    public SKShader GetBoardShader(PuzzleKey puzzleKey, VisualSettings settings, SKRect boardRect)
    {
        SkinDefinition skin = SkinCatalog.Resolve(settings.SelectedSkinId);
        VisualSettings normalized = settings.Normalize();
        SKImage texture = worldTextureProvider.GetTexture(puzzleKey, skin, normalized.QualityTier);
        return BuildCompositeShader(texture, skin, normalized, boardRect, normalized.QualityTier, forBoard: true);
    }

    public SKPaint GetPieceFillPaint(PuzzleKey puzzleKey, VisualSettings settings, SKRect boardRect, SKRect pieceRect, bool forcePieceLocal)
    {
        SkinDefinition skin = SkinCatalog.Resolve(settings.SelectedSkinId);
        VisualSettings normalized = settings.Normalize();
        SKImage texture = worldTextureProvider.GetTexture(puzzleKey, skin, normalized.QualityTier);

        bool pieceLocal = forcePieceLocal || normalized.MappingMode == TextureMappingMode.PieceLocal;
        SKRect mappedRect = pieceLocal ? pieceRect : boardRect;

        SKShader shader = BuildCompositeShader(texture, skin, normalized, mappedRect, normalized.QualityTier, forBoard: false);
        ReplaceShader(ref pieceShader, pieceFillPaint, shader);

        pieceFillPaint.Color = SKColors.White;
        pieceFillPaint.BlendMode = SKBlendMode.SrcOver;

        return pieceFillPaint;
    }

    public SKPaint GetPieceShadowPaint(VisualSettings settings, bool isDragging, float elevationMultiplier)
    {
        VisualSettings normalized = settings.Normalize();
        SkinDefinition skin = SkinCatalog.Resolve(normalized.SelectedSkinId);

        float quantizedDepth = MathF.Round(normalized.DepthIntensity * 10f) / 10f;
        float quantizedElevation = MathF.Round(Math.Clamp(elevationMultiplier, 0.8f, 2.2f) * 10f) / 10f;
        string key = $"{skin.Id}:{normalized.QualityTier}:{quantizedDepth:F1}:{quantizedElevation:F1}:{isDragging}";

        if (shadowPaintCache.TryGetValue(key, out SKPaint? cached))
        {
            return cached;
        }

        float dragBoost = isDragging ? 1.28f : 1f;
        float alphaFactor = skin.ShadowStrength * quantizedDepth * quantizedElevation * dragBoost;
        byte alpha = (byte)Math.Clamp((int)MathF.Round(34f + (alphaFactor * 82f)), 28, 214);

        SKColor color = skin.ShadowColor.WithAlpha(alpha);
        SKPaint paint = new()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            BlendMode = SKBlendMode.Multiply,
            Color = color
        };

        shadowPaintCache[key] = paint;
        return paint;
    }

    public SKPaint GetPieceBevelPaint(VisualSettings settings, SKRect pieceRect, bool darkPass)
    {
        VisualSettings normalized = settings.Normalize();
        SkinDefinition skin = SkinCatalog.Resolve(normalized.SelectedSkinId);

        SKRect safeRect = NormalizeRect(pieceRect);

        SKColor startColor;
        SKColor endColor;

        if (darkPass)
        {
            byte darkAlpha = (byte)Math.Clamp((int)MathF.Round(120f * skin.BevelStrength * normalized.DepthIntensity), 28, 170);
            startColor = skin.ShadowColor.WithAlpha(darkAlpha);
            endColor = SKColors.Transparent;
        }
        else
        {
            byte lightAlpha = (byte)Math.Clamp((int)MathF.Round(132f * skin.BevelStrength * normalized.DepthIntensity), 30, 182);
            startColor = skin.KeyLightColor.WithAlpha(lightAlpha);
            endColor = SKColors.Transparent;
        }

        SKShader shader = SKShader.CreateLinearGradient(
            new SKPoint(safeRect.Left, safeRect.Top),
            new SKPoint(safeRect.Right, safeRect.Bottom),
            new[] { startColor, endColor },
            new[] { 0f, 1f },
            SKShaderTileMode.Clamp);

        float strokeWidth = darkPass
            ? 1.1f + (2.1f * skin.BevelStrength * normalized.DepthIntensity)
            : 0.9f + (1.8f * skin.BevelStrength * normalized.DepthIntensity);

        SKPaint targetPaint = darkPass ? bevelDarkPaint : bevelLightPaint;
        targetPaint.StrokeWidth = strokeWidth;

        if (darkPass)
        {
            ReplaceShader(ref bevelDarkShader, targetPaint, shader);
        }
        else
        {
            ReplaceShader(ref bevelLightShader, targetPaint, shader);
        }

        return targetPaint;
    }

    public SKPaint GetStripBackgroundPaint(PuzzleKey puzzleKey, VisualSettings settings, SKRect stripRect)
    {
        VisualSettings normalized = settings.Normalize();
        SkinDefinition skin = SkinCatalog.Resolve(normalized.SelectedSkinId);

        SKRect safeRect = NormalizeRect(stripRect);
        byte stripAlpha = (byte)Math.Clamp((int)MathF.Round(255f * normalized.StripOpacity), 24, 245);

        SKColor topColor = BlendColor(skin.StripColor, skin.FillLightColor, 0.38f).WithAlpha(stripAlpha);
        SKColor bottomColor = BlendColor(skin.StripColor, skin.ShadowColor, 0.32f)
            .WithAlpha((byte)Math.Clamp(stripAlpha - 24, 20, 240));

        SKShader gradientShader = SKShader.CreateLinearGradient(
            new SKPoint(safeRect.Left, safeRect.Top),
            new SKPoint(safeRect.Left, safeRect.Bottom),
            new[] { topColor, bottomColor },
            new[] { 0f, 1f },
            SKShaderTileMode.Clamp);

        GraphicsQualityTier frostQuality = normalized.QualityTier == GraphicsQualityTier.High
            ? GraphicsQualityTier.Medium
            : GraphicsQualityTier.Low;

        SKImage texture = worldTextureProvider.GetTexture(puzzleKey, skin, frostQuality);
        SKMatrix textureMatrix = CreateRectToTextureMatrix(safeRect, texture);
        SKShader textureShader = SKShader.CreateImage(
            texture,
            SKShaderTileMode.Repeat,
            SKShaderTileMode.Repeat,
            textureMatrix);

        SKShader frostedShader = SKShader.CreateCompose(gradientShader, textureShader, SKBlendMode.SoftLight);
        gradientShader.Dispose();
        textureShader.Dispose();

        byte frostAlpha = (byte)Math.Clamp((int)MathF.Round(168f * normalized.StripFrostAmount), 0, 168);
        if (frostAlpha > 0)
        {
            SKShader frostLightShader = SKShader.CreateLinearGradient(
                new SKPoint(safeRect.Left, safeRect.Top),
                new SKPoint(safeRect.Right, safeRect.Bottom),
                new[]
                {
                    skin.KeyLightColor.WithAlpha(frostAlpha),
                    SKColors.Transparent
                },
                new[] { 0f, 1f },
                SKShaderTileMode.Clamp);

            SKShader finalShader = SKShader.CreateCompose(frostedShader, frostLightShader, SKBlendMode.Screen);
            frostedShader.Dispose();
            frostLightShader.Dispose();
            frostedShader = finalShader;
        }

        ReplaceShader(ref stripBackgroundShader, stripBackgroundPaint, frostedShader);
        stripBackgroundPaint.Color = new SKColor(255, 255, 255, stripAlpha);

        return stripBackgroundPaint;
    }

    public SKPaint GetStripDividerPaint(VisualSettings settings)
    {
        VisualSettings normalized = settings.Normalize();
        SkinDefinition skin = SkinCatalog.Resolve(normalized.SelectedSkinId);

        float quantizedDepth = MathF.Round(normalized.DepthIntensity * 10f) / 10f;
        float quantizedOpacity = MathF.Round(normalized.StripOpacity * 10f) / 10f;
        string key = $"{skin.Id}:{quantizedDepth:F1}:{quantizedOpacity:F1}";

        if (dividerPaintCache.TryGetValue(key, out SKPaint? cached))
        {
            return cached;
        }

        byte dividerAlpha = (byte)Math.Clamp((int)MathF.Round((130f + (70f * quantizedDepth)) * quantizedOpacity), 18, 220);
        SKColor dividerColor = skin.KeyLightColor.WithAlpha(dividerAlpha);

        SKPaint paint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeWidth = 1.2f + (0.95f * quantizedDepth),
            BlendMode = SKBlendMode.Screen,
            Color = dividerColor,
            StrokeCap = SKStrokeCap.Round
        };

        dividerPaintCache[key] = paint;
        return paint;
    }

    public SKPaint GetOutlinePaint(VisualSettings settings)
    {
        VisualSettings normalized = settings.Normalize();
        SkinDefinition skin = SkinCatalog.Resolve(normalized.SelectedSkinId);
        string key = $"{skin.Id}:{normalized.ShowStrongOutlines}:{normalized.DepthIntensity:F1}";

        if (outlinePaintCache.TryGetValue(key, out SKPaint? cached))
        {
            return cached;
        }

        float depthScale = 0.75f + (normalized.DepthIntensity * 0.55f);

        SKColor outlineColor = normalized.ShowStrongOutlines
            ? skin.OutlineColor
            : BlendColor(skin.OutlineColor, skin.PieceBaseColor, 0.68f).WithAlpha(185);

        float strokeWidth = normalized.ShowStrongOutlines
            ? 1.8f * depthScale
            : 0.85f * depthScale;

        SKPaint paint = new()
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = strokeWidth,
            Color = outlineColor,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };

        outlinePaintCache[key] = paint;
        return paint;
    }

    public MaterialEffectFlags GetQualityEffects(GraphicsQualityTier qualityTier)
    {
        return qualityTier switch
        {
            GraphicsQualityTier.Low => new MaterialEffectFlags(
                UseGradient: true,
                UseTexture: false,
                UseShadow: true,
                UseAmbientOcclusion: false,
                UseBevel: false,
                UseRimHighlight: false,
                UseGlintOverlay: false),
            GraphicsQualityTier.Medium => new MaterialEffectFlags(
                UseGradient: true,
                UseTexture: true,
                UseShadow: true,
                UseAmbientOcclusion: true,
                UseBevel: true,
                UseRimHighlight: false,
                UseGlintOverlay: false),
            _ => new MaterialEffectFlags(
                UseGradient: true,
                UseTexture: true,
                UseShadow: true,
                UseAmbientOcclusion: true,
                UseBevel: true,
                UseRimHighlight: true,
                UseGlintOverlay: true)
        };
    }

    public void InvalidateCacheForSkinOrSeed(PuzzleKey puzzleKey, string skinId)
    {
        worldTextureProvider.Invalidate(puzzleKey, skinId);

        RemoveCachedPaintsForSkin(outlinePaintCache, skinId);
        RemoveCachedPaintsForSkin(shadowPaintCache, skinId);
        RemoveCachedPaintsForSkin(dividerPaintCache, skinId);
    }

    public void Dispose()
    {
        pieceShader?.Dispose();
        pieceFillPaint.Dispose();

        bevelLightShader?.Dispose();
        bevelDarkShader?.Dispose();
        bevelLightPaint.Dispose();
        bevelDarkPaint.Dispose();

        stripBackgroundShader?.Dispose();
        stripBackgroundPaint.Dispose();

        foreach (SKPaint paint in outlinePaintCache.Values)
        {
            paint.Dispose();
        }

        foreach (SKPaint paint in shadowPaintCache.Values)
        {
            paint.Dispose();
        }

        foreach (SKPaint paint in dividerPaintCache.Values)
        {
            paint.Dispose();
        }

        outlinePaintCache.Clear();
        shadowPaintCache.Clear();
        dividerPaintCache.Clear();
    }

    private static void RemoveCachedPaintsForSkin(Dictionary<string, SKPaint> cache, string skinId)
    {
        List<string> keys = cache.Keys
            .Where(key => key.StartsWith($"{skinId}:", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (string key in keys)
        {
            cache[key].Dispose();
            cache.Remove(key);
        }
    }

    private static void ReplaceShader(ref SKShader? targetShader, SKPaint paint, SKShader shader)
    {
        targetShader?.Dispose();
        targetShader = shader;
        paint.Shader = targetShader;
    }

    private SKShader BuildCompositeShader(
        SKImage texture,
        SkinDefinition skin,
        VisualSettings settings,
        SKRect targetRect,
        GraphicsQualityTier qualityTier,
        bool forBoard)
    {
        SKRect safeRect = NormalizeRect(targetRect);

        SKColor litTop = forBoard
            ? BlendColor(skin.BoardColor, skin.KeyLightColor, 0.54f)
            : BlendColor(skin.PieceBaseColor, skin.FillLightColor, 0.43f);

        SKColor litBottom = forBoard
            ? BlendColor(skin.BoardColor, skin.ShadowColor, 0.18f)
            : BlendColor(skin.PieceBaseColor, skin.ShadowColor, 0.24f);

        SKShader lightingGradient = SKShader.CreateLinearGradient(
            new SKPoint(safeRect.Left, safeRect.Top),
            new SKPoint(safeRect.Right, safeRect.Bottom),
            new[] { litTop, litBottom },
            new[] { 0f, 1f },
            SKShaderTileMode.Clamp);

        MaterialEffectFlags effects = GetQualityEffects(qualityTier);
        if (!effects.UseTexture)
        {
            return lightingGradient;
        }

        SKMatrix matrix = CreateRectToTextureMatrix(safeRect, texture);
        SKShader textureShader = SKShader.CreateImage(
            texture,
            SKShaderTileMode.Clamp,
            SKShaderTileMode.Clamp,
            matrix);

        SKShader textured = SKShader.CreateCompose(lightingGradient, textureShader, SKBlendMode.Overlay);
        lightingGradient.Dispose();
        textureShader.Dispose();

        float depth = settings.DepthIntensity;
        byte directionalAlpha = (byte)Math.Clamp((int)MathF.Round(75f * depth), 16, 90);
        SKShader directionalLight = SKShader.CreateLinearGradient(
            new SKPoint(safeRect.Left, safeRect.Top),
            new SKPoint(safeRect.Left + (safeRect.Width * 0.78f), safeRect.Top + (safeRect.Height * 0.78f)),
            new[]
            {
                skin.KeyLightColor.WithAlpha(directionalAlpha),
                SKColors.Transparent
            },
            new[] { 0f, 1f },
            SKShaderTileMode.Clamp);

        SKShader lit = SKShader.CreateCompose(textured, directionalLight, SKBlendMode.Screen);
        textured.Dispose();
        directionalLight.Dispose();

        if (!effects.UseRimHighlight)
        {
            return lit;
        }

        byte centerAlpha = (byte)Math.Clamp((int)MathF.Round(58f * depth), 10, 72);
        SKShader centerHighlight = SKShader.CreateRadialGradient(
            new SKPoint((safeRect.Left + safeRect.Right) * 0.5f, (safeRect.Top + safeRect.Bottom) * 0.5f),
            Math.Max(safeRect.Width, safeRect.Height) * 0.82f,
            new[]
            {
                skin.FillLightColor.WithAlpha(centerAlpha),
                SKColors.Transparent
            },
            new[] { 0f, 1f },
            SKShaderTileMode.Clamp);

        SKShader high = SKShader.CreateCompose(lit, centerHighlight, SKBlendMode.SoftLight);
        lit.Dispose();
        centerHighlight.Dispose();

        return high;
    }

    private static SKRect NormalizeRect(SKRect rect)
    {
        float width = Math.Max(rect.Width, 1f);
        float height = Math.Max(rect.Height, 1f);

        if (width == rect.Width && height == rect.Height)
        {
            return rect;
        }

        return new SKRect(rect.Left, rect.Top, rect.Left + width, rect.Top + height);
    }

    private static SKMatrix CreateRectToTextureMatrix(SKRect targetRect, SKImage texture)
    {
        SKRect safeRect = NormalizeRect(targetRect);

        float scaleX = texture.Width / safeRect.Width;
        float scaleY = texture.Height / safeRect.Height;

        float translateX = -safeRect.Left * scaleX;
        float translateY = -safeRect.Top * scaleY;

        return SKMatrix.CreateScaleTranslation(scaleX, scaleY, translateX, translateY);
    }

    private static SKColor BlendColor(SKColor from, SKColor to, float amount)
    {
        float t = Math.Clamp(amount, 0f, 1f);

        byte r = (byte)Math.Clamp((int)MathF.Round(from.Red + ((to.Red - from.Red) * t)), 0, 255);
        byte g = (byte)Math.Clamp((int)MathF.Round(from.Green + ((to.Green - from.Green) * t)), 0, 255);
        byte b = (byte)Math.Clamp((int)MathF.Round(from.Blue + ((to.Blue - from.Blue) * t)), 0, 255);
        byte a = (byte)Math.Clamp((int)MathF.Round(from.Alpha + ((to.Alpha - from.Alpha) * t)), 0, 255);

        return new SKColor(r, g, b, a);
    }
}
