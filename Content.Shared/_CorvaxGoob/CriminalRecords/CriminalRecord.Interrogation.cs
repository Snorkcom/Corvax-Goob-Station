// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.CriminalRecords;

public sealed partial record CriminalRecord
{
    public static readonly TimeSpan InterrogationDuration = TimeSpan.FromMinutes(10);

    /// <summary>
    /// The round time at which the interrogation becomes overdue.
    /// </summary>
    [DataField]
    public TimeSpan? InterrogationEndTime;
}
