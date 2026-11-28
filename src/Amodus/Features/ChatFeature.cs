using UnityEngine;

namespace Amodus.Features;

/// <summary>
/// Keeps the chat button on screen during the round. The unmodded game shows it only in the lobby
/// and in meetings (and to dead players). While this feature is on, the patch in
/// <see cref="Patches.ChatControllerPatches"/> turns every "hide the chat" call of the game into "show the chat".
/// </summary>
internal sealed class ChatFeature : Feature
{
    public override string Id => "Chat";

    public override string Title => "Chat";

    public override string Category => "Chat";

    public override string Description => "Keep the chat open during the round";

    public override KeyCode DefaultKey => KeyCode.F2;

    protected override void OnEnabled() => ChatVisibility.Refresh();

    protected override void OnDisabled() => ChatVisibility.Refresh();
}
