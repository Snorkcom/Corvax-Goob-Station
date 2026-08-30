// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.Store.Components;
using Content.Shared.Traitor.Cooperation;

namespace Content.Server.Traitor.Cooperation;

/// <summary>
/// Handles traitor device pairing, including the do-after, employer whispers, and reward dispatch.
/// </summary>
public sealed partial class TraitorUplinkCooperationSystem
{
    private void OnUplinkAfterInteract(Entity<TraitorUplinkCooperationComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target || target == ent.Owner)
            return;

        // Security metagame protection: mindshielded users cannot probe traitor devices for pairing feedback.
        if (HasComp<MindShieldComponent>(args.User))
            return;

        if (!TryComp<TraitorUplinkCooperationComponent>(target, out var targetComp))
            return;

        if (!CanLinkUplinks((ent.Owner, ent.Comp), (target, targetComp), out var reason))
        {
            if (reason != null)
                _popup.PopupEntity(reason, ent.Owner, args.User, PopupType.SmallCaution);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager,
            args.User,
            TimeSpan.FromSeconds(5),
            new TraitorUplinkLinkDoAfterEvent(),
            ent.Owner,
            target: target,
            used: ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnHandChange = true,
            DistanceThreshold = 2f,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _popup.PopupEntity(Loc.GetString("traitor-cooperation-uplink-link-start"), ent.Owner, args.User);
        args.Handled = true;
    }

    private void OnUplinkLinkDoAfter(Entity<TraitorUplinkCooperationComponent> ent, ref TraitorUplinkLinkDoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not { } target)
            return;

        if (!TryComp<TraitorUplinkCooperationComponent>(target, out var targetComp))
            return;

        if (!CanLinkUplinks((ent.Owner, ent.Comp), (target, targetComp), out _))
            return;

        LinkUplinks((ent.Owner, ent.Comp), (target, targetComp));
    }

    private bool CanLinkUplinks(
        Entity<TraitorUplinkCooperationComponent> source,
        Entity<TraitorUplinkCooperationComponent> target,
        out string? reason)
    {
        reason = null;

        if (!HasExistingUplink(source.Owner) || !HasExistingUplink(target.Owner))
            return false;

        // Uniqueness is scoped to the source device and keyed by the other traitor mind.
        if (!TryGetUplinkOwnerMind(source, out var sourceMind) ||
            !TryGetUplinkOwnerMind(target, out var targetMind) ||
            sourceMind == targetMind)
            return false;

        if (source.Comp.LinkedMinds.Contains(targetMind.Value))
        {
            reason = Loc.GetString("traitor-cooperation-uplink-link-already-linked");
            return false;
        }

        return true;
    }

    private bool TryGetUplinkOwnerMind(
        Entity<TraitorUplinkCooperationComponent> uplink,
        [NotNullWhen(true)] out EntityUid? mindId)
    {
        mindId = uplink.Comp.OwnerMind;

        if (mindId != null)
            return true;

        // Fall back to StoreComponent for any uplink that existed before cooperation metadata was attached.
        if (TryComp<StoreComponent>(uplink.Owner, out var store) && store.AccountOwner != null)
        {
            mindId = store.AccountOwner;
            return true;
        }

        return false;
    }

    private void LinkUplinks(Entity<TraitorUplinkCooperationComponent> source, Entity<TraitorUplinkCooperationComponent> target)
    {
        if (!TryGetUplinkOwnerMind(source, out var sourceMind) ||
            !TryGetUplinkOwnerMind(target, out var targetMind))
            return;

        source.Comp.LinkedMinds.Add(targetMind.Value);
        target.Comp.LinkedMinds.Add(sourceMind.Value);

        // The radio implanter reward is first-link and initiator-only.
        if (source.Comp.LinkedMinds.Count == 1)
            GrantRadioImplanterDiscount(source);

        GrantCooperationDiscounts(source, source.Comp.LinkedMinds.Count);
        GrantCooperationDiscounts(target, target.Comp.LinkedMinds.Count);
        GrantEmagDiscount(source);

        var sourceEmployer = GetEmployerName(source.Comp);
        var targetEmployer = GetEmployerName(target.Comp);
        var message = Loc.GetString("traitor-cooperation-uplink-link-whisper",
            ("first", sourceEmployer),
            ("second", targetEmployer));

        WhisperFromDevice(source.Owner, message);
        WhisperFromDevice(target.Owner, message);

        Dirty(source);
        Dirty(target);
    }

    private string GetEmployerName(TraitorUplinkCooperationComponent comp)
    {
        return string.IsNullOrWhiteSpace(comp.Employer)
            ? Loc.GetString("traitor-cooperation-uplink-employer-unknown")
            : comp.Employer;
    }

    private void WhisperFromDevice(EntityUid uid, string message)
    {
        _chat.TrySendInGameICMessage(uid,
            message,
            InGameICChatType.Whisper,
            ChatTransmitRange.Normal,
            hideLog: true,
            nameOverride: Name(uid),
            ignoreActionBlocker: true,
            forced: true);
    }
}
