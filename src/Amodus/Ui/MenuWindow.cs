using System;
using System.Collections.Generic;
using Amodus.Features;
using UnityEngine;

namespace Amodus.Ui;

/// <summary>
/// The Amodus window. It is split in two on purpose:
/// <see cref="Update"/> reads the mouse and the keyboard from Unity's Input (clicks, dragging, picking a new hotkey),
/// <see cref="Draw"/> only draws, through <see cref="Painter"/>. Reading Input instead of IMGUI events keeps the menu
/// away from the parts of IMGUI that Among Us does not have (Event.current and friends).
/// </summary>
internal static class MenuWindow
{
    public static bool Visible { get; private set; }

    /// <summary>True while the menu waits for the player to press a new hotkey.</summary>
    public static bool IsRebinding => _rebinding != null;

    // ---- state ----
    private static float _x = 40f;
    private static float _y = 40f;
    private static bool _dragging;
    private static float _dragOffsetX;
    private static float _dragOffsetY;

    private static Feature _rebinding;
    private static string _status = string.Empty;
    private static float _statusUntil;
    private static KeyCode[] _bindableKeys;

    // ---- look ----
    // The tints are multiplied with the colours of the default IMGUI skin (see Painter).
    private static readonly Color PanelTint = new Color(0.5f, 0.55f, 0.8f, 1f);
    private static readonly Color HeaderTint = new Color(0.6f, 0.9f, 1.6f, 1f);
    private static readonly Color OnTint = new Color(0.5f, 1.6f, 0.7f, 1f);
    private static readonly Color OffTint = new Color(1f, 1f, 1f, 1f);
    private static readonly Color AccentTint = new Color(0.7f, 1.1f, 1.7f, 1f);
    private static readonly Color DimTextColor = new Color(0.78f, 0.82f, 0.92f, 1f);
    private static readonly Color StatusTextColor = new Color(0.55f, 0.75f, 1f, 1f);

    public static void Toggle()
    {
        Visible = !Visible;

        if (!Visible)
        {
            _rebinding = null;
            _dragging = false;
        }
    }

    // ------------------------------------------------------------------ layout

    private struct Metrics
    {
        public float Scale;
        public float Width;
        public float Height;
        public float Pad;
        public float HeaderH;
        public float RowH;
        public float Gap;
        public float FooterH;
    }

    private static Metrics Measure()
    {
        // Everything is sized relative to a 1080p screen, so the menu stays usable on 1440p / 4K.
        float s = Math.Min(Math.Max(Screen.height / 1080f, 0.8f), 2f);

        var m = new Metrics
        {
            Scale = s,
            Width = 340f * s,
            Pad = 12f * s,
            HeaderH = 40f * s,
            RowH = 38f * s,
            Gap = 8f * s,
            FooterH = 50f * s,
        };

        m.Height = m.HeaderH + m.Pad + FeatureRegistry.All.Length * (m.RowH + m.Gap) + m.FooterH;
        return m;
    }

    /// <summary>The window rectangle. The window is kept on the screen.</summary>
    private static Rect WindowRect(Metrics m)
    {
        _x = Math.Min(Math.Max(_x, 0f), Math.Max(0f, Screen.width - m.Width));
        _y = Math.Min(Math.Max(_y, 0f), Math.Max(0f, Screen.height - m.Height));
        return new Rect(_x, _y, m.Width, m.Height);
    }

    private static Rect HeaderRect(Rect window, Metrics m)
    {
        return new Rect(window.x, window.y, m.Width, m.HeaderH);
    }

    private static Rect CloseRect(Rect window, Metrics m)
    {
        float size = m.HeaderH - 12f * m.Scale;
        return new Rect(window.x + m.Width - m.HeaderH + 6f * m.Scale, window.y + 6f * m.Scale, size, size);
    }

