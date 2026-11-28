using System;
using BepInEx.Configuration;
using UnityEngine;

namespace Amodus.Features;

/// <summary>Base class for everything that gets a row in the menu.</summary>
internal abstract class Feature
{
    /// <summary>Short stable name, used as the key in the config file. Don't rename it after a release (players would lose their hotkey).</summary>
    public abstract string Id { get; }

    /// <summary>Name of the row in the menu.</summary>
    public abstract string Title { get; }

    /// <summary>The menu tab the row is shown in. Features with the same category share a tab.</summary>
    public virtual string Category => "Game";

    /// <summary>One short line under the title (about 40 characters fit). It may change while the game runs.</summary>
    public virtual string Description => string.Empty;

    /// <summary>Hotkey used until the player picks another one. KeyCode.None = no hotkey.</summary>
    public virtual KeyCode DefaultKey => KeyCode.None;

    /// <summary>True if the feature is already switched on when the game starts. The player can change it in the config file.</summary>
    public virtual bool EnabledByDefault => false;

    /// <summary>The hotkey, stored in the config file. The menu changes it when the player picks a new key.</summary>
    public ConfigEntry<KeyCode> Hotkey { get; private set; }

    public bool Enabled { get; private set; }

    public void BindConfig(ConfigFile config)
    {
        Hotkey = config.Bind("Hotkeys", Id, DefaultKey, $"Hotkey for \"{Title}\". Set to None to have no hotkey.");

        // The game is not running yet at this point, so the starting state is set directly (OnEnabled is not called).
        var startEnabled = config.Bind("Startup", Id, EnabledByDefault, $"Switch \"{Title}\" on when the game starts.");
        Enabled = startEnabled.Value;
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

    /// <summary>Called every frame after all Update calls, also while the feature is off.</summary>
    public virtual void OnLateUpdate()
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
