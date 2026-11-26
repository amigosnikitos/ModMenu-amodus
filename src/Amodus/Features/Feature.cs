using System;
using BepInEx.Configuration;
using UnityEngine;

namespace Amodus.Features;

/// <summary>Base class for everything that gets a button in the menu.</summary>
internal abstract class Feature
{
    /// <summary>Short stable name, used as the key in the config file. Don't rename it after a release (players would lose their hotkey).</summary>
    public abstract string Id { get; }

    /// <summary>Text on the menu button.</summary>
    public abstract string Title { get; }

    /// <summary>Hotkey used until the player picks another one. KeyCode.None = no hotkey.</summary>
    public virtual KeyCode DefaultKey => KeyCode.None;

    /// <summary>The hotkey, stored in the config file. The menu changes it when the player picks a new key.</summary>
    public ConfigEntry<KeyCode> Hotkey { get; private set; }

    public bool Enabled { get; private set; }

    public void BindConfig(ConfigFile config)
    {
        Hotkey = config.Bind("Hotkeys", Id, DefaultKey, $"Hotkey for \"{Title}\". Set to None to have no hotkey.");
    }

    public void Toggle() => SetEnabled(!Enabled);

    public void SetEnabled(bool value)
    {
        if (Enabled == value)
            return;

        Enabled = value;

        try
        {
            if (value)
                OnEnabled();
            else
                OnDisabled();
        }
        catch (Exception e)
        {
            ErrorOnce.Log(Id, e);
        }
    }

    /// <summary>Called every frame, also while the feature is off - check <see cref="Enabled"/> yourself.</summary>
    public virtual void OnUpdate()
    {
    }

    /// <summary>Called on every physics step, also while the feature is off.</summary>
    public virtual void OnFixedUpdate()
    {
    }

    /// <summary>The feature was just switched on.</summary>
    protected virtual void OnEnabled()
    {
    }

    /// <summary>The feature was just switched off. Undo what it did to the game here.</summary>
    protected virtual void OnDisabled()
    {
    }
}
