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

    // If the station has no criminal records console when the timer expires, keep trying at this interval.
    private static readonly TimeSpan ConsoleRetryDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Starts a new interrogation countdown or clears the current one when another status is selected.
    /// </summary>
    private void UpdateInterrogationTimer(StationRecordKey key, CriminalRecord record, SecurityStatus status)
    {
        if (status != SecurityStatus.Interrogation)
        {
            // Scheduled callbacks cannot be cancelled directly, so clearing the deadline invalidates them.
            record.InterrogationEndTime = null;
            return;
        }

        // Store the deadline in the record so SecHUD examination can calculate the live countdown.
        var endTime = _ticker.RoundDuration() + CriminalRecord.InterrogationDuration;
        record.InterrogationEndTime = endTime;

        // Capture this exact deadline. If the status is reset later, the old callback will fail validation below.
        Timer.Spawn(CriminalRecord.InterrogationDuration, () => AnnounceOverdueInterrogation(key, endTime));
    }

    /// <summary>
    /// Sends the security-channel warning if this is still the active interrogation countdown.
    /// </summary>
    private void AnnounceOverdueInterrogation(StationRecordKey key, TimeSpan endTime)
    {
        // The record may have been deleted, released, or placed under a newer interrogation timer.
        if (!_records.TryGetRecord<CriminalRecord>(key, out var record) ||
            record.Status != SecurityStatus.Interrogation ||
            record.InterrogationEndTime != endTime ||
            !_records.TryGetRecord<GeneralStationRecord>(key, out var generalRecord))
        {
            return;
        }

        if (!TryGetCriminalRecordsConsole(key.OriginStation, out var console, out var consoleComponent))
        {
            // A console may be rebuilt later, so retry without scanning every record on every server tick.
            Timer.Spawn(ConsoleRetryDelay, () => AnnounceOverdueInterrogation(key, endTime));
            return;
        }

        var message = Loc.GetString("criminal-records-console-interrogation-overdue",
            ("name", generalRecord.Name),
            ("job", generalRecord.JobTitle),
            ("minutes", (int) CriminalRecord.InterrogationDuration.TotalMinutes));
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
