// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server.Traitor.Cooperation;

/// <summary>
/// Per-device state for traitor uplink cooperation rewards and identity disclosure.
/// The component is attached to the actual uplink store entity, not to the current item holder.
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
    /// Source catalog listing IDs that already received a one-shot discount on this uplink.
    /// Used to prevent granting the same discounted item again, even after its temporary sale listing is bought and removed.
    /// </summary>
    [DataField]
    public HashSet<string> DiscountedListingIds = new();
}

/// <summary>
/// Marker component added to the traitor mind so this system can receive uplink purchase events.
/// It does not store data; it avoids subscribing to the same MindComponent purchase event as other systems.
/// </summary>
[RegisterComponent]
public sealed partial class TraitorUplinkPurchaseRelayComponent : Component;
