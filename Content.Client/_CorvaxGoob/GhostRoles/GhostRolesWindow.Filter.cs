// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Ghost.Controls.Roles;

public sealed partial class GhostRolesWindow
{
    private const string FilterIcon = "/Textures/Interface/Nano/filter.svg.96dpi.png";
    private const string NotificationsIcon = "/Textures/_DV/Interface/VerbIcons/bell.svg.png";
    private const string NotificationsMutedIcon = "/Textures/_DV/Interface/VerbIcons/bell_muted.png";

    private TextureButton _importantFilterButton = default!;
    private TextureButton _notificationsButton = default!;

    public event Action<bool>? OnImportantFilterToggled;
    public event Action<bool>? OnNotificationsToggled;

    public bool ImportantFilterEnabled => _importantFilterButton.Pressed;

    private void InitializeCorvaxGoobControls()
    {
        _importantFilterButton = CreateHeaderButton(
            FilterIcon,
            Loc.GetString("ghost-roles-window-important-filter-tooltip"),
            false);

        _notificationsButton = CreateHeaderButton(
            NotificationsIcon,
            Loc.GetString("ghost-roles-window-notifications-tooltip"),
            true);

        var header = (BoxContainer) CloseButton.Parent!;
        header.AddChild(_importantFilterButton);
        _importantFilterButton.SetPositionInParent(CloseButton.GetPositionInParent());
        header.AddChild(_notificationsButton);
        _notificationsButton.SetPositionInParent(CloseButton.GetPositionInParent());

        _importantFilterButton.OnPressed += _ =>
            OnImportantFilterToggled?.Invoke(_importantFilterButton.Pressed);
        _notificationsButton.OnPressed += _ =>
        {
            UpdateNotificationsIcon();
            OnNotificationsToggled?.Invoke(_notificationsButton.Pressed);
        };
    }

    public void SetNotificationsEnabled(bool enabled)
    {
        _notificationsButton.Pressed = enabled;
        UpdateNotificationsIcon();
    }

    public void SetNoRolesMessage()
    {
        NoRolesMessage.Text = Loc.GetString(ImportantFilterEnabled
            ? "ghost-roles-window-no-important-roles"
            : "ghost-roles-window-no-roles-available-label");
    }

    private void UpdateNotificationsIcon()
    {
        _notificationsButton.TexturePath = _notificationsButton.Pressed
            ? NotificationsIcon
            : NotificationsMutedIcon;
    }

    private TextureButton CreateHeaderButton(
        string icon,
        string tooltip,
        bool pressed)
    {
        CloseButton.Measure(Vector2Helpers.Infinity);
        var buttonSize = CloseButton.DesiredSize;

        var button = new TextureButton
        {
            ToggleMode = true,
            Pressed = pressed,
            TexturePath = icon,
            MinSize = buttonSize,
            VerticalAlignment = VAlignment.Center,
            ToolTip = tooltip,
            TooltipDelay = 0.25f,
            StyleClasses = { DefaultWindow.StyleClassWindowCloseButton },
        };

        if (button.TextureNormal is { } texture)
            button.Scale = buttonSize / texture.Size;

        return button;
    }
}
