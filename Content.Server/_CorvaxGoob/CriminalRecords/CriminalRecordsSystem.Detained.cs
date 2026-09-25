// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.CriminalRecords;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Robust.Shared.Timing;

namespace Content.Server.CriminalRecords.Systems;

public sealed partial class CriminalRecordsSystem
{
    /// <summary>
    /// Starts a new detention countdown or clears the current one when another status is selected.
    /// </summary>
    private void UpdateDetainedTimer(StationRecordKey key, CriminalRecord record, SecurityStatus status, int? duration)
    {
        if (status != SecurityStatus.Detained || duration == null || duration <= 0)
        {
            record.DetainedEndTime = null;
            return;
        }

        var detainedDuration = TimeSpan.FromMinutes(duration.Value);
        var endTime = _ticker.RoundDuration() + detainedDuration;
        record.DetainedEndTime = endTime;

        Timer.Spawn(detainedDuration, () => AnnounceCompletedDetention(key, endTime));
    }

    /// <summary>
    /// Sends the security-channel warning if this is still the active detention countdown.
    /// </summary>
    private void AnnounceCompletedDetention(StationRecordKey key, TimeSpan endTime)
    {
        if (!_records.TryGetRecord<CriminalRecord>(key, out var record) ||
            record.Status != SecurityStatus.Detained ||
            record.DetainedEndTime != endTime ||
            !_records.TryGetRecord<GeneralStationRecord>(key, out var generalRecord))
        {
            return;
        }

        if (!TryGetCriminalRecordsConsole(key.OriginStation, out var console, out var consoleComponent))
            return;

        var message = Loc.GetString("criminal-records-console-detained-expired",
            ("name", generalRecord.Name),
            ("job", generalRecord.JobTitle));
        _radio.SendRadioMessage(console, message, consoleComponent.SecurityChannel, console);
    }
}
