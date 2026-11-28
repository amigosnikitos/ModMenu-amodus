using System;
using Amodus.Features;
using HarmonyLib;

namespace Amodus.Patches;

/// <summary>
/// Runs right after the game has updated a player, on every physics step. Noclip uses it to keep the collider of the
/// local player switched off at the right moment, and See ghosts to show dead players right after the game has hidden them.
/// </summary>
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
internal static class PlayerControlPatches
{
    [HarmonyPostfix]
    public static void Postfix(PlayerControl __instance)
    {
        try
        {
            FeatureRegistry.NoClip.OnPlayerFixedUpdate(__instance);
        }
        catch (Exception e)
        {
            ErrorOnce.Log("PlayerControl.FixedUpdate (NoClip)", e);
        }

        try
        {
            FeatureRegistry.SeeGhosts.OnPlayerUpdate(__instance);
        }
        catch (Exception e)
        {
            ErrorOnce.Log("PlayerControl.FixedUpdate (SeeGhosts)", e);
        }
    }
}