    /// <summary>The two rectangles of a feature row: the big on/off button and the small "Bind" button.</summary>
    private static void RowRects(Rect window, Metrics m, int index, out Rect main, out Rect bind)
    {
        float x = window.x + m.Pad;
        float y = window.y + m.HeaderH + m.Pad + index * (m.RowH + m.Gap);
        float width = m.Width - 2f * m.Pad;
        float bindWidth = 56f * m.Scale;
        float spacing = 6f * m.Scale;

        main = new Rect(x, y, width - bindWidth - spacing, m.RowH);
        bind = new Rect(x + width - bindWidth, y, bindWidth, m.RowH);
    }

    private static bool Hit(Rect r, float x, float y)
    {
        return x >= r.x && x < r.x + r.width && y >= r.y && y < r.y + r.height;
    }

    // ------------------------------------------------------------------ input

    /// <summary>Called every frame from <see cref="AmodusBehaviour.Update"/>.</summary>
    public static void Update()
    {
        if (!Visible)
            return;

        var m = Measure();
        var window = WindowRect(m);

        // Unity's mouse position starts in the bottom left corner, the menu's in the top left one.
        var mouse = Input.mousePosition;
        float mx = mouse.x;
        float my = Screen.height - mouse.y;

        if (_rebinding != null)
            PollKeyForBinding();

        if (_dragging)
        {
            if (Input.GetMouseButton(0))
            {
                _x = mx - _dragOffsetX;
                _y = my - _dragOffsetY;
            }
            else
            {
                _dragging = false;
            }
        }

        bool left = Input.GetMouseButtonDown(0);
        bool right = Input.GetMouseButtonDown(1);
        if ((!left && !right) || !Hit(window, mx, my))
            return;

        if (left)
        {
            if (Hit(CloseRect(window, m), mx, my))
            {
                Toggle();
                return;
            }

            // The window can be dragged by its header.
            if (Hit(HeaderRect(window, m), mx, my))
            {
                _dragging = true;
                _dragOffsetX = mx - _x;
                _dragOffsetY = my - _y;
                return;
            }
        }

        var features = FeatureRegistry.All;
        for (int i = 0; i < features.Length; i++)
        {
            var feature = features[i];
            bool listening = _rebinding == feature;
            RowRects(window, m, i, out var main, out var bind);

            if (Hit(main, mx, my))
            {
                // Right click: choose a new hotkey. Left click: switch the feature on / off.
                if (right)
                    StartBinding(feature);
                else if (listening)
                    _rebinding = null;
                else
                    feature.Toggle();

                return;
            }

            // The small "Bind" button does the same as the right click.
            if (left && Hit(bind, mx, my))
            {
                if (listening)
                    _rebinding = null;
                else
                    StartBinding(feature);

                return;
            }
        }
    }

    // ------------------------------------------------------------------ drawing

    /// <summary>Called from OnGUI. Draws the window; does nothing while the menu is hidden.</summary>
    public static void Draw()
    {
        if (!Visible)
            return;

        var m = Measure();
        var window = WindowRect(m);
        var features = FeatureRegistry.All;

        Painter.Box(window, string.Empty, PanelTint);
        Painter.Box(HeaderRect(window, m), "AMODUS   v" + ModInfo.Version, HeaderTint);
        Painter.Box(CloseRect(window, m), "X", OffTint);

        for (int i = 0; i < features.Length; i++)
        {
            var feature = features[i];
            bool listening = _rebinding == feature;
            RowRects(window, m, i, out var main, out var bind);

            // The hotkey is written on the button itself.
            string key = listening ? "press a key" : "[" + PrettyKey(feature.Hotkey.Value) + "]";
            string state = feature.Enabled ? "ON" : "OFF";
            Painter.Box(main, feature.Title + "     " + state + "     " + key, feature.Enabled ? OnTint : OffTint);
            Painter.Box(bind, listening ? "..." : "Bind", listening ? AccentTint : OffTint);
        }

        DrawFooter(window, m, features.Length);
    }

