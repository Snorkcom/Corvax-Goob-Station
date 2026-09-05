// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._CorvaxGoob.Traitor.HighRiskPinpointer;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._CorvaxGoob.Traitor.HighRiskPinpointer;

/// <summary>
/// Opens the fixed target list and sends the selected high-risk item or DNA sequence to the server.
/// </summary>
[UsedImplicitly]
public sealed class HighRiskPinpointerUserInterface : BoundUserInterface
{
    private HighRiskPinpointerWindow? _window;
    private int _selectedTargetId = (int) HighRiskPinpointerTarget.Hypospray;

    public HighRiskPinpointerUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<HighRiskPinpointerWindow>();
        _window.TargetSelected += target => SendMessage(new HighRiskPinpointerSelectTargetMessage(target));
        _window.DnaSubmitted += dna => SendMessage(new HighRiskPinpointerTrackDnaMessage(dna));
        _window.SelectionChanged += targetId => _selectedTargetId = targetId;
        _window.SelectTarget(_selectedTargetId);
        _window.OpenCentered();
    }
}
