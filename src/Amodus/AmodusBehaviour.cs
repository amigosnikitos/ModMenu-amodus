using System;
using Amodus.Features;
using Amodus.Ui;
using UnityEngine;

namespace Amodus;

/// <summary>
/// The only Unity component Amodus adds to the game. It does nothing but forward Unity's callbacks
/// (Update, FixedUpdate, OnGUI) to ordinary C# classes, so the rest of the mod is plain managed code.
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

            var features = FeatureRegistry.All;
            for (int i = 0; i < features.Length; i++)
                features[i].OnUpdate();
        }
        catch (Exception e)
        {
            ErrorOnce.Log("Update", e);
        }
    }

    public void FixedUpdate()
    {
        try
        {
            var features = FeatureRegistry.All;
            for (int i = 0; i < features.Length; i++)
                features[i].OnFixedUpdate();
        }
        catch (Exception e)
        {
            ErrorOnce.Log("FixedUpdate", e);
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
