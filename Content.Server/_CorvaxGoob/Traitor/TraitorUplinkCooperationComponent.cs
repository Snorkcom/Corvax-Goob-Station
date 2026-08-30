// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server.Traitor.Cooperation;

/// <summary>
/// Per-device state for traitor uplink cooperation rewards and identity disclosure.
/// </summary>
[RegisterComponent]
public sealed partial class TraitorUplinkCooperationComponent : Component
{
    /// <summary>
    /// The traitor mind that owns this uplink for per-device uniqueness checks.
    /// </summary>
    [DataField]
    public EntityUid? OwnerMind;

    /// <summary>
    /// Employer name shown when two traitor devices complete pairing.
    /// </summary>
    [DataField]
    public string Employer = string.Empty;

    /// <summary>
    /// Other traitor minds this specific device has already paired with.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> LinkedMinds = new();

    /// <summary>
    /// Prevents more than one free radio implanter discount on this device.
    /// </summary>
    [DataField]
    public bool RadioImplanterDiscountGranted;

    /// <summary>
    /// Prevents more than one emag discount on this device.
    /// </summary>
    [DataField]
    public bool EmagDiscountGranted;

    /// <summary>
    /// Catalog listing ids that already received a manually cloned discount on this device.
    /// </summary>
    [DataField]
    public HashSet<string> GrantedManualDiscountListings = new();
}
