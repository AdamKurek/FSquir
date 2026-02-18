using Fillsquir.Domain;
using SkiaSharp;

namespace Fillsquir.Visuals;

public sealed class WorldTextureProvider : IDisposable
{
    private readonly Dictionary<string, SKImage> cache = new(StringComparer.Ordinal);

    public SKImage GetTexture(PuzzleKey puzzleKey, SkinDefinition skin, GraphicsQualityTier qualityTier)
    {
        string cacheKey = $"{PuzzleKey.BuildStableId(puzzleKey)}:{skin.Id}:{qualityTier}";
        if (cache.TryGetValue(cacheKey, out SKImage? cached))
        {
            return cached;
        }

        SKImage created = CreateTexture(puzzleKey, skin, qualityTier);
        cache[cacheKey] = created;
        return created;
    }

    public void Invalidate(PuzzleKey puzzleKey, string skinId)
    {
        string prefix = $"{PuzzleKey.BuildStableId(puzzleKey)}:{skinId}:";
        List<string> keys = cache.Keys
            .Where(key => key.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();

        foreach (string key in keys)
        {
            cache[key].Dispose();
            cache.Remove(key);
        }
    }

    public void Dispose()
    {
        foreach (SKImage image in cache.Values)
        {
            image.Dispose();
        }

        cache.Clear();
    }

    private static SKImage CreateTexture(PuzzleKey puzzleKey, SkinDefinition skin, GraphicsQualityTier qualityTier)
    {
        int size = qualityTier switch
        {
            GraphicsQualityTier.Low => 256,
            GraphicsQualityTier.Medium => 512,
            _ => 1024
        };

        int seed = HashCode.Combine(puzzleKey.Level, puzzleKey.Seed, puzzleKey.RulesVersion, skin.Id);

        using SKBitmap bitmap = new(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);

        for (int y = 0; y < size; y++)
        {
            float ny = y / (float)(size - 1);
            for (int x = 0; x < size; x++)
            {
                float nx = x / (float)(size - 1);
                SKColor color = SampleColor(nx, ny, seed, skin, qualityTier);
                bitmap.SetPixel(x, y, color);
            }
        }

        bitmap.SetImmutable();
        return SKImage.FromBitmap(bitmap);
    }

    private static SKColor SampleColor(float nx, float ny, int seed, SkinDefinition skin, GraphicsQualityTier qualityTier)
    {
        float coarseNoise = ValueNoise(nx * skin.NoiseScale, ny * skin.NoiseScale, seed);
        float detailNoise = qualityTier == GraphicsQualityTier.Low
            ? 0f
            : ValueNoise(nx * skin.NoiseScale * 2.7f, ny * skin.NoiseScale * 2.7f, seed + 137);

        float combinedNoise = (coarseNoise * 0.72f) + (detailNoise * 0.28f);
        float contrasted = ((combinedNoise - 0.5f) * skin.Contrast) + 0.5f;

        float dx = nx - 0.5f;
        float dy = ny - 0.5f;
        float distance = MathF.Sqrt((dx * dx) + (dy * dy)) * 1.42f;
        float vignette = 1f - (Math.Clamp(distance, 0f, 1f) * skin.VignetteIntensity);

        float sample = Math.Clamp(contrasted * vignette, 0f, 1f);

        // Directional light response keeps texture deterministic but less flat.
        float directionalLight = Math.Clamp(0.58f + ((0.5f - nx) * 0.24f) + ((0.5f - ny) * 0.28f), 0f, 1f);
        sample = Math.Clamp(Lerp(sample, directionalLight, 0.18f + (skin.AccentIntensity * 0.10f)), 0f, 1f);

        if (qualityTier == GraphicsQualityTier.High)
        {
            float accentWave = (MathF.Sin((nx + ny) * 18f + (seed * 0.0001f)) + 1f) * 0.5f;
            sample = Math.Clamp(Lerp(sample, accentWave, skin.AccentIntensity * 0.25f), 0f, 1f);
        }

        SKColor baseColor = LerpColor(skin.TextureLowColor, skin.TextureHighColor, sample);

        if (qualityTier == GraphicsQualityTier.High)
        {
            SKColor accent = skin.HoverColor.WithAlpha((byte)(85 + (skin.AccentIntensity * 70f)));
            return LerpColor(baseColor, accent, 0.12f);
        }

        return baseColor;
    }

    private static float ValueNoise(float x, float y, int seed)
    {
        int x0 = (int)MathF.Floor(x);
        int y0 = (int)MathF.Floor(y);

        int x1 = x0 + 1;
        int y1 = y0 + 1;

        float tx = x - x0;
        float ty = y - y0;

        float s = HashToUnit(x0, y0, seed);
        float t = HashToUnit(x1, y0, seed);
        float u = HashToUnit(x0, y1, seed);
        float v = HashToUnit(x1, y1, seed);

        float sx = SmoothStep(tx);
        float sy = SmoothStep(ty);

        float blendTop = Lerp(s, t, sx);
        float blendBottom = Lerp(u, v, sx);

        return Lerp(blendTop, blendBottom, sy);
    }

    private static float HashToUnit(int x, int y, int seed)
    {
        int hash = seed;
        hash ^= x * 374761393;
        hash = (hash << 13) ^ hash;
        hash ^= y * 668265263;
        hash = (hash << 17) ^ hash;
        hash = unchecked(hash * 1274126177);

        uint normalized = (uint)hash;
        return (normalized & 0x00FFFFFF) / 16777215f;
    }

    private static float SmoothStep(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return t * t * (3f - (2f * t));
    }

    private static float Lerp(float from, float to, float amount)
    {
        return from + ((to - from) * amount);
    }

    private static SKColor LerpColor(SKColor from, SKColor to, float amount)
    {
        byte r = (byte)Math.Clamp((int)MathF.Round(Lerp(from.Red, to.Red, amount)), 0, 255);
        byte g = (byte)Math.Clamp((int)MathF.Round(Lerp(from.Green, to.Green, amount)), 0, 255);
        byte b = (byte)Math.Clamp((int)MathF.Round(Lerp(from.Blue, to.Blue, amount)), 0, 255);
        byte a = (byte)Math.Clamp((int)MathF.Round(Lerp(from.Alpha, to.Alpha, amount)), 0, 255);
        return new SKColor(r, g, b, a);
    }
}
