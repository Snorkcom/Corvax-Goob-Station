// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._CorvaxGoob.Traitor.HighRiskPinpointer;

namespace Content.Server._CorvaxGoob.Traitor.HighRiskPinpointer;

/// <summary>
/// Contains the fixed high-risk target list and resolves matching spawned entities for the pinpointer.
/// </summary>
public sealed partial class HighRiskPinpointerSystem
{
    private static readonly Dictionary<HighRiskPinpointerTarget, HighRiskTargetDefinition> HighRiskTargets = new()
    {
        [HighRiskPinpointerTarget.Hypospray] = new("Hypospray"),
        [HighRiskPinpointerTarget.ResearchDirectorHardsuit] = new("ClothingOuterHardsuitRd"),
        [HighRiskPinpointerTarget.HandTeleporter] = new("HandTeleporter"),
        [HighRiskPinpointerTarget.AdvancedMagboots] = new("ClothingShoesBootsMagAdv"),
        [HighRiskPinpointerTarget.QuartermasterClipboard] = new("BoxFolderQmClipboard"),
        [HighRiskPinpointerTarget.GoldenKnuckledusters] = new("ClothingHandsKnuckleDustersQM"),
        [HighRiskPinpointerTarget.CorgiMeat] = new("FoodMeatCorgi"),
        [HighRiskPinpointerTarget.HeadOfPersonnelCorgi] = new("MobCorgiIan", "MobCorgiIanOld", "MobCorgiLisa", "MobCorgiIanPup"),
        [HighRiskPinpointerTarget.CaptainId] = new("CaptainIDCard"),
        [HighRiskPinpointerTarget.CaptainJetpack] = new("JetpackCaptainFilled"),
        [HighRiskPinpointerTarget.AntiqueLaser] = new("WeaponAntiqueLaser"),
        [HighRiskPinpointerTarget.NuclearDisk] = new("NukeDisk"),
        [HighRiskPinpointerTarget.HeadOfSecurityMagnum] = new("WeaponEnergyMagnum"),
        [HighRiskPinpointerTarget.WardenShotgun] = new("WeaponEnergyShotgun")
    };

    private List<EntityUid> FindEntitiesByPrototype(IReadOnlySet<string> prototypes)
    {
        var matches = new List<EntityUid>();
        var query = EntityQueryEnumerator<MetaDataComponent>();

        while (query.MoveNext(out var uid, out var metadata))
        {
            if (metadata.EntityPrototype is { } prototype && prototypes.Contains(prototype.ID))
                matches.Add(uid);
        }

        return matches;
    }

    private sealed class HighRiskTargetDefinition(params string[] prototypes)
    {
        public readonly HashSet<string> Prototypes = new(prototypes);
    }
}
