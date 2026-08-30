// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Pinpointer;
using Robust.Shared.Random;

namespace Content.Server.Traitor.Cooperation;

/// <summary>
/// Generates and stores the shared meeting hint shown in every traitor briefing for the round.
/// </summary>
public sealed partial class TraitorUplinkCooperationSystem
{
    public string GetOrCreateMeetingBriefing(EntityUid ruleUid)
    {
        // Store the meeting hint on the rule so every traitor in the round receives the same window and area.
        var comp = EnsureComp<TraitorMeetingComponent>(ruleUid);
        if (comp.StartMinute == 0)
        {
            var middleMinute = _random.Next(10, 31);
            comp.StartMinute = Math.Clamp(middleMinute - 3, 10, 30);
            comp.EndMinute = Math.Clamp(middleMinute + 3, 10, 30);
            comp.Location = PickMeetingLocation();
            Dirty(ruleUid, comp);
        }

        return Loc.GetString("traitor-cooperation-meeting-briefing",
            ("start", comp.StartMinute),
            ("end", comp.EndMinute),
            ("location", comp.Location));
    }

    private string PickMeetingLocation()
    {
        var locations = new List<string>();
        var query = EntityQueryEnumerator<ConfigurableNavMapBeaconComponent, NavMapBeaconComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var beacon, out var xform))
        {
            // Station-owned nav beacons keep the hint useful and avoid off-station landmarks.
            if (!beacon.Enabled || xform.GridUid == null || _station.GetOwningStation(xform.GridUid.Value) == null)
                continue;

            var text = beacon.Text;
            if (string.IsNullOrWhiteSpace(text) && beacon.DefaultText != null)
                text = Loc.GetString(beacon.DefaultText);

            if (!string.IsNullOrWhiteSpace(text))
                locations.Add(text);
        }

        return locations.Count == 0
            ? Loc.GetString("traitor-cooperation-meeting-location-unknown")
            : _random.Pick(locations);
    }
}
