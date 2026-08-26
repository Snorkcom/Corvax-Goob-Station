// SPDX-License-Identifier: AGPL-3.0-or-later

using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

/// <summary>
/// Isolated UI glue for the small "+" button near the highlights label.
/// The popup raises a request event only; the chat controller decides which text to generate.
/// </summary>
public sealed partial class ChannelFilterPopup
{
    /// <summary>
    /// Fills the highlights edit field when the "+" button is pressed, without applying or saving it.
    /// </summary>
    public event Action? OnFillHighlightsRequested;

    /// <summary>
    /// Wires the "+" button to the autofill event after XAML names are loaded.
    /// </summary>
    private partial void InitializeHighlightAutofill()
    {
        HighlightAutoFillButton.OnPressed += HighlightAutoFillPressed;
    }

    /// <summary>
    /// Converts the button press into a feature-specific request without touching saved highlights.
    /// </summary>
    private void HighlightAutoFillPressed(ButtonEventArgs args)
    {
        OnFillHighlightsRequested?.Invoke();
    }
}
