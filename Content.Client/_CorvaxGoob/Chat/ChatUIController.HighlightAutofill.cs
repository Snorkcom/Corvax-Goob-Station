// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Access;
using Content.Shared._CorvaxGoob.Chat;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Roles;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;

namespace Content.Client.UserInterface.Systems.Chat;

/// <summary>
/// Manual highlight filler for the filter popup "+" button.
/// It prepares text for the edit field only and deliberately avoids writing to <c>chat.highlights</c>.
/// </summary>
public sealed partial class ChatUIController
{
    /// <summary>
    /// Builds replacement text from the local character name and the job on the equipped PDA's ID card.
    /// Missing name, PDA, ID card, job, or locale preset is skipped instead of producing a fallback value.
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

        if (_player.LocalEntity is { } localEntity &&
            TryGetEquippedPdaJobId(localEntity, out var jobId))
        {
            if (!TryGetJobHighlight(jobId, out var highlight))
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
    /// Resolves the canonical job ID from the localized title on the ID card inside the equipped PDA.
    /// A bare ID card, custom title, or missing PDA is intentionally ignored.
    /// </summary>
    private bool TryGetEquippedPdaJobId(
        EntityUid entity,
        [NotNullWhen(true)] out string? jobId)
    {
        jobId = null;

        if (!_ent.System<InventorySystem>().TryGetSlotEntity(entity, "id", out var idSlot) ||
            !_ent.TryGetComponent<PdaComponent>(idSlot.Value, out _) ||
            !_ent.System<IdCardSystem>().TryGetIdCard(idSlot.Value, out var idCard) ||
            string.IsNullOrWhiteSpace(idCard.Comp.LocalizedJobTitle))
        {
            return false;
        }

        foreach (var job in _prototypeManager.EnumeratePrototypes<JobPrototype>())
        {
            if (job.LocalizedName != idCard.Comp.LocalizedJobTitle)
                continue;

            jobId = job.ID;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Finds the editable, culture-specific word list attached to the resolved job prototype ID.
    /// The selected prototype may contain quoted <c>words</c> and unquoted <c>rawWords</c>.
    /// </summary>
    private bool TryGetJobHighlight(
        string jobId,
        [NotNullWhen(true)] out ChatHighlightAutofillPrototype? highlight)
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
