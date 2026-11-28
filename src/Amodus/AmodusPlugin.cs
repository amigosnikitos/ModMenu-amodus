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

    /// <summary>Size of the menu: 1 is normal, 1.5 is one and a half times bigger. Can be changed in the menu too.</summary>
    internal static ConfigEntry<float> UiScale { get; private set; }

    public override void Load()
    {
        LogSource = Log;

        MenuKey = Config.Bind("General", "MenuKey", KeyCode.Delete, "Key that shows / hides the Amodus menu.");

        UiScale = Config.Bind(
            "General",
            "UiScale",
            1f,
            new ConfigDescription(
                "Size of the menu. 1 is normal, 1.5 is one and a half times bigger. It can also be changed on the Settings tab of the menu.",
                new AcceptableValueRange<float>(0.6f, 2f)));

        foreach (var feature in FeatureRegistry.All)
            feature.BindConfig(Config);

        ApplyPatches();

        // The menu and the hotkeys live in one Unity component.
        ClassInjector.RegisterTypeInIl2Cpp<AmodusBehaviour>();
        AddComponent<AmodusBehaviour>();

        Log.LogInfo($"{ModInfo.Name} {ModInfo.Version} loaded. Press {MenuKey.Value} in game to open the menu.");
    }

    /// <summary>
    /// Applies every class with [HarmonyPatch] from the Patches folder, one by one. A patch that fails (for example after
    /// a game update) only switches off the feature that needs it; the menu and the other patches still load.
    /// </summary>
    private void ApplyPatches()
    {
        var harmony = new Harmony(ModInfo.Guid);

        foreach (var type in typeof(AmodusPlugin).Assembly.GetTypes())
        {
            if (!type.IsDefined(typeof(HarmonyPatch), false))
                continue;

            try
            {
                harmony.CreateClassProcessor(type).Patch();
            }
            catch (Exception e)
            {
                Log.LogError($"Patch {type.Name} failed, the feature that needs it will not work: {e}");
            }
        }
    }
}
