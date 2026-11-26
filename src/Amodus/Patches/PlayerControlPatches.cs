using System;
using Amodus.Features;
using HarmonyLib;

namespace Amodus.Patches;

/// <summary>
/// Runs right after the game has updated a player, on every physics step. The Noclip feature uses it to keep
/// the collider of the local player switched off at the right moment.
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
            ErrorOnce.Log("PlayerControl.FixedUpdate", e);
        }
    }
}
