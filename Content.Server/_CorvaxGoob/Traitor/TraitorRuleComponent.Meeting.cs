// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Stores the meeting hint generated for this traitor rule.
/// </summary>
public sealed partial class TraitorRuleComponent
{
    /// <summary>
    /// Set once when the first traitor receives their briefing, then reused for every traitor assigned by this rule.
    /// </summary>
    public TraitorMeetingHint? MeetingHint;
}

/// <summary>
/// Immutable time window and station area used in traitor meeting briefings.
/// </summary>
public readonly record struct TraitorMeetingHint(int StartMinute, int EndMinute, string Location);
