using Fillsquir.Visuals;

namespace tests;

[TestClass]
public class VisualSettingsTests
{
    [TestMethod]
    public async Task VisualSettingsState_RoundTripsAllFields()
    {
        MemoryVisualSettingsStore store = new();
        VisualSettingsState state = new(store);

        VisualSettings expected = new()
        {
            SelectedSkinId = "nature",
            QualityTier = GraphicsQualityTier.High,
            MappingMode = TextureMappingMode.PieceLocal,
            ShowStrongOutlines = false,
            DepthIntensity = 0.83f,
            StripOpacity = 0.67f,
            StripFrostAmount = 0.42f
        };

        await state.UpdateAsync(expected);
        VisualSettings actual = await state.LoadAsync();

        Assert.AreEqual(expected.SelectedSkinId, actual.SelectedSkinId);
        Assert.AreEqual(expected.QualityTier, actual.QualityTier);
        Assert.AreEqual(expected.MappingMode, actual.MappingMode);
        Assert.AreEqual(expected.ShowStrongOutlines, actual.ShowStrongOutlines);
        Assert.AreEqual(expected.DepthIntensity, actual.DepthIntensity, 0.0001f);
        Assert.AreEqual(expected.StripOpacity, actual.StripOpacity, 0.0001f);
        Assert.AreEqual(expected.StripFrostAmount, actual.StripFrostAmount, 0.0001f);
    }

    private sealed class MemoryVisualSettingsStore : IVisualSettingsStore
    {
        private VisualSettings current = new();

        public Task<VisualSettings> LoadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(current.Clone());
        }

        public Task SaveAsync(VisualSettings settings, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            current = settings.Clone();
            return Task.CompletedTask;
        }
    }
}
