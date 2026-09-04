// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Implants.Components;
using Content.Server.Pinpointer;
using Content.Server.Radio.EntitySystems;
using Content.Shared._EinsteinEngines.Language;
using Content.Shared._EinsteinEngines.Language.Systems;
using Content.Shared.Implants;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Implants;

/// <summary>
/// Handles the syndicate distress implant action by broadcasting the user's real name
/// and current position over the syndicate radio channel.
/// </summary>
public sealed partial class SyndicateDistressImplantSystem : EntitySystem
{
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private RadioSystem _radio = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private LanguagePrototype _broadcastLanguage = default!;

    public override void Initialize()
    {
        base.Initialize();

        _broadcastLanguage = _prototype.Index<LanguagePrototype>(SharedLanguageSystem.FallbackLanguagePrototype);

        SubscribeLocalEvent<SyndicateDistressImplantComponent, SyndicateDistressImplantActionEvent>(OnDistressAction);
    }

    private void OnDistressAction(Entity<SyndicateDistressImplantComponent> ent, ref SyndicateDistressImplantActionEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;

        // The implant can be used while alive or in critical condition, but not after death.
        if (_mobState.IsDead(user))
            return;

        var name = GetBroadcastName(user);
        var position = GetPositionText(user);
        var message = Loc.GetString(ent.Comp.DistressMessage, ("name", name), ("position", position));

        if (_mobState.IsCritical(user))
            message = $"{message} {Loc.GetString("syndicate-distress-implant-critical-state")}";

        // Use common language so the automated signal is not blocked by the user's current language.
        _radio.SendRadioMessage(user, message, ent.Comp.RadioChannel, user, _broadcastLanguage);
        args.Handled = true;
    }

    private string GetBroadcastName(EntityUid user)
    {
        if (!_mind.TryGetMind(user, out var mindId, out var mind) ||
            !_role.MindHasRole<TraitorRoleComponent>(mindId))
            return Loc.GetString("syndicate-distress-implant-name-unknown");

        // Traitor broadcasts expose the real name from the mind instead of the current body name.
        if (!string.IsNullOrWhiteSpace(mind.CharacterName))
            return mind.CharacterName;

        return Name(user);
    }

    private string GetPositionText(EntityUid user)
    {
        var coordinates = _transform.GetMapCoordinates(user);
        var coordinateText = Loc.GetString("syndicate-distress-implant-coordinates",
            ("x", (int) MathF.Round(coordinates.Position.X)),
            ("y", (int) MathF.Round(coordinates.Position.Y)));

        if (Transform(user).GridUid == null)
            return Loc.GetString("syndicate-distress-implant-location-space", ("coordinates", coordinateText));

        var beaconText = FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString(coordinates));
        return Loc.GetString("syndicate-distress-implant-location",
            ("coordinates", coordinateText),
            ("beacon", beaconText));
    }
}
