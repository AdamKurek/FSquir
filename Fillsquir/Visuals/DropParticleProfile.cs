namespace Fillsquir.Visuals;

public readonly record struct DropParticleProfile(
    float CountScale,
    float SpeedMin,
    float SpeedMax,
    float LifeMinSeconds,
    float LifeMaxSeconds,
    float RadiusMin,
    float RadiusMax,
    float GravityMin,
    float GravityMax,
    float OutwardBias,
    float TangentJitter,
    float SpawnJitter)
{
    public static readonly DropParticleProfile Default = new(
        CountScale: 1f,
        SpeedMin: 52f,
        SpeedMax: 178f,
        LifeMinSeconds: 0.32f,
        LifeMaxSeconds: 0.68f,
        RadiusMin: 1.6f,
        RadiusMax: 4.6f,
        GravityMin: 92f,
        GravityMax: 240f,
        OutwardBias: 0.82f,
        TangentJitter: 0.24f,
        SpawnJitter: 1.25f);
}
