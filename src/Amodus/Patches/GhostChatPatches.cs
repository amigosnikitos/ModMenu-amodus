using System;
using Amodus.Features;
using HarmonyLib;

namespace Amodus.Patches;

/// <summary>
/// ChatController.AddChat shows a chat message. Its first lines drop the message when the sender is dead and the local
/// player is alive. While the Ghost chat feature is on, the local player is marked as dead for the length of that one call
/// (the call does not wait for anything, so nothing else sees it), and the mark is removed again right after,
/// even if the call fails.
/// </summary>
[HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
internal static class GhostChatPatches
{
    [HarmonyPrefix]
    public static void Prefix(PlayerControl sourcePlayer, out bool __state)
    {
        __state = false;

        try
        {
            if (!FeatureRegistry.GhostChat.Enabled || sourcePlayer == null)
                return;

            var me = PlayerControl.LocalPlayer;
            if (me == null || me.Data == null || sourcePlayer.Data == null)
                return;

            if (sourcePlayer.Data.IsDead && !me.Data.IsDead)
            {
                me.Data.IsDead = true;
                __state = true;
            }
        }
        catch (Exception e)
        {
            ErrorOnce.Log("GhostChat.Prefix", e);
        }
    }

    [HarmonyFinalizer]
    public static void Finalizer(bool __state)
    {
        if (!__state)
            return;

        try
        {
            var me = PlayerControl.LocalPlayer;
            if (me != null && me.Data != null)
                me.Data.IsDead = false;
        }
        catch (Exception e)
        {
            ErrorOnce.Log("GhostChat.Finalizer", e);
        }
    }
}
