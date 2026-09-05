// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxGoob.Traitor.HighRiskPinpointer;

/// <summary>
/// Marks a Syndicate pinpointer that can select station high-risk targets or a target by DNA.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HighRiskPinpointerComponent : Component;

[Serializable, NetSerializable]
public enum HighRiskPinpointerUiKey : byte
{
    Key
}

/// <summary>
/// Fixed target choices displayed by the pinpointer interface.
/// The server maps these choices to the exact prototypes used by traitor steal objectives.
/// </summary>
[Serializable, NetSerializable]
public enum HighRiskPinpointerTarget : byte
{
    Hypospray,
    ResearchDirectorHardsuit,
    HandTeleporter,
    AdvancedMagboots,
    QuartermasterClipboard,
    GoldenKnuckledusters,
    CorgiMeat,
    HeadOfPersonnelCorgi,
    CaptainId,
    CaptainJetpack,
    AntiqueLaser,
    NuclearDisk,
    HeadOfSecurityMagnum,
    WardenShotgun
}

[Serializable, NetSerializable]
public sealed class HighRiskPinpointerSelectTargetMessage(HighRiskPinpointerTarget target) : BoundUserInterfaceMessage
{
    public readonly HighRiskPinpointerTarget Target = target;
}

[Serializable, NetSerializable]
public sealed class HighRiskPinpointerTrackDnaMessage(string dna) : BoundUserInterfaceMessage
{
    public readonly string Dna = dna;
}
