// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Radio.EntitySystems;
using Content.Shared.CriminalRecords;
using Content.Shared.CriminalRecords.Components;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Robust.Shared.Timing;

namespace Content.Server.CriminalRecords.Systems;

public sealed partial class CriminalRecordsSystem
{
    [Dependency] private RadioSystem _radio = default!;

    /// <summary>
    /// Starts a countdown for a timed status or clears the current deadline.
    /// </summary>
    private void UpdateStatusTimer(StationRecordKey key, CriminalRecord record, SecurityStatus status, int? duration)
    {
        TimeSpan? timerDuration = status switch
        {
            SecurityStatus.Interrogation => CriminalRecord.InterrogationDuration,
            SecurityStatus.Detained when duration is > 0 => TimeSpan.FromMinutes(duration.Value),
            _ => null,
        };

        record.StatusEndTime = null;
        if (timerDuration is not { } activeDuration)
            return;

        var endTime = _ticker.RoundDuration() + activeDuration;
        record.StatusEndTime = endTime;

        // Check that this is still the active timer before sending the warning.
        Timer.Spawn(activeDuration, () => AnnounceExpiredStatus(key, status, endTime));
    }

    /// <summary>
    /// Sends the security-channel warning if this is still the active status countdown.
    /// </summary>
    private void AnnounceExpiredStatus(StationRecordKey key, SecurityStatus status, TimeSpan endTime)
    {
        if (!_records.TryGetRecord<CriminalRecord>(key, out var record) ||
            record.Status != status ||
            record.StatusEndTime != endTime ||
            !_records.TryGetRecord<GeneralStationRecord>(key, out var generalRecord))
        {
            return;
        }

        if (!TryGetCriminalRecordsConsole(key.OriginStation, out var console, out var consoleComponent))
            return;

        var message = status switch
        {
            SecurityStatus.Interrogation => Loc.GetString("criminal-records-console-interrogation-overdue",
                ("name", generalRecord.Name),
                ("job", generalRecord.JobTitle),
                ("minutes", (int) CriminalRecord.InterrogationDuration.TotalMinutes)),
            SecurityStatus.Detained => Loc.GetString("criminal-records-console-detained-expired",
                ("name", generalRecord.Name),
                ("job", generalRecord.JobTitle)),
            _ => null,
        };

        if (message != null)
            _radio.SendRadioMessage(console, message, consoleComponent.SecurityChannel, console);
    }

    /// <summary>
    /// Finds a criminal records console on the same station to use as the radio-message source.
    /// </summary>
    private bool TryGetCriminalRecordsConsole(
        EntityUid station,
        out EntityUid console,
        out CriminalRecordsConsoleComponent consoleComponent)
    {
        var consoles = EntityQueryEnumerator<CriminalRecordsConsoleComponent>();
        while (consoles.MoveNext(out var uid, out var component))
        {
            if (_station.GetOwningStation(uid) != station)
                continue;

            console = uid;
            consoleComponent = component;
            return true;
        }

        console = EntityUid.Invalid;
        consoleComponent = default!;
        return false;
    }
}
