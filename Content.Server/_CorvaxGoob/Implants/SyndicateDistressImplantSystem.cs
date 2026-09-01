// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Implants.Components;
using Content.Server.Pinpointer;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Implants;
using Content.Shared.Mind;
using Robust.Shared.Utility;

namespace Content.Server.Implants;

/// <summary>
/// Handles the syndicate distress implant action by broadcasting the user's registered name
/// and current position over the syndicate radio channel.
/// </summary>
public sealed partial class SyndicateDistressImplantSystem : EntitySystem
{
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private RadioSystem _radio = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SyndicateDistressImplantComponent, SyndicateDistressImplantActionEvent>(OnDistressAction);
    }

    private void OnDistressAction(Entity<SyndicateDistressImplantComponent> ent, ref SyndicateDistressImplantActionEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;
        var name = GetRegisteredName(user);
        var position = GetPositionText(user);
        var message = Loc.GetString(ent.Comp.DistressMessage, ("name", name), ("position", position));

        _radio.SendRadioMessage(user, message, ent.Comp.RadioChannel, user);
        args.Handled = true;
    }

    private string GetRegisteredName(EntityUid user)
    {
        // Mind.CharacterName is the character's registered identity and is not affected by ordinary body renaming.
        if (_mind.TryGetMind(user, out _, out var mind) && !string.IsNullOrWhiteSpace(mind.CharacterName))
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
