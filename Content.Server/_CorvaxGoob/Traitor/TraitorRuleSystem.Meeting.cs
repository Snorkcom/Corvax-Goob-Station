// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Systems;
using Content.Shared.Pinpointer;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Generates the meeting hint shared by traitors assigned by the same traitor rule.
/// </summary>
public sealed partial class TraitorRuleSystem
{
    [Dependency] private StationSystem _station = default!;

    private const int MinimumMeetingMinute = 10;
    private const int MaximumMeetingMinute = 50;
    private const int MeetingWindowRadius = 3;

    /// <summary>
    /// Returns chat and character-menu versions of the rule's shared meeting hint.
    /// </summary>
    private (string Chat, string Character) GetOrCreateMeetingBriefings(Entity<TraitorRuleComponent> rule)
    {
        if (rule.Comp.MeetingHint is not { } meeting)
        {
            meeting = CreateMeetingHint();
            rule.Comp.MeetingHint = meeting;
        }

        return (
            FormatMeetingBriefing("traitor-meeting-briefing", meeting),
            FormatMeetingBriefing("traitor-meeting-briefing-character", meeting));
    }

    private string FormatMeetingBriefing(string localizationKey, TraitorMeetingHint meeting)
    {
        return Loc.GetString(localizationKey,
            ("start", meeting.StartMinute),
            ("end", meeting.EndMinute),
            ("location", meeting.Location));
    }

    private TraitorMeetingHint CreateMeetingHint()
    {
        var middleMinute = _random.Next(MinimumMeetingMinute, MaximumMeetingMinute + 1);
        var startMinute = Math.Clamp(
            middleMinute - MeetingWindowRadius,
            MinimumMeetingMinute,
            MaximumMeetingMinute);
        var endMinute = Math.Clamp(
            middleMinute + MeetingWindowRadius,
            MinimumMeetingMinute,
            MaximumMeetingMinute);

        return new TraitorMeetingHint(startMinute, endMinute, PickMeetingLocation());
    }

    private string PickMeetingLocation()
    {
        var locations = new List<string>();
        var query = EntityQueryEnumerator<ConfigurableNavMapBeaconComponent, NavMapBeaconComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var beacon, out var xform))
        {
            // Station-owned nav beacons keep the hint useful and avoid off-station landmarks.
            if (!beacon.Enabled ||
                xform.GridUid == null ||
                _station.GetOwningStation(xform.GridUid.Value) == null ||
                !IsAllowedMeetingBeacon(uid, beacon))
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