    private static void DrawFooter(Rect window, Metrics m, int rows)
    {
        float x = window.x + m.Pad;
        float y = window.y + m.HeaderH + m.Pad + rows * (m.RowH + m.Gap);
        float width = m.Width - 2f * m.Pad;
        float half = (m.FooterH - 4f * m.Scale) * 0.5f;

        string first;
        var firstColor = DimTextColor;

        if (_rebinding != null)
        {
            first = "Press a key for " + _rebinding.Title + "...";
            firstColor = StatusTextColor;
        }
        else if (Time.unscaledTime < _statusUntil)
        {
            first = _status;
            firstColor = StatusTextColor;
        }
        else
        {
            first = "Click: on/off    Right-click / Bind: set key";
        }

        string second = _rebinding != null
            ? "Esc: cancel    Backspace: no hotkey"
            : PrettyKey(AmodusPlugin.MenuKey.Value) + ": hide the menu";

        Painter.Label(new Rect(x, y, width, half), first, firstColor);
        Painter.Label(new Rect(x, y + half, width, half), second, DimTextColor);
    }

    // ------------------------------------------------------------ rebinding

    private static void StartBinding(Feature feature)
    {
        _rebinding = feature;
        _statusUntil = 0f;
    }

    /// <summary>Looks for a key that was pressed this frame while the menu waits for a new hotkey.</summary>
    private static void PollKeyForBinding()
    {
        if (_bindableKeys == null)
            _bindableKeys = ListBindableKeys();

        for (int i = 0; i < _bindableKeys.Length; i++)
        {
            if (Input.GetKeyDown(_bindableKeys[i]))
            {
                OnKeyWhileBinding(_bindableKeys[i]);
                return;
            }
        }
    }

    // Every keyboard key; mouse buttons and gamepad buttons are not offered as hotkeys.
    private static KeyCode[] ListBindableKeys()
    {
        var keys = new List<KeyCode>();

        foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
        {
            string name = key.ToString();
            if (key == KeyCode.None || name.StartsWith("Mouse") || name.StartsWith("Joystick"))
                continue;

            keys.Add(key);
        }

        return keys.ToArray();
    }

    private static void OnKeyWhileBinding(KeyCode key)
    {
        var feature = _rebinding;

        if (key == KeyCode.Escape)
        {
            _rebinding = null;
            return;
        }

        if (key == KeyCode.Backspace)
        {
            feature.Hotkey.Value = KeyCode.None;
            FinishBinding(feature.Title + ": no hotkey");
            return;
        }

        // The menu key and the hotkeys of other features are taken; keep waiting for another key.
        if (key == AmodusPlugin.MenuKey.Value)
        {
            SetStatus(PrettyKey(key) + " opens the menu - pick another key");
            return;
        }

        var all = FeatureRegistry.All;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != feature && all[i].Hotkey.Value == key)
            {
                SetStatus(PrettyKey(key) + " is used by " + all[i].Title);
                return;
            }
        }

        feature.Hotkey.Value = key;
        FinishBinding(feature.Title + ": " + PrettyKey(key));
    }

    private static void FinishBinding(string message)
    {
        _rebinding = null;
        SetStatus(message);

        // The key that was just pressed must not also switch a feature.
        Hotkeys.SuppressFor(2);
    }

    private static void SetStatus(string text)
    {
        _status = text;
        _statusUntil = Time.unscaledTime + 3f;
    }

    /// <summary>A short, readable name for a key: "F1", "LShift", "5", "Num1"...</summary>
    internal static string PrettyKey(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.None:
                return "none";
            case KeyCode.LeftControl:
                return "LCtrl";
            case KeyCode.RightControl:
                return "RCtrl";
            case KeyCode.LeftShift:
                return "LShift";
            case KeyCode.RightShift:
                return "RShift";
            case KeyCode.LeftAlt:
                return "LAlt";
            case KeyCode.RightAlt:
                return "RAlt";
            default:
                string name = key.ToString();
                if (name.StartsWith("Alpha"))
                    return name.Substring(5);
                if (name.StartsWith("Keypad"))
                    return "Num" + name.Substring(6);
                return name;
        }
    }
}
