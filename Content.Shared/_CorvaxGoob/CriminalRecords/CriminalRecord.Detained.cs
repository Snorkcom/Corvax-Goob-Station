// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.CriminalRecords;

public sealed partial record CriminalRecord
{
    /// <summary>
    /// The round time at which the detention sentence expires.
    /// </summary>
    [DataField]
    public TimeSpan? DetainedEndTime;
}
