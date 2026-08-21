// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Ghost.Roles.Components;
using Content.Shared._CorvaxGoob.GhostBar;
using Content.Shared.Ghost;
using Content.Shared.Ghost.Roles;
using Content.Shared.Ghost.Roles.Components;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Ghost.Roles;

/// <summary>
/// Resolves explicitly maintained ghost-role metadata and handles availability notifications.
/// </summary>
public sealed class GhostRoleClassificationSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private static readonly SoundSpecifier ImportantGhostRoleSound = new SoundPathSpecifier(
        "/Audio/_Goobstation/Wizard/swap.ogg",
        AudioParams.Default.WithVolume(-4f));

    public GhostRoleClassificationPrototype? GetClassification(Entity<GhostRoleComponent> role)
    {
        var prototypeId = MetaData(role.Owner).EntityPrototype?.ID;
        if (prototypeId is not null &&
            _prototype.TryIndex<GhostRoleClassificationPrototype>(prototypeId, out var classification))
        {
            return classification;
        }

        if (TryComp<GhostRoleMobSpawnerComponent>(role.Owner, out var spawner) &&
            spawner.Prototype is { } spawnPrototype &&
            _prototype.TryIndex<GhostRoleClassificationPrototype>(spawnPrototype.Id, out classification))
        {
            return classification;
        }

        return null;
    }

    public void NotifyRoleAvailable(Entity<GhostRoleComponent> role)
    {
        if (GetClassification(role)?.NotifyOnAvailable != true)
            return;

        foreach (var session in _playerManager.Sessions)
        {
            if (CanReceiveNotification(session))
                _audio.PlayGlobal(ImportantGhostRoleSound, session);
        }
    }

    private bool CanReceiveNotification(ICommonSession session)
    {
        if (session.AttachedEntity is not { Valid: true } attached)
            return false;

        return TryComp<GhostComponent>(attached, out var ghost) && ghost.CanTakeGhostRoles
            || HasComp<GhostBarPlayerComponent>(attached);
    }
}
