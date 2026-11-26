using System;
using Amodus.Features;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace Amodus;

/// <summary>Entry point: BepInEx creates this class and calls <see cref="Load"/> when the game starts.</summary>
[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
[BepInProcess("Among Us.exe")]
public sealed class AmodusPlugin : BasePlugin
{
    internal static ManualLogSource LogSource { get; private set; }

    /// <summary>The key that shows / hides the menu (Delete by default).</summary>
    internal static ConfigEntry<KeyCode> MenuKey { get; private set; }

    public override void Load()
    {
        LogSource = Log;

        MenuKey = Config.Bind("General", "MenuKey", KeyCode.Delete, "Key that shows / hides the Amodus menu.");

        foreach (var feature in FeatureRegistry.All)
            feature.BindConfig(Config);

        // Harmony patches: every class with [HarmonyPatch] in the Patches folder.
        // A patch that fails (for example after a game update) must not stop the menu from loading.
        try
        {
            new Harmony(ModInfo.Guid).PatchAll(typeof(AmodusPlugin).Assembly);
        }
        catch (Exception e)
        {
            Log.LogError($"Harmony patches failed: {e}");
        }

        // The menu and the hotkeys live in one Unity component.
        ClassInjector.RegisterTypeInIl2Cpp<AmodusBehaviour>();
        AddComponent<AmodusBehaviour>();

        Log.LogInfo($"{ModInfo.Name} {ModInfo.Version} loaded. Press {MenuKey.Value} in game to open the menu.");
    }
}
