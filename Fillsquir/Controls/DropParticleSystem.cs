using System.Buffers;
using Fillsquir.Visuals;
using SkiaSharp;

namespace Fillsquir.Controls;

internal sealed class DropParticleSystem
{
    private readonly List<Particle> particles = new();
    private readonly Random random = new();
    private readonly SKPaint fillPaint = new()
    {
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
        BlendMode = SKBlendMode.Screen
    };

    private readonly SKPaint streakPaint = new()
    {
        Style = SKPaintStyle.Stroke,
        IsAntialias = true,
        BlendMode = SKBlendMode.Screen,
        StrokeCap = SKStrokeCap.Round
    };

    internal bool HasActiveParticles => particles.Count > 0;

    internal void Spawn(
        SKPoint[] perimeterPoints,
        SKColor primaryColor,
        SKColor accentColor,
        int count,
        in DropParticleProfile profile,
        double nowSeconds)
    {
        if (perimeterPoints is null || perimeterPoints.Length < 2)
        {
            return;
        }

        int spawnCount = Math.Clamp(count, 8, 96);
        if (particles.Capacity < particles.Count + spawnCount)
        {
            particles.Capacity = Math.Max(particles.Count + spawnCount, Math.Max(32, particles.Capacity * 2));
        }

        SKPoint centroid = AveragePoint(perimeterPoints);
        int segmentCount = perimeterPoints.Length;
        float[] segmentLengths = ArrayPool<float>.Shared.Rent(segmentCount);
        try
        {
            float perimeter = 0f;
            for (int i = 0; i < segmentCount; i++)
            {
                SKPoint start = perimeterPoints[i];
                SKPoint end = perimeterPoints[(i + 1) % segmentCount];
                float length = Distance(start, end);
                segmentLengths[i] = length;
                perimeter += length;
            }

            if (perimeter <= 0.001f)
            {
                return;
            }

            float speedMin = Math.Max(18f, profile.SpeedMin);
            float speedMax = Math.Max(speedMin + 0.1f, profile.SpeedMax);
            float lifeMin = Math.Max(0.08f, profile.LifeMinSeconds);
            float lifeMax = Math.Max(lifeMin + 0.01f, profile.LifeMaxSeconds);
            float radiusMin = Math.Max(0.2f, profile.RadiusMin);
            float radiusMax = Math.Max(radiusMin + 0.05f, profile.RadiusMax);
            float gravityMin = Math.Max(0f, profile.GravityMin);
            float gravityMax = Math.Max(gravityMin + 1f, profile.GravityMax);
            float outwardBias = Math.Clamp(profile.OutwardBias, 0.2f, 1.8f);
            float tangentJitter = Math.Clamp(profile.TangentJitter, 0f, 0.85f);
            float spawnJitter = Math.Max(0f, profile.SpawnJitter);

            for (int i = 0; i < spawnCount; i++)
            {
                SamplePointOnPerimeter(perimeterPoints, segmentLengths, segmentCount, perimeter, out SKPoint origin, out SKPoint edgeDirection);

                SKPoint radialDirection = Normalize(new SKPoint(origin.X - centroid.X, origin.Y - centroid.Y));
                SKPoint normalDirection = Normalize(Perpendicular(edgeDirection));
                if (Dot(normalDirection, radialDirection) < 0f)
                {
                    normalDirection = new SKPoint(-normalDirection.X, -normalDirection.Y);
                }

                SKPoint direction = Normalize(new SKPoint(
                    (normalDirection.X * NextFloat(outwardBias * 0.72f, outwardBias * 1.08f)) +
                    (radialDirection.X * NextFloat(0.22f, 0.68f)) +
                    (edgeDirection.X * NextFloat(-tangentJitter, tangentJitter)),
                    (normalDirection.Y * NextFloat(outwardBias * 0.72f, outwardBias * 1.08f)) +
                    (radialDirection.Y * NextFloat(0.22f, 0.68f)) +
                    (edgeDirection.Y * NextFloat(-tangentJitter, tangentJitter))));

                if (direction.X == 0f && direction.Y == 0f)
                {
                    direction = radialDirection;
                }

                if (direction.X == 0f && direction.Y == 0f)
                {
                    float angle = (float)(random.NextDouble() * Math.PI * 2d);
                    direction = new SKPoint(MathF.Cos(angle), MathF.Sin(angle));
                }

                float speed = NextFloat(speedMin, speedMax);
                float upwardLift = NextFloat(speed * 0.08f, speed * 0.25f);

                SKPoint velocity = new(
                    (direction.X * speed),
                    (direction.Y * speed) - upwardLift);

                float tangentOffset = spawnJitter * 0.42f;
                origin = new SKPoint(
                    origin.X + (normalDirection.X * NextFloat(-spawnJitter, spawnJitter)) + (edgeDirection.X * NextFloat(-tangentOffset, tangentOffset)),
                    origin.Y + (normalDirection.Y * NextFloat(-spawnJitter, spawnJitter)) + (edgeDirection.Y * NextFloat(-tangentOffset, tangentOffset)));

                Particle particle = new()
                {
                    Origin = origin,
                    Velocity = velocity,
                    Gravity = NextFloat(gravityMin, gravityMax),
                    Radius = NextFloat(radiusMin, radiusMax),
                    LifeSeconds = NextFloat(lifeMin, lifeMax),
                    BornAtSeconds = nowSeconds,
                    BaseColor = LerpColor(primaryColor, accentColor, NextFloat(0.12f, 0.72f))
                };

                particles.Add(particle);
            }
        }
        finally
        {
            ArrayPool<float>.Shared.Return(segmentLengths, clearArray: false);
        }
    }

