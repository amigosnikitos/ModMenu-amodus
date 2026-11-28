using System;
using UnityEngine;

namespace Amodus.Ui;

/// <summary>
/// The only place that talks to Unity's IMGUI.
///
/// Among Us is an IL2CPP game. The parts of IMGUI that the game itself never uses are cut out of it, and a call into a cut
/// part throws "Method unstripping failed". So Painter uses only members that the game keeps: a <see cref="GUIStyle"/> with the
/// plain white texture as its background draws every rectangle (tinted with GUI.backgroundColor), and GUI.Label with a
/// copy of the default label style draws every text (tinted with GUI.contentColor). There is no DrawTexture, no GUI.Button.
///
/// Every call is wrapped: if one of them is missing in some version of the game, it is switched off for good, one line is
/// written to the log, and the next best way is used (the default box style, the default label style).
/// </summary>
internal static class Painter
{
    internal enum Align
    {
        Left = 0,
        Center = 1,
        Right = 2,
    }

    // The default box of IMGUI is see-through. When the flat style is not available, a few boxes are drawn on top of each other.
    private const int OpacityLayers = 4;

    private static float _scale = 1f;
    private static bool _repaint;
    private static bool _stylesTried;

    private static GUIStyle _solid;
    private static bool _solidIsFlat;
    private static readonly GUIStyle[] TextStyles = new GUIStyle[3];
    private static readonly int[] TextSizes = new int[3];

    private static bool _drawBroken;
    private static bool _labelBroken;
    private static bool _boxBroken;
    private static bool _backgroundColorBroken;
    private static bool _contentColorBroken;
    private static bool _eventBroken;

    private static Color _savedBackground;
    private static Color _savedContent;
    private static bool _backgroundSaved;
    private static bool _contentSaved;

    /// <summary>Called at the start of OnGUI. <paramref name="scale"/> turns layout units into pixels.</summary>
    public static void Begin(float scale)
    {
        _scale = scale;
        _repaint = IsRepaintEvent();
        if (!_repaint)
            return;

        EnsureStyles();
        SaveColors();
    }

    /// <summary>Called at the end of OnGUI: puts the global GUI colours back.</summary>
    public static void End()
    {
        if (_repaint)
            RestoreColors();
    }

    /// <summary>A flat rectangle. The rectangle is given in layout units.</summary>
    public static void Fill(Rect rect, Color color)
    {
        if (!_repaint)
            return;

        rect = Scaled(rect);
        SetBackground(color);

        if (_solid != null && !_drawBroken)
        {
            // The flat style needs one pass; the see-through default box needs a few to look solid.
            if (_solidIsFlat || (rect.width >= 4f && rect.height >= 4f))
            {
                try
                {
                    int layers = _solidIsFlat ? 1 : OpacityLayers;
                    for (int i = 0; i < layers; i++)
                        _solid.Draw(rect, GUIContent.none, false, false, false, false);

                    return;
                }
                catch (Exception e)
                {
                    _drawBroken = true;
                    Unavailable("GUIStyle.Draw", e);
                }
            }
            else
            {
                return;
            }
        }

        if (_boxBroken || rect.width < 4f || rect.height < 4f)
            return;

        try
        {
            for (int i = 0; i < OpacityLayers; i++)
                GUI.Box(rect, string.Empty);
        }
        catch (Exception e)
        {
            _boxBroken = true;
            ErrorOnce.Log("GUI.Box", e);
        }
    }

    /// <summary>A single line of text. <paramref name="size"/> is the font size in layout units; the text is centred vertically in the rectangle.</summary>
    public static void Text(Rect rect, string text, Color color, float size, Align align)
    {
        if (!_repaint || _labelBroken || string.IsNullOrEmpty(text))
            return;

        rect = Scaled(rect);
        SetContent(color);

        try
        {
            var style = TextStyle(align, Math.Max(8, (int)Math.Round(size * _scale)));
            if (style != null)
                GUI.Label(rect, text, style);
            else
                GUI.Label(rect, text);
        }
        catch (Exception e)
        {
            _labelBroken = true;
            ErrorOnce.Log("GUI.Label", e);
        }
    }

    // ------------------------------------------------------------------ styles

