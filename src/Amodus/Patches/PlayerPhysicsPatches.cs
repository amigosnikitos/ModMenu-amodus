using System;
using Amodus.Features;
using HarmonyLib;

namespace Amodus.Patches;

/// <summary>Runs right after the game has updated the look of a player, once per frame. See ghosts uses it.</summary>
[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.LateUpdate))]
internal static class PlayerPhysicsPatches
{
    [HarmonyPostfix]
    public static void Postfix(PlayerPhysics __instance)
    {
        try
        {
            FeatureRegistry.SeeGhosts.OnPlayerUpdate(__instance.myPlayer);
        }
        catch (Exception e)
        {
            ErrorOnce.Log("PlayerPhysics.LateUpdate", e);
        }
    }
}
