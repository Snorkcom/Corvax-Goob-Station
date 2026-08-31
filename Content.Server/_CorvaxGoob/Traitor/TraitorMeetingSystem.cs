// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Station.Systems;
using Content.Shared.Pinpointer;
using Robust.Shared.Random;

namespace Content.Server.Traitor.Meeting;

/// <summary>
/// Generates and stores the shared meeting hint shown in every traitor briefing for the round.
/// </summary>
public sealed class TraitorMeetingSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private StationSystem _station = default!;

    private const int MinimumMeetingMinute = 10;
    private const int MaximumMeetingMinute = 30;
    private const int MeetingWindowRadius = 3;

    /// <summary>
    /// Returns chat and character-menu versions of the shared meeting hint for this traitor rule.
    /// </summary>
    public (string Chat, string Character) GetOrCreateMeetingBriefings(EntityUid ruleUid)
    {
        var comp = GetOrCreateMeetingHint(ruleUid);
        return (
            FormatMeetingBriefing("traitor-meeting-briefing", comp),
            FormatMeetingBriefing("traitor-meeting-briefing-character", comp));
    }

    private string FormatMeetingBriefing(string localizationKey, TraitorMeetingComponent meeting)
    {
        return Loc.GetString(localizationKey,
            ("start", meeting.StartMinute),
            ("end", meeting.EndMinute),
            ("location", meeting.Location));
    }

    private TraitorMeetingComponent GetOrCreateMeetingHint(EntityUid ruleUid)
    {
        // Store the meeting hint on the rule so every traitor in the round receives the same window and area.
        var comp = EnsureComp<TraitorMeetingComponent>(ruleUid);
        if (!comp.Initialized)
        {
            var middleMinute = _random.Next(MinimumMeetingMinute, MaximumMeetingMinute + 1);
            comp.StartMinute = Math.Clamp(
                middleMinute - MeetingWindowRadius,
                MinimumMeetingMinute,
                MaximumMeetingMinute);
            comp.EndMinute = Math.Clamp(
                middleMinute + MeetingWindowRadius,
                MinimumMeetingMinute,
                MaximumMeetingMinute);
            comp.Location = PickMeetingLocation();
            comp.Initialized = true;
        }

        return comp;
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
            ? Loc.GetString("traitor-meeting-location-unknown")
            : _random.Pick(locations);
    }
}