    private static void EnsureStyles()
    {
        if (_stylesTried)
            return;

        _stylesTried = true;

        GUISkin skin;
        try
        {
            skin = GUI.skin;
        }
        catch (Exception e)
        {
            Unavailable("GUI.skin", e);
            return;
        }

        if (skin == null)
            return;

        try
        {
            TextStyles[(int)Align.Left] = NewTextStyle(skin, TextAnchor.MiddleLeft);
            TextStyles[(int)Align.Center] = NewTextStyle(skin, TextAnchor.MiddleCenter);
            TextStyles[(int)Align.Right] = NewTextStyle(skin, TextAnchor.MiddleRight);
        }
        catch (Exception e)
        {
            TextStyles[0] = null;
            TextStyles[1] = null;
            TextStyles[2] = null;
            Unavailable("own text styles", e);
        }

        try
        {
            var solid = new GUIStyle();
            solid.normal.background = Texture2D.whiteTexture;
            _solid = solid;
            _solidIsFlat = true;
        }
        catch (Exception e)
        {
            Unavailable("a flat rectangle style", e);

            try
            {
                _solid = new GUIStyle(skin.box);
                _solidIsFlat = false;
            }
            catch (Exception e2)
            {
                Unavailable("a copy of the box style", e2);
            }
        }
    }

    private static GUIStyle NewTextStyle(GUISkin skin, TextAnchor anchor)
    {
        var style = new GUIStyle(skin.label);
        style.alignment = anchor;
        style.wordWrap = false;
        style.fontSize = 14;

        // The colour of the text comes from GUI.contentColor, so the style itself must not tint it.
        try
        {
            style.normal.textColor = new Color(1f, 1f, 1f, 1f);
        }
        catch (Exception e)
        {
            Unavailable("GUIStyleState.textColor", e);
        }

        return style;
    }

    private static GUIStyle TextStyle(Align align, int pixels)
    {
        int index = (int)align;
        var style = TextStyles[index];
        if (style == null)
            return null;

        if (TextSizes[index] != pixels)
        {
            style.fontSize = pixels;
            TextSizes[index] = pixels;
        }

        return style;
    }

    // ------------------------------------------------------------------ colours and events

    private static bool IsRepaintEvent()
    {
        if (_eventBroken)
            return true;

        try
        {
            var current = Event.current;
            return current != null && current.type == EventType.Repaint;
        }
        catch (Exception e)
        {
            _eventBroken = true;
            Unavailable("Event.current", e);
            return true;
        }
    }

    private static void SaveColors()
    {
        _backgroundSaved = false;
        _contentSaved = false;

        if (!_backgroundColorBroken)
        {
            try
            {
                _savedBackground = GUI.backgroundColor;
                _backgroundSaved = true;
            }
            catch (Exception e)
            {
                _backgroundColorBroken = true;
                Unavailable("GUI.backgroundColor", e);
            }
        }

        if (!_contentColorBroken)
        {
            try
            {
                _savedContent = GUI.contentColor;
                _contentSaved = true;
            }
            catch (Exception e)
            {
                _contentColorBroken = true;
                Unavailable("GUI.contentColor", e);
            }
        }
    }

    private static void RestoreColors()
    {
        if (_backgroundSaved)
        {
            try
            {
                GUI.backgroundColor = _savedBackground;
            }
            catch (Exception e)
            {
                _backgroundColorBroken = true;
                Unavailable("GUI.backgroundColor", e);
            }
        }

        if (_contentSaved)
        {
            try
            {
                GUI.contentColor = _savedContent;
            }
            catch (Exception e)
            {
                _contentColorBroken = true;
                Unavailable("GUI.contentColor", e);
            }
        }
    }

    private static void SetBackground(Color color)
    {
        if (_backgroundColorBroken)
            return;

        try
        {
            GUI.backgroundColor = color;
        }
        catch (Exception e)
        {
            _backgroundColorBroken = true;
            Unavailable("GUI.backgroundColor", e);
        }
    }

    private static void SetContent(Color color)
    {
        if (_contentColorBroken)
            return;

        try
        {
            GUI.contentColor = color;
        }
        catch (Exception e)
        {
            _contentColorBroken = true;
            Unavailable("GUI.contentColor", e);
        }
    }

    // ------------------------------------------------------------------ helpers

    private static Rect Scaled(Rect r)
    {
        return new Rect(r.x * _scale, r.y * _scale, r.width * _scale, r.height * _scale);
    }

    /// <summary>A part of IMGUI is missing in this game. It is not an error: a simpler way is used instead. Logged once per part.</summary>
    private static void Unavailable(string what, Exception e)
    {
        AmodusPlugin.LogSource.LogInfo($"UI: {what} is not available in this version of the game ({e.GetType().Name}: {e.Message}). A simpler way is used instead.");
    }
}
