using UnityEngine;

namespace Amodus.Features;

/// <summary>
/// Walk through walls. Walls only stop the player because the player has a physics collider,
/// so while the feature is on, the collider of the local player is switched off.
/// It is the collider the game keeps in <c>PlayerControl.Collider</c>, and it is switched off on every
/// physics step right after the game has updated the player (see <see cref="Patches.PlayerControlPatches"/>),
/// so the game cannot switch it back on before the physics engine looks at it.
/// </summary>
internal sealed class NoClipFeature : Feature
{
    public override string Id => "NoClip";

    public override string Title => "Noclip";

    public override KeyCode DefaultKey => KeyCode.F1;

    /// <summary>Called right after the game has updated a player (every physics step, for every player).</summary>
    public void OnPlayerFixedUpdate(PlayerControl player)
    {
        if (!Enabled || player == null || !player.AmOwner)
            return;

        SetColliderEnabled(player, false);
    }

    protected override void OnDisabled() => SetColliderEnabled(PlayerControl.LocalPlayer, true);

    private static void SetColliderEnabled(PlayerControl player, bool value)
    {
        if (player == null)
            return;

        var collider = player.Collider;
        if (collider != null && collider.enabled != value)
            collider.enabled = value;
    }
}
