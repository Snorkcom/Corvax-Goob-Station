// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;

namespace Content.Shared.Implants;

/// <summary>
/// Raised when a syndicate distress implant action is pressed.
/// The server-side implant system handles the actual radio broadcast.
/// </summary>
public sealed partial class SyndicateDistressImplantActionEvent : InstantActionEvent;
