namespace Amodus.Features;

/// <summary>
/// Decides if the chat button is on screen. The unmodded game shows it only in the lobby, in meetings and to dead players.
/// The Chat and Ghost chat features both want it on screen all the time, so they share this class.
/// </summary>
internal static class ChatVisibility
{
    /// <summary>True while a feature wants the chat button on screen during the round too.</summary>
    public static bool Forced => FeatureRegistry.Chat.Enabled || FeatureRegistry.GhostChat.Enabled;

    /// <summary>Applies the current wish to the chat button: forced on, or what the unmodded game would do.</summary>
    public static void Refresh()
    {
        var hud = HudManager.Instance;
        if (hud == null || hud.Chat == null)
            return;

        hud.Chat.SetVisible(Forced || VanillaShowsChat());
    }

    private static bool VanillaShowsChat()
    {
        if (LobbyBehaviour.Instance != null || MeetingHud.Instance != null)
            return true;

        var me = PlayerControl.LocalPlayer;
        return me != null && me.Data != null && me.Data.IsDead;
    }
}
