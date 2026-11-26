using Amodus.Features;
using Amodus.Ui;
using UnityEngine;

namespace Amodus;

/// <summary>Watches the keyboard: opens / closes the menu and switches features with their hotkeys.</summary>
internal static class Hotkeys
{
    private static int _suppressUntilFrame = -1;

    /// <summary>Ignore the feature hotkeys for a few frames (used right after the player picked a new key).</summary>
    public static void SuppressFor(int frames)
    {
        _suppressUntilFrame = Time.frameCount + frames;
    }

    /// <summary>Called every frame from <see cref="AmodusBehaviour.Update"/>.</summary>
    public static void Tick()
    {
        var menuKey = AmodusPlugin.MenuKey.Value;
        if (menuKey != KeyCode.None && Input.GetKeyDown(menuKey))
            MenuWindow.Toggle();

        // While the menu waits for a new key, no hotkey may fire.
        if (MenuWindow.IsRebinding || Time.frameCount <= _suppressUntilFrame)
            return;

        var features = FeatureRegistry.All;
        for (int i = 0; i < features.Length; i++)
        {
            var feature = features[i];
            var key = feature.Hotkey.Value;
            if (key == KeyCode.None || !Input.GetKeyDown(key))
                continue;

            // A letter typed into the chat box must not switch a feature.
            if (GameState.IsTypingInChat())
                continue;

            feature.Toggle();
        }
    }
}
