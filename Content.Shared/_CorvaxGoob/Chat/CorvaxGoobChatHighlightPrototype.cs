// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Shared._CorvaxGoob.Chat;

/// <summary>
/// CorvaxGoob per-job word list used by the chat filter popup's manual highlight-fill button.
/// The prototype ID is the job ID plus a locale suffix, for example <c>StationEngineer-ru-RU</c>.
/// </summary>
[Prototype("corvaxGoobChatHighlight")]
public sealed partial class CorvaxGoobChatHighlightPrototype : IPrototype
{
    /// <summary>
    /// Job-based identifier with locale suffix. Separate locale files are loaded at the same time.
    /// </summary>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Words that will be inserted into the highlights edit field, one quoted line per value.
    /// </summary>
    [DataField]
    public List<string> Words { get; private set; } = new();
}
