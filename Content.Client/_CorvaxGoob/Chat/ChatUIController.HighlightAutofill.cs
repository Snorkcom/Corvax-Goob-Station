// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Roles;
using Content.Shared._CorvaxGoob.Chat;
using Robust.Shared.GameObjects;

namespace Content.Client.UserInterface.Systems.Chat;

/// <summary>
/// Manual highlight filler for the filter popup "+" button.
/// It prepares text for the edit field only and deliberately avoids writing to <c>chat.highlights</c>.
/// </summary>
public sealed partial class ChatUIController
{
    /// <summary>
    /// Builds replacement text from the local character name and original mind job.
    /// Missing name, mind, job, or locale preset is skipped instead of producing a fallback value.
    /// </summary>
    public string BuildCharacterHighlights()
    {
        var lines = new List<string>();
        // RobustToolbox content sandbox forbids StringComparer, so duplicate keys are normalized explicitly.
        var seen = new HashSet<string>();

        if (_player.LocalEntity is { } entity &&
            _ent.TryGetComponent(entity, out MetaDataComponent? metadata))
        {
            // Each space- or hyphen-separated name part becomes its own quoted highlight.
            foreach (var word in metadata.EntityName.Split([' ', '-'],
                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                TryAddHighlightLine(lines, seen, word);
            }
        }

        if (_player.LocalUser is { } user &&
            _mindSystem != null &&
            _mindSystem.TryGetMind(user, out var mindId) &&
            _ent.System<JobSystem>().MindTryGetJobId(mindId, out var jobId) &&
            jobId is { } originalJobId)
        {
            if (!TryGetJobHighlight(originalJobId.Id, out var highlight))
                return string.Join("\n", lines);

            foreach (var word in highlight.Words)
            {
                TryAddHighlightLine(lines, seen, word);
            }

            foreach (var rawWord in highlight.RawWords)
            {
                TryAddHighlightLine(lines, seen, rawWord, true);
            }
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Finds the editable, culture-specific word list attached to the exact original job prototype ID.
    /// The selected prototype may contain quoted <c>words</c> and unquoted <c>rawWords</c>.
    /// </summary>
    private bool TryGetJobHighlight(string jobId, out ChatHighlightAutofillPrototype highlight)
    {
        var culture = _loc.DefaultCulture?.TwoLetterISOLanguageName == "ru"
            ? "ru-RU"
            : "en-US";

        return _prototypeManager.TryIndex($"{jobId}-{culture}", out highlight);
    }

    /// <summary>
    /// Adds a formatted line if the source value is not empty and has not already been generated.
    /// </summary>
    private static void TryAddHighlightLine(
        List<string> lines,
        HashSet<string> seen,
        string value,
        bool raw = false)
    {
        var line = FormatHighlightLine(value, raw);

        if (line == null || !seen.Add(line.ToLowerInvariant()))
            return;

        lines.Add(line);
    }

    /// <summary>
    /// Normalizes one highlight value into the user-editable format expected by chat highlights.
    /// Regular values become <c>"word"</c>; raw values are inserted without adding double quotes.
    /// </summary>
    private static string? FormatHighlightLine(string value, bool raw)
    {
        var trimmed = value.Trim();

        if (!raw && trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
            trimmed = trimmed[1..^1].Trim();

        if (trimmed.Length == 0)
            return null;

        return raw
            ? trimmed
            : $"\"{trimmed}\"";
    }
}
