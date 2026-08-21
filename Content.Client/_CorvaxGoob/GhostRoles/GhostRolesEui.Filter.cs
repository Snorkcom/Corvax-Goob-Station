// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._CorvaxGoob.GhostRoles;
using Content.Shared.Ghost.Roles;

namespace Content.Client.UserInterface.Systems.Ghost.Controls.Roles;

public sealed partial class GhostRolesEui
{
    private GhostRolesEuiState? _lastGhostRolesState;

    private void InitializeCorvaxGoobControls()
    {
        _window.OnImportantFilterToggled += _ =>
        {
            if (_lastGhostRolesState is { } state)
                HandleState(state);
        };
        _window.OnNotificationsToggled += enabled =>
            SendMessage(new SetGhostRoleNotificationsMessage(enabled));
    }

    private IEnumerable<GhostRoleInfo> FilterCorvaxGoobRoles(GhostRolesEuiState state)
    {
        _lastGhostRolesState = state;
        _window.SetNotificationsEnabled(state.NotificationsEnabled);

        if (!_window.ImportantFilterEnabled)
            return state.GhostRoles;

        return state.GhostRoles
            .Where(role => role.Priority > GhostRoleClassificationPrototype.UnclassifiedPriority)
            .OrderByDescending(role => role.Priority)
            .ThenBy(role => role.Name);
    }
}
