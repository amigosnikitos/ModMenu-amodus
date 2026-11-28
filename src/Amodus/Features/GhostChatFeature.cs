using UnityEngine;

namespace Amodus.Features;

/// <summary>
/// Lets a living player read what dead players write, so the living and the ghosts can talk during the round.
/// The unmodded game receives the messages of dead players but drops them when the local player is alive;
/// the patch in <see cref="Patches.GhostChatPatches"/> switches that check off while this feature is on.
/// The chat button also stays on screen, so the feature works without the Chat feature.
/// </summary>
internal sealed class GhostChatFeature : Feature
{
    public override string Id => "GhostChat";

    public override string Title => "Ghost chat";

    public override string Category => "Chat";

    public override string Description => "Read what dead players write";

    public override KeyCode DefaultKey => KeyCode.F3;

    protected override void OnEnabled() => ChatVisibility.Refresh();

    protected override void OnDisabled() => ChatVisibility.Refresh();
}
