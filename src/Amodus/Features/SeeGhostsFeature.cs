using UnityEngine;

namespace Amodus.Features;

/// <summary>
/// Shows dead players (ghosts) to a living player. The unmodded game hides them from the living.
/// The game decides who is visible by itself while it updates every player, so the patches in
/// <see cref="Patches.PlayerControlPatches"/> and <see cref="Patches.PlayerPhysicsPatches"/> call
/// <see cref="OnPlayerUpdate"/> right after the game, and <see cref="OnLateUpdate"/> repeats it once per frame as a safety net.
/// </summary>
internal sealed class SeeGhostsFeature : Feature
{
    public override string Id => "SeeGhosts";

    public override string Title => "See ghosts";

    public override string Category => "Game";

    public override string Description => "Show dead players while you are alive";

    public override KeyCode DefaultKey => KeyCode.F4;

    /// <summary>Called right after the game has updated a player.</summary>
    public void OnPlayerUpdate(PlayerControl player)
    {
        if (!Enabled || !IsGhostSeenByAliveMe(player))
            return;

        if (!player.Visible)
            player.Visible = true;
    }

    public override void OnLateUpdate()
    {
        if (Enabled)
            SetGhostsVisible(true);
    }

    protected override void OnDisabled() => SetGhostsVisible(false);

    private static void SetGhostsVisible(bool visible)
    {
        var players = PlayerControl.AllPlayerControls;
        if (players == null)
            return;

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            if (IsGhostSeenByAliveMe(player) && player.Visible != visible)
                player.Visible = visible;
        }
    }

    // A dead local player sees the other ghosts anyway, so the feature only matters while the local player is alive.
    private static bool IsGhostSeenByAliveMe(PlayerControl player)
    {
        if (player == null || player.AmOwner || player.Data == null || !player.Data.IsDead)
            return false;

        var me = PlayerControl.LocalPlayer;
        return me != null && me.Data != null && !me.Data.IsDead;
    }
}
