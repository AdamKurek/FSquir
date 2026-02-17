namespace Fillsquir.Domain;

public sealed class PlacedFragmentState
{
    public int FragmentIndex { get; set; }
    public float PositionXWorld { get; set; }
    public float PositionYWorld { get; set; }
    public bool WasTouched { get; set; }
}
