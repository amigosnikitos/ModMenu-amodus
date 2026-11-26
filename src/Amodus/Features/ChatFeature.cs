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

    public override KeyCode DefaultKey => KeyCode.F2;

    protected override void OnEnabled() => SetChatVisible(true);

    protected override void OnDisabled() => SetChatVisible(VanillaShowsChat());

    private static void SetChatVisible(bool visible)
    {
        var hud = HudManager.Instance;
        if (hud == null || hud.Chat == null)
            return;

        hud.Chat.SetVisible(visible);
    }

    // What the unmodded game would do right now: chat in the lobby and in meetings, and for dead players.
    private static bool VanillaShowsChat()
    {
        if (LobbyBehaviour.Instance != null || MeetingHud.Instance != null)
            return true;

        var me = PlayerControl.LocalPlayer;
        return me != null && me.Data != null && me.Data.IsDead;
    }
}
