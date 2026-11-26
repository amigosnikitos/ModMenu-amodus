using System;
using UnityEngine;

namespace Amodus.Ui;

/// <summary>
/// The only place that talks to Unity's IMGUI.
///
/// Among Us is built without most of IMGUI. BepInEx restores some of the missing methods when the game starts,
/// but the ones it cannot restore throw "Method unstripping failed" as soon as they are called. So the menu uses
/// the smallest set that is known to work in IL2CPP games: GUI.Button and GUI.Label with the default skin,
/// tinted with GUI.backgroundColor / GUI.contentColor. It does not use Event.current, GUIStyle, GUI.skin or
/// GUI.DrawTexture. If a call fails anyway, it is switched off, the reason is written to the log once,
/// and the game keeps running.
/// </summary>
internal static class Painter
{
    private static bool _buttonBroken;
    private static bool _labelBroken;
    private static bool _backgroundColorBroken;
    private static bool _contentColorBroken;

    /// <summary>
    /// A tinted box with a text in the middle. It is a default-skin button; its clicks are ignored
    /// (the menu reads the mouse itself, see <see cref="MenuWindow"/>).
    /// </summary>
    public static void Box(Rect rect, string text, Color tint)
    {
        if (_buttonBroken)
            return;

        var previous = default(Color);
        bool tinted = false;

        if (!_backgroundColorBroken)
        {
            try
            {
                previous = GUI.backgroundColor;
                GUI.backgroundColor = tint;
                tinted = true;
            }
            catch (Exception e)
            {
                _backgroundColorBroken = true;
                ErrorOnce.Log("GUI.backgroundColor", e);
            }
        }

        try
        {
            GUI.Button(rect, text);
        }
        catch (Exception e)
        {
            _buttonBroken = true;
            ErrorOnce.Log("GUI.Button", e);
        }

        if (tinted)
        {
            try
            {
                GUI.backgroundColor = previous;
            }
            catch (Exception e)
            {
                _backgroundColorBroken = true;
                ErrorOnce.Log("GUI.backgroundColor", e);
            }
        }
    }

    /// <summary>A text with the default label look (left aligned), in the given colour.</summary>
    public static void Label(Rect rect, string text, Color color)
    {
        if (_labelBroken)
            return;

        var previous = default(Color);
        bool tinted = false;

        if (!_contentColorBroken)
        {
            try
            {
                previous = GUI.contentColor;
                GUI.contentColor = color;
                tinted = true;
            }
            catch (Exception e)
            {
                _contentColorBroken = true;
                ErrorOnce.Log("GUI.contentColor", e);
            }
        }

        try
        {
            GUI.Label(rect, text);
        }
        catch (Exception e)
        {
            _labelBroken = true;
            ErrorOnce.Log("GUI.Label", e);
        }

        if (tinted)
        {
            try
            {
                GUI.contentColor = previous;
            }
            catch (Exception e)
            {
                _contentColorBroken = true;
                ErrorOnce.Log("GUI.contentColor", e);
            }
        }
    }
}
