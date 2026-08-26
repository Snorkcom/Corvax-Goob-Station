// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Client.UserInterface.Systems.Chat.Widgets;

/// <summary>
/// CorvaxGoob-only bridge between the filter popup button and <see cref="ChatUIController"/>.
/// Keeping this in a partial avoids mixing manual highlight-fill behavior into the main chat widget.
/// </summary>
public partial class ChatBox
{
    /// <summary>
    /// Subscribes this chat box to the popup's manual highlight-fill request.
    /// </summary>
    private partial void InitializeCorvaxGoobHighlightAutofill()
    {
        ChatInput.FilterButton.Popup.OnCorvaxGoobFillHighlightsRequested += CorvaxGoobFillHighlightsRequested;
    }

    /// <summary>
    /// Mirrors the subscription cleanup in the main widget's dispose path.
    /// </summary>
    private partial void DisposeCorvaxGoobHighlightAutofill()
    {
        ChatInput.FilterButton.Popup.OnCorvaxGoobFillHighlightsRequested -= CorvaxGoobFillHighlightsRequested;
    }

    /// <summary>
    /// Clears only the visible edit field, then asks the controller to fill it from character data.
    /// The normal "Send" button remains the only path that saves <c>chat.highlights</c>.
    /// </summary>
    private void CorvaxGoobFillHighlightsRequested()
    {
        ChatInput.FilterButton.Popup.UpdateHighlights(string.Empty);
        _controller.CorvaxGoobPrepareCharacterHighlights(ChatInput.FilterButton.Popup.UpdateHighlights);
    }
}
