// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Client.UserInterface.Systems.Chat.Widgets;

/// <summary>
/// Isolated bridge between the filter popup button and <see cref="ChatUIController"/>.
/// Keeping this in a partial avoids mixing manual highlight-fill behavior into the main chat widget.
/// </summary>
public partial class ChatBox
{
    /// <summary>
    /// Subscribes this chat box to the popup's manual highlight-fill request.
    /// </summary>
    private partial void InitializeHighlightAutofill()
    {
        ChatInput.FilterButton.Popup.OnFillHighlightsRequested += FillHighlightsRequested;
    }

    /// <summary>
    /// Mirrors the subscription cleanup in the main widget's dispose path.
    /// </summary>
    private partial void DisposeHighlightAutofill()
    {
        ChatInput.FilterButton.Popup.OnFillHighlightsRequested -= FillHighlightsRequested;
    }

    /// <summary>
    /// Replaces only the visible edit field with locally available character and original-job highlights.
    /// The normal "Send" button remains the only path that saves <c>chat.highlights</c>.
    /// </summary>
    private void FillHighlightsRequested()
    {
        ChatInput.FilterButton.Popup.UpdateHighlights(_controller.BuildCharacterHighlights());
    }
}
