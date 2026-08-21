// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Ghost.Roles.Components;
using Content.Shared._CorvaxGoob.GhostBar;
using Content.Shared._CorvaxGoob.GhostRoles;
using Content.Shared.Ghost;
using Content.Shared.Ghost.Roles.Components;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._CorvaxGoob.GhostRoles;

/// <summary>
/// Resolves explicitly maintained ghost-role metadata and sends optional availability sounds.
/// </summary>
public sealed class GhostRoleClassificationSystem : EntitySystem
{
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private static readonly SoundSpecifier ImportantRoleSound = new SoundPathSpecifier(
        "/Audio/_Goobstation/Wizard/swap.ogg",
        AudioParams.Default.WithVolume(-4f));

    private readonly HashSet<NetUserId> _notificationsDisabled = new();

    public override void Initialize()
    {
        base.Initialize();
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        _notificationsDisabled.Clear();
    }

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

    public bool NotificationsEnabled(ICommonSession session)
    {
        return !_notificationsDisabled.Contains(session.UserId);
    }

    public void SetNotificationsEnabled(ICommonSession session, bool enabled)
    {
        if (enabled)
            _notificationsDisabled.Remove(session.UserId);
        else
            _notificationsDisabled.Add(session.UserId);
    }

    public void NotifyRoleAvailable(Entity<GhostRoleComponent> role)
    {
        if (GetClassification(role)?.NotifyOnAvailable != true)
            return;

        foreach (var session in _playerManager.Sessions)
        {
            if (NotificationsEnabled(session) && CanReceiveNotification(session))
                _audio.PlayGlobal(ImportantRoleSound, session);
        }
    }

    private bool CanReceiveNotification(ICommonSession session)
    {
        if (session.AttachedEntity is not { Valid: true } attached)
            return false;

        return TryComp<GhostComponent>(attached, out var ghost) && ghost.CanTakeGhostRoles
            || HasComp<GhostBarPlayerComponent>(attached);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus == SessionStatus.Disconnected)
            _notificationsDisabled.Remove(args.Session.UserId);
    }
}
