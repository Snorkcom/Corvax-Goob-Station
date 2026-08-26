// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Shared._CorvaxGoob.Chat;

/// <summary>
/// Per-job word list used by the chat filter popup's manual highlight-fill button.
/// The prototype ID is the job ID plus a locale suffix, for example <c>StationEngineer-ru-RU</c>.
/// </summary>
[Prototype("chatHighlightAutofill")]
public sealed partial class ChatHighlightAutofillPrototype : IPrototype
{
    /// <summary>
    /// Job-based identifier with locale suffix. Separate locale files are loaded at the same time.
    /// </summary>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Localized job title and optional aliases inserted into the highlights edit field, one quoted line per value.
    /// </summary>
    [DataField]
    public List<string> Words { get; private set; } = new();

    /// <summary>
    /// Optional aliases inserted into the highlights edit field exactly as written, without adding double quotes.
    /// Use this when the chat filter syntax needs a raw value instead of a quoted word.
    /// </summary>
    [DataField("rawWords")]
    public List<string> RawWords { get; private set; } = new();
}
