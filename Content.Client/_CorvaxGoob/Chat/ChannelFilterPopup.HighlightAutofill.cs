// SPDX-License-Identifier: AGPL-3.0-or-later

using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

/// <summary>
/// CorvaxGoob-only UI glue for the small "+" button near the highlights label.
/// The popup raises a request event only; the chat controller decides which text to generate.
/// </summary>
public sealed partial class ChannelFilterPopup
{
    /// <summary>
    /// Raised when the user presses the "+" button to prepare highlight text for the edit field.
    /// This is intentionally separate from <c>OnNewHighlights</c>, which applies and saves the field.
    /// </summary>
    public event Action? OnCorvaxGoobFillHighlightsRequested;

    /// <summary>
    /// Connects the XAML button to the CorvaxGoob request event after typed names are loaded.
    /// </summary>
    private partial void InitializeCorvaxGoobHighlightAutofill()
    {
        HighlightAutoFillButton.OnPressed += CorvaxGoobHighlightAutoFillPressed;
    }

    /// <summary>
    /// Converts the button press into a feature-specific request without touching saved highlights.
    /// </summary>
    private void CorvaxGoobHighlightAutoFillPressed(ButtonEventArgs args)
    {
        OnCorvaxGoobFillHighlightsRequested?.Invoke();
    }
}
