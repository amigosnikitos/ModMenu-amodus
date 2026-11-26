using UnityEngine;

namespace Amodus.Features;

/// <summary>
/// Walk through walls. Walls only stop the player because the player has a physics collider,
/// so while the feature is on the collider of the local player is switched off.
/// </summary>
internal sealed class NoClipFeature : Feature
{
    public override string Id => "NoClip";

    public override string Title => "Noclip";

    public override KeyCode DefaultKey => KeyCode.F1;

    // The game may switch the collider back on (meetings, vents...), so it is forced off every frame
    // and also right before every physics step.
    public override void OnUpdate() => Apply();

    public override void OnFixedUpdate() => Apply();

    protected override void OnDisabled() => SetColliderEnabled(true);

    private void Apply()
    {
        if (Enabled)
            SetColliderEnabled(false);
    }

    private static void SetColliderEnabled(bool value)
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null)
            return;

        var collider = player.GetComponent<Collider2D>();
        if (collider != null && collider.enabled != value)
            collider.enabled = value;
    }
}
