// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;
// CorvaxGoob Start - ghost-role-filter-and-notification
using Content.Server._CorvaxGoob.GhostRoles;
using Content.Shared._CorvaxGoob.GhostRoles;
// CorvaxGoob End

namespace Content.Server.Ghost.Roles.UI
{
    public sealed class GhostRolesEui : BaseEui
    {
        private readonly GhostRoleSystem _ghostRoleSystem;
        private readonly GhostRoleClassificationSystem _classificationSystem; // CorvaxGoob - ghost-role-filter-and-notification

        public GhostRolesEui()
        {
            _ghostRoleSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GhostRoleSystem>();
            _classificationSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GhostRoleClassificationSystem>(); // CorvaxGoob - ghost-role-filter-and-notification
        }

        public override GhostRolesEuiState GetNewState()
        {
            return new(
                _ghostRoleSystem.GetGhostRolesInfo(Player),
                _classificationSystem.NotificationsEnabled(Player)); // CorvaxGoob Edit - ghost-role-filter-and-notification
        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);

            switch (msg)
            {
                case RequestGhostRoleMessage req:
                    _ghostRoleSystem.Request(Player, req.Identifier);
                    break;
                case FollowGhostRoleMessage req:
                    _ghostRoleSystem.Follow(Player, req.Identifier);
                    break;
                case LeaveGhostRoleRaffleMessage req:
                    _ghostRoleSystem.LeaveRaffle(Player, req.Identifier);
                    break;
                // CorvaxGoob Start - ghost-role-filter-and-notification
                case SetGhostRoleNotificationsMessage req:
                    _classificationSystem.SetNotificationsEnabled(Player, req.Enabled);
                    StateDirty();
                    break;
                // CorvaxGoob End
            }
        }

        public override void Closed()
        {
            base.Closed();

            _ghostRoleSystem.CloseEui(Player);
        }
    }
}