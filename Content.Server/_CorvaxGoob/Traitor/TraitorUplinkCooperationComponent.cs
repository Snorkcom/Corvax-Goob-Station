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
    public EntityUid? OwnerMindId;

    /// <summary>
    /// Employer name shown when two traitor devices complete pairing.
    /// </summary>
    [DataField]
    public string EmployerName = string.Empty;

    /// <summary>
    /// Other traitor minds this specific device has already paired with.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> LinkedOwnerMindIds = new();

    /// <summary>
    /// Catalog listing IDs that already received a one-shot discount on this device.
    /// This also prevents deterministic radio implanter and emag rewards from being granted twice.
    /// </summary>
    [DataField]
    public HashSet<string> DiscountedListingIds = new();
}

/// <summary>
/// Relays store purchase events from a traitor mind without conflicting with the existing mind event subscriber.
/// </summary>
[RegisterComponent]
public sealed partial class TraitorUplinkPurchaseRelayComponent : Component;
