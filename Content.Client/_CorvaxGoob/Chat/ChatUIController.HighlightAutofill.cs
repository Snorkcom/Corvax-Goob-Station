// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text;
using System.Text.RegularExpressions;
using Content.Shared._CorvaxGoob.Chat;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using static Content.Client.CharacterInfo.CharacterInfoSystem;

namespace Content.Client.UserInterface.Systems.Chat;

/// <summary>
/// CorvaxGoob manual highlight filler for the filter popup "+" button.
/// It prepares text for the edit field only and deliberately avoids writing to <c>chat.highlights</c>.
/// </summary>
public sealed partial class ChatUIController
{
    // Name words are split the way players expect to highlight them: spaces and hyphenated parts become separate lines.
    private static readonly Regex CorvaxGoobNameWordSeparators = new(@"[\s\-]+", RegexOptions.Compiled);

    // Set only while the popup "+" button is waiting for CharacterInfoSystem to answer.
    private Action<string>? _corvaxGoobPendingHighlightsReceiver;

    /// <summary>
    /// Requests current character data and sends the generated text to <paramref name="receiver"/>.
    /// The receiver is usually <c>ChannelFilterPopup.UpdateHighlights</c>, so this does not save settings.
    /// </summary>
    public void CorvaxGoobPrepareCharacterHighlights(Action<string> receiver)
    {
        if (_player.LocalEntity == null)
            return;

        _corvaxGoobPendingHighlightsReceiver = receiver;
        _characterInfo.RequestCharacterInfo();
    }

    /// <summary>
    /// Handles the character-info response for the manual popup button.
    /// Setting <paramref name="handled"/> prevents the old auto-fill branch from applying and saving this request.
    /// </summary>
    private partial void CorvaxGoobHandleCharacterUpdated(CharacterData data, ref bool handled)
    {
        if (_corvaxGoobPendingHighlightsReceiver == null)
            return;

        handled = true;
        var receiver = _corvaxGoobPendingHighlightsReceiver;
        _corvaxGoobPendingHighlightsReceiver = null;
        _charInfoIsAttach = false;
        receiver(BuildCorvaxGoobHighlightText(data));
    }

    /// <summary>
    /// Builds the replacement text for the edit field: quoted name parts first, then quoted job words.
    /// Duplicate generated lines are skipped before the text reaches the UI.
    /// </summary>
    private string BuildCorvaxGoobHighlightText(CharacterData data)
    {
        var lines = new List<string>();
        var seen = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

        foreach (var word in CorvaxGoobNameWordSeparators.Split(data.EntityName))
        {
            TryAddCorvaxGoobHighlightLine(lines, seen, word);
        }

        foreach (var word in GetCorvaxGoobJobHighlightWords(data.Job))
        {
            TryAddCorvaxGoobHighlightLine(lines, seen, word);
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Returns the culture-specific word list for the character's original job title.
    /// If the job cannot be resolved at all, the raw title is used as a last-resort visible fallback.
    /// </summary>
    private IEnumerable<string> GetCorvaxGoobJobHighlightWords(string jobName)
    {
        if (TryResolveCorvaxGoobJobPrototype(jobName, out var job))
        {
            foreach (var highlightId in GetCorvaxGoobJobHighlightIds(job.ID))
            {
                if (_prototypeManager.TryIndex<CorvaxGoobChatHighlightPrototype>(highlightId, out var highlight))
                    return highlight.Words;
            }
        }

        if (string.IsNullOrWhiteSpace(jobName))
            return Array.Empty<string>();

        return new[] { jobName };
    }

    /// <summary>
    /// Yields prototype IDs in preferred order. Both locale files are loaded together,
    /// so IDs include a culture suffix to avoid prototype ID collisions.
    /// </summary>
    private IEnumerable<string> GetCorvaxGoobJobHighlightIds(string jobId)
    {
        var primaryCulture = _loc.DefaultCulture?.TwoLetterISOLanguageName == "ru"
            ? "ru-RU"
            : "en-US";
        var fallbackCulture = primaryCulture == "ru-RU"
            ? "en-US"
            : "ru-RU";

        yield return $"{jobId}-{primaryCulture}";
        yield return $"{jobId}-{fallbackCulture}";
    }

    /// <summary>
    /// Resolves <see cref="CharacterData.Job"/> back to a job prototype.
    /// The title may arrive localized, so localized names are checked before the English-like job ID fallback.
    /// </summary>
    private bool TryResolveCorvaxGoobJobPrototype(string jobName, out JobPrototype job)
    {
        var normalizedJobName = NormalizeCorvaxGoobJobName(jobName);

        foreach (var prototype in _prototypeManager.EnumeratePrototypes<JobPrototype>())
        {
            if (NormalizeCorvaxGoobJobName(prototype.LocalizedName) == normalizedJobName
                || NormalizeCorvaxGoobJobName(HumanizeCorvaxGoobJobId(prototype.ID)) == normalizedJobName)
            {
                job = prototype;
                return true;
            }
        }

        job = default!;
        return false;
    }

    /// <summary>
    /// Adds a formatted line if the source value is not empty and has not already been generated.
    /// </summary>
    private static void TryAddCorvaxGoobHighlightLine(List<string> lines, HashSet<string> seen, string value)
    {
        var line = FormatCorvaxGoobHighlightLine(value);

        if (line == null || !seen.Add(line))
            return;

        lines.Add(line);
    }

    /// <summary>
    /// Normalizes one highlight value into the user-editable format expected by chat highlights: <c>"word"</c>.
    /// Already quoted values in prototype data are accepted and re-quoted consistently.
    /// </summary>
    private static string? FormatCorvaxGoobHighlightLine(string value)
    {
        var trimmed = value.Trim();

        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
            trimmed = trimmed[1..^1].Trim();

        if (trimmed.Length == 0)
            return null;

        return $"\"{trimmed}\"";
    }

    /// <summary>
    /// Makes localized job titles and generated fallback titles comparable.
    /// </summary>
    private static string NormalizeCorvaxGoobJobName(string name)
    {
        return string.Join(" ", CorvaxGoobNameWordSeparators.Split(name.Trim())).ToLowerInvariant();
    }

    /// <summary>
    /// Converts job IDs such as <c>StationEngineer</c> into <c>Station Engineer</c>
    /// for cases where the server sends an English title while the client is using another locale.
    /// </summary>
    private static string HumanizeCorvaxGoobJobId(string id)
    {
        var builder = new StringBuilder(id.Length + 8);

        for (var i = 0; i < id.Length; i++)
        {
            var current = id[i];

            if (i > 0
                && char.IsUpper(current)
                && (!char.IsUpper(id[i - 1]) || (i + 1 < id.Length && char.IsLower(id[i + 1]))))
            {
                builder.Append(' ');
            }

            builder.Append(current);
        }

        return builder.ToString();
    }
}
