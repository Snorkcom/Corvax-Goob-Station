// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Mind;
using Content.Server.Pinpointer;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Shared._CorvaxGoob.Traitor.HighRiskPinpointer;
using Content.Shared.Forensics.Components;
using Content.Shared.Pinpointer;
using Content.Shared.Roles.Components;
using Content.Shared.UserInterface;

namespace Content.Server._CorvaxGoob.Traitor.HighRiskPinpointer;

/// <summary>
/// Lets traitors configure a purchased pinpointer to track high-risk steal targets or an exact DNA sequence.
/// </summary>
public sealed partial class HighRiskPinpointerSystem : EntitySystem
{
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private PinpointerSystem _pinpointer = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private RoleSystem _roles = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HighRiskPinpointerComponent, ActivatableUIOpenAttemptEvent>(OnUiOpenAttempt);
        SubscribeLocalEvent<HighRiskPinpointerComponent, HighRiskPinpointerSelectTargetMessage>(OnTargetSelected);
        SubscribeLocalEvent<HighRiskPinpointerComponent, HighRiskPinpointerTrackDnaMessage>(OnDnaSubmitted);
    }

    private void OnUiOpenAttempt(Entity<HighRiskPinpointerComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (IsTraitor(args.User))
            return;

        args.Cancel();
    }

    private void OnTargetSelected(Entity<HighRiskPinpointerComponent> ent, ref HighRiskPinpointerSelectTargetMessage args)
    {
        if (!IsTraitor(args.Actor) || !HighRiskTargets.TryGetValue(args.Target, out var target))
            return;

        var matches = FindEntitiesByPrototype(target.Prototypes);
        if (matches.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("high-risk-pinpointer-target-not-found"), ent, args.Actor);
            return;
        }

        if (TrackTargets(ent, matches))
            _popup.PopupEntity(Loc.GetString("high-risk-pinpointer-search-started"), ent, args.Actor);
    }

    private void OnDnaSubmitted(Entity<HighRiskPinpointerComponent> ent, ref HighRiskPinpointerTrackDnaMessage args)
    {
        if (!IsTraitor(args.Actor) || string.IsNullOrWhiteSpace(args.Dna))
            return;

        var matches = FindEntitiesByDna(args.Dna);
        if (matches.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("high-risk-pinpointer-dna-not-found"), ent, args.Actor);
            return;
        }

        if (TrackTargets(ent, matches))
            _popup.PopupEntity(Loc.GetString("high-risk-pinpointer-search-started"), ent, args.Actor);
    }

    // The UI gate is also repeated for messages because client BUI messages are never trusted by the server.
    private bool IsTraitor(EntityUid user)
    {
        return _mind.TryGetMind(user, out var mindId, out _) && _roles.MindHasRole<TraitorRoleComponent>(mindId);
    }

    private bool TrackTargets(Entity<HighRiskPinpointerComponent> ent, List<EntityUid> targets)
    {
        if (!TryComp<PinpointerComponent>(ent, out var pinpointer))
            return false;

        _pinpointer.SetTargets(ent.Owner, targets, pinpointer);

        if (!pinpointer.IsActive)
            _pinpointer.TogglePinpointer(ent.Owner, pinpointer);

        return true;
    }

    private List<EntityUid> FindEntitiesByDna(string dna)
    {
        var matches = new List<EntityUid>();
        var query = EntityQueryEnumerator<DnaComponent>();

        while (query.MoveNext(out var uid, out var targetDna))
        {
            if (targetDna.DNA == dna)
                matches.Add(uid);
        }

        return matches;
    }
}
