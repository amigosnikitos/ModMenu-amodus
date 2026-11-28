using Amodus.Features;
using HarmonyLib;

namespace Amodus.Patches;

/// <summary>
/// The game calls ChatController.SetVisible(false) when a round starts and in other places.
/// While the Chat or the Ghost chat feature is on, that call is changed into SetVisible(true) before it runs.
/// </summary>
[HarmonyPatch(typeof(ChatController), nameof(ChatController.SetVisible))]
internal static class ChatControllerPatches
{
    [HarmonyPrefix]
    public static void Prefix(ref bool visible)
    {
        if (ChatVisibility.Forced)
            visible = true;
    }
}