    internal void Draw(SKCanvas canvas, double nowSeconds)
    {
        if (particles.Count == 0)
        {
            return;
        }

        int write = 0;

        for (int i = 0; i < particles.Count; i++)
        {
            Particle particle = particles[i];
            float age = (float)(nowSeconds - particle.BornAtSeconds);

            if (age <= 0f || age >= particle.LifeSeconds)
            {
                continue;
            }

            float t = age / particle.LifeSeconds;
            float invT = 1f - t;

            SKPoint offset = new(
                (particle.Velocity.X * age),
                (particle.Velocity.Y * age) + (0.5f * particle.Gravity * age * age));
            SKPoint position = new(particle.Origin.X + offset.X, particle.Origin.Y + offset.Y);

            byte alpha = (byte)Math.Clamp((int)MathF.Round((invT * invT) * particle.BaseColor.Alpha), 0, 255);
            float radius = Math.Max(0.6f, particle.Radius * (0.45f + (0.55f * invT)));

            fillPaint.Color = particle.BaseColor.WithAlpha(alpha);
            canvas.DrawCircle(position.X, position.Y, radius, fillPaint);

            if (i % 4 == 0)
            {
                float streak = 1.2f + (1.8f * invT);
                streakPaint.StrokeWidth = Math.Max(0.8f, radius * 0.72f);
                streakPaint.Color = particle.BaseColor.WithAlpha((byte)Math.Clamp(alpha / 2, 0, 255));
                canvas.DrawLine(
                    position.X,
                    position.Y,
                    position.X - (particle.Velocity.X * 0.011f * streak),
                    position.Y - (particle.Velocity.Y * 0.011f * streak),
                    streakPaint);
            }

            particles[write++] = particle;
        }

        if (write < particles.Count)
        {
            particles.RemoveRange(write, particles.Count - write);
        }
    }

    private float NextFloat(float min, float max)
    {
        return min + ((float)random.NextDouble() * (max - min));
    }

    private static SKColor LerpColor(SKColor from, SKColor to, float t)
    {
        t = Math.Clamp(t, 0f, 1f);

        byte r = (byte)Math.Clamp((int)MathF.Round(from.Red + ((to.Red - from.Red) * t)), 0, 255);
        byte g = (byte)Math.Clamp((int)MathF.Round(from.Green + ((to.Green - from.Green) * t)), 0, 255);
        byte b = (byte)Math.Clamp((int)MathF.Round(from.Blue + ((to.Blue - from.Blue) * t)), 0, 255);
        byte a = (byte)Math.Clamp((int)MathF.Round(from.Alpha + ((to.Alpha - from.Alpha) * t)), 0, 255);

        return new SKColor(r, g, b, a);
    }

    private void SamplePointOnPerimeter(
        SKPoint[] points,
        float[] segmentLengths,
        int segmentCount,
        float totalPerimeter,
        out SKPoint point,
        out SKPoint edgeDirection)
    {
        float sample = NextFloat(0f, totalPerimeter);
        float walked = 0f;

        for (int i = 0; i < segmentCount; i++)
        {
            float length = segmentLengths[i];
            if (length <= 0f)
            {
                continue;
            }

            float segmentEnd = walked + length;
            if (sample <= segmentEnd || i == segmentCount - 1)
            {
                SKPoint start = points[i];
                SKPoint end = points[(i + 1) % points.Length];
                float t = (sample - walked) / length;
                point = new SKPoint(
                    start.X + ((end.X - start.X) * t),
                    start.Y + ((end.Y - start.Y) * t));
                edgeDirection = Normalize(new SKPoint(end.X - start.X, end.Y - start.Y));
                return;
            }

            walked = segmentEnd;
        }

        point = points[0];
        edgeDirection = new SKPoint(1f, 0f);
    }

    private static SKPoint AveragePoint(SKPoint[] points)
    {
        float sumX = 0f;
        float sumY = 0f;

        for (int i = 0; i < points.Length; i++)
        {
            sumX += points[i].X;
            sumY += points[i].Y;
        }

        float inv = 1f / points.Length;
        return new SKPoint(sumX * inv, sumY * inv);
    }

    private static float Distance(SKPoint a, SKPoint b)
    {
        float x = b.X - a.X;
        float y = b.Y - a.Y;
        return MathF.Sqrt((x * x) + (y * y));
    }

    private static float Dot(SKPoint left, SKPoint right)
    {
        return (left.X * right.X) + (left.Y * right.Y);
    }

    private static SKPoint Perpendicular(SKPoint vector)
    {
        return new SKPoint(-vector.Y, vector.X);
    }

    private static SKPoint Normalize(SKPoint vector)
    {
        float length = MathF.Sqrt((vector.X * vector.X) + (vector.Y * vector.Y));
        if (length <= 0.0001f)
        {
            return new SKPoint(0f, 0f);
        }

        return new SKPoint(vector.X / length, vector.Y / length);
    }

    private struct Particle
    {
        internal SKPoint Origin;
        internal SKPoint Velocity;
        internal float Gravity;
        internal float Radius;
        internal float LifeSeconds;
        internal double BornAtSeconds;
        internal SKColor BaseColor;
    }
}
