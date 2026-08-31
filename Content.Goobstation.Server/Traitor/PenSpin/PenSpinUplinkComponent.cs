namespace Content.Goobstation.Server.Traitor.PenSpin;

[RegisterComponent]
public sealed partial class PenSpinUplinkComponent : Component
{
    [DataField]
    public int[]? Code;

    [DataField]
    public bool Unlocked;

    [DataField]
    public int[] CurrentCombination = Array.Empty<int>();

    [DataField]
    public int CurrentIndex;

    [DataField]
    public TimeSpan? NextSpinTime;

    /// <summary>
    /// CorvaxGoob. Indicates that the pen uplink may be opened without matching the generated spin code.
    /// </summary>
    [DataField]
    public bool PermanentlyUnlocked;
}
