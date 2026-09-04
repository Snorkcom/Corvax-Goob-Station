// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Pinpointer;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Restricts traitor meeting hints to a curated set of standard station nav-map beacons.
/// </summary>
public sealed partial class TraitorRuleSystem
{
    // Keep both identifiers together so prototype and localization checks cannot get out of sync.
    private static readonly (string PrototypeId, string DefaultTextId)[] AllowedMeetingBeacons =
    {
        ("DefaultStationBeaconCommand", "station-beacon-command"),
        ("DefaultStationBeaconBridge", "station-beacon-bridge"),
        ("DefaultStationBeaconVault", "station-beacon-vault"),
        ("DefaultStationBeaconGateway", "station-beacon-gateway"),
        ("DefaultStationBeaconHOPOffice", "station-beacon-hop"),
        ("DefaultStationBeaconSecurity", "station-beacon-security"),
        ("DefaultStationBeaconBrig", "station-beacon-brig"),
        ("DefaultStationBeaconDetectiveRoom", "station-beacon-detective"),
        ("DefaultStationBeaconCourtroom", "station-beacon-courtroom"),
        ("DefaultStationBeaconLawOffice", "station-beacon-law"),
        ("DefaultStationBeaconMedical", "station-beacon-medical"),
        ("DefaultStationBeaconMedbay", "station-beacon-medbay"),
        ("DefaultStationBeaconChemistry", "station-beacon-chemistry"),
        ("DefaultStationBeaconCryonics", "station-beacon-cryonics"),
        ("DefaultStationBeaconMorgue", "station-beacon-morgue"),
        ("DefaultStationBeaconSurgery", "station-beacon-surgery"),
        ("DefaultStationBeaconPsychology", "station-beacon-psychology"),
        ("DefaultStationBeaconClinic", "station-beacon-clinic"),
        ("DefaultStationBeaconParamedic", "station-beacon-paramedic"),
        ("DefaultStationBeaconScience", "station-beacon-science"),
        ("DefaultStationBeaconRND", "station-beacon-research-and-development"),
        ("DefaultStationBeaconSupply", "station-beacon-supply"),
        ("DefaultStationBeaconCargoReception", "station-beacon-cargo"),
        ("DefaultStationBeaconCargoBay", "station-beacon-cargo-bay"),
        ("DefaultStationBeaconEngineering", "station-beacon-engineering"),
        ("DefaultStationBeaconGravGen", "station-beacon-gravgen"),
        ("DefaultStationBeaconAnchor", "station-beacon-anchor"),
        ("DefaultStationBeaconSingularity", "station-beacon-pa"),
        ("DefaultStationBeaconPowerBank", "station-beacon-smes"),
        ("DefaultStationBeaconTelecoms", "station-beacon-telecoms"),
        ("DefaultStationBeaconAtmospherics", "station-beacon-atmos"),
        ("DefaultStationBeaconTEG", "station-beacon-teg"),
        ("DefaultStationBeaconTechVault", "station-beacon-tech-vault"),
        ("DefaultStationBeaconShipyard", "station-beacon-shipyard"),
        ("DefaultStationBeaconSolarsN", "station-beacon-solars-N"),
        ("DefaultStationBeaconSolarsNE", "station-beacon-solars-NE"),
        ("DefaultStationBeaconSolarsE", "station-beacon-solars-E"),
        ("DefaultStationBeaconSolarsSE", "station-beacon-solars-SE"),
        ("DefaultStationBeaconSolarsS", "station-beacon-solars-S"),
        ("DefaultStationBeaconSolarsSW", "station-beacon-solars-SW"),
        ("DefaultStationBeaconSolarsW", "station-beacon-solars-W"),
        ("DefaultStationBeaconSolarsNW", "station-beacon-solars-NW"),
        ("DefaultStationBeaconService", "station-beacon-service"),
        ("DefaultStationBeaconKitchen", "station-beacon-kitchen"),
        ("DefaultStationBeaconBar", "station-beacon-bar"),
        ("DefaultStationBeaconBotany", "station-beacon-botany"),
        ("DefaultStationBeaconJanitorsCloset", "station-beacon-janitor"),
        ("DefaultStationBeaconChapel", "station-beacon-chapel"),
        ("DefaultStationBeaconLibrary", "station-beacon-library"),
        ("DefaultStationBeaconReporter", "station-beacon-reporter"),
        ("DefaultStationBeaconTheater", "station-beacon-theater"),
        ("DefaultStationBeaconDorms", "station-beacon-dorms"),
        ("DefaultStationBeaconToolRoom", "station-beacon-tools"),
        ("DefaultStationBeaconDisposals", "station-beacon-disposals"),
        ("DefaultStationBeaconCryosleep", "station-beacon-cryosleep"),
        ("DefaultStationBeaconVox", "station-beacon-vox"),
        ("DefaultStationBeaconAI", "station-beacon-ai"),
        ("DefaultStationBeaconAISatellite", "station-beacon-ai-sat"),
        ("DefaultStationBeaconArrivals", "station-beacon-arrivals"),
        ("DefaultStationBeaconEvac", "station-beacon-evac"),
        ("DefaultStationBeaconEVAStorage", "station-beacon-eva-storage"),
        ("DefaultStationBeaconEscapePodN", "station-beacon-escape-pod-N"),
        ("DefaultStationBeaconEscapePodNE", "station-beacon-escape-pod-NE"),
        ("DefaultStationBeaconEscapePodE", "station-beacon-escape-pod-E"),
        ("DefaultStationBeaconEscapePodSE", "station-beacon-escape-pod-SE"),
        ("DefaultStationBeaconEscapePodS", "station-beacon-escape-pod-S"),
        ("DefaultStationBeaconEscapePodSW", "station-beacon-escape-pod-SW"),
        ("DefaultStationBeaconEscapePodW", "station-beacon-escape-pod-W"),
        ("DefaultStationBeaconEscapePodNW", "station-beacon-escape-pod-NW"),
    };

    private bool IsAllowedMeetingBeacon(EntityUid uid, NavMapBeaconComponent beacon)
    {
        // Accept either the stable prototype ID or its default localization key.
        // The second check keeps map-specific child prototypes usable without relying on displayed text.
        var prototypeId = MetaData(uid).EntityPrototype?.ID;
        foreach (var allowed in AllowedMeetingBeacons)
        {
            if (prototypeId == allowed.PrototypeId || beacon.DefaultText == allowed.DefaultTextId)
                return true;
        }

        return false;
    }
}
