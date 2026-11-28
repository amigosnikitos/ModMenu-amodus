using System;
using Amodus.Features;
using Amodus.Ui;
using UnityEngine;

namespace Amodus;

/// <summary>
/// The only Unity component Amodus adds to the game. It does nothing but forward Unity's callbacks
/// (Update, LateUpdate, FixedUpdate, OnGUI) to ordinary C# classes, so the rest of the mod is plain managed code.
/// The constructor with IntPtr is required for every class that is injected into IL2CPP.
/// </summary>
public class AmodusBehaviour : MonoBehaviour
{
    public AmodusBehaviour(IntPtr ptr) : base(ptr)
    {
    }

    public void Update()
    {
        try
        {
            Hotkeys.Tick();
        }
        catch (Exception e)
        {
            ErrorOnce.Log("Hotkeys", e);
        }

        // A problem in the menu must not stop the features from running, and the other way round.
        try
        {
            MenuWindow.Update();
        }
        catch (Exception e)
        {
            ErrorOnce.Log("MenuWindow.Update", e);
        }

        var features = FeatureRegistry.All;
        for (int i = 0; i < features.Length; i++)
        {
            try
            {
                features[i].OnUpdate();
            }
            catch (Exception e)
            {
                ErrorOnce.Log(features[i].Id + ".OnUpdate", e);
            }
        }
    }

    public void LateUpdate()
    {
        var features = FeatureRegistry.All;
        for (int i = 0; i < features.Length; i++)
        {
            try
            {
                features[i].OnLateUpdate();
            }
            catch (Exception e)
            {
                ErrorOnce.Log(features[i].Id + ".OnLateUpdate", e);
            }
        }
    }

    public void FixedUpdate()
    {
        var features = FeatureRegistry.All;
        for (int i = 0; i < features.Length; i++)
        {
            try
            {
                features[i].OnFixedUpdate();
            }
            catch (Exception e)
            {
                ErrorOnce.Log(features[i].Id + ".OnFixedUpdate", e);
            }
        }
    }

    public void OnGUI()
    {
        try
        {
            MenuWindow.Draw();
        }
        catch (Exception e)
        {
            ErrorOnce.Log("OnGUI", e);
        }
    }
}
