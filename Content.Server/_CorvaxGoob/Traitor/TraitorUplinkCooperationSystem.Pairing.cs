// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.Traitor.Cooperation;

namespace Content.Server.Traitor.Cooperation;

/// <summary>
/// Handles traitor device pairing, including the do-after, employer whispers, and reward dispatch.
/// </summary>
public sealed partial class TraitorUplinkCooperationSystem
{
    private static readonly TimeSpan PairingDuration = TimeSpan.FromSeconds(5);
    private const float PairingDistanceThreshold = 2f;

    private void OnUplinkAfterInteract(Entity<TraitorUplinkCooperationComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target || target == ent.Owner)
            return;

        // Security metagame protection: mindshielded users cannot probe traitor devices for pairing feedback.
        if (HasComp<MindShieldComponent>(args.User))
            return;

        if (!TryComp<TraitorUplinkCooperationComponent>(target, out var targetComp))
            return;

        if (!TryValidatePairing((ent.Owner, ent.Comp), (target, targetComp), out _, out _, out var failureMessage))
        {
            if (failureMessage != null)
                _popup.PopupEntity(failureMessage, ent.Owner, args.User, PopupType.SmallCaution);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager,
            args.User,
            PairingDuration,
            new TraitorUplinkLinkDoAfterEvent(),
            ent.Owner,
            target: target,
            used: ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnHandChange = true,
            DistanceThreshold = PairingDistanceThreshold,
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

        if (HasComp<MindShieldComponent>(args.User))
            return;

        if (!TryComp<TraitorUplinkCooperationComponent>(target, out var targetComp))
            return;

        if (!TryValidatePairing((ent.Owner, ent.Comp),
                (target, targetComp),
                out var sourceMindId,
                out var targetMindId,
                out _))
            return;

        CompletePairing((ent.Owner, ent.Comp), (target, targetComp), sourceMindId, targetMindId);
        args.Handled = true;
    }

    private bool TryValidatePairing(
        Entity<TraitorUplinkCooperationComponent> source,
        Entity<TraitorUplinkCooperationComponent> target,
        out EntityUid sourceMindId,
        out EntityUid targetMindId,
        out string? failureMessage)
    {
        sourceMindId = default;
        targetMindId = default;
        failureMessage = null;

        if (!HasExistingUplink(source.Owner) || !HasExistingUplink(target.Owner))
            return false;

        // Uniqueness is scoped to the source device and keyed by the other traitor mind.
        if (source.Comp.OwnerMindId is not { } sourceOwnerMindId ||
            target.Comp.OwnerMindId is not { } targetOwnerMindId ||
            sourceOwnerMindId == targetOwnerMindId)
            return false;

        sourceMindId = sourceOwnerMindId;
        targetMindId = targetOwnerMindId;

        if (source.Comp.LinkedOwnerMindIds.Contains(targetMindId))
        {
            failureMessage = Loc.GetString("traitor-cooperation-uplink-link-already-linked");
            return false;
        }

        return true;
    }

    private void CompletePairing(
        Entity<TraitorUplinkCooperationComponent> source,
        Entity<TraitorUplinkCooperationComponent> target,
        EntityUid sourceMindId,
        EntityUid targetMindId)
    {
        source.Comp.LinkedOwnerMindIds.Add(targetMindId);
        target.Comp.LinkedOwnerMindIds.Add(sourceMindId);

        var sourceLinkCount = source.Comp.LinkedOwnerMindIds.Count;
        var targetLinkCount = target.Comp.LinkedOwnerMindIds.Count;

        // Both devices receive their fixed rewards on their own first pairing, regardless of which one initiated it.
        GrantPairingRewards(source, sourceLinkCount);
        GrantPairingRewards(target, targetLinkCount);

        var sourceEmployer = GetEmployerDisplayName(source.Comp.EmployerName);
        var targetEmployer = GetEmployerDisplayName(target.Comp.EmployerName);
        var message = Loc.GetString("traitor-cooperation-uplink-link-whisper",
            ("first", sourceEmployer),
            ("second", targetEmployer));

        WhisperFromDevice(source.Owner, message);
        WhisperFromDevice(target.Owner, message);
    }

    private string GetEmployerDisplayName(string employerName)
    {
        return string.IsNullOrWhiteSpace(employerName)
            ? Loc.GetString("traitor-cooperation-uplink-employer-unknown")
            : employerName;
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
