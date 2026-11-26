using Amodus.Features;
using UnityEngine;

namespace Amodus.Ui;

/// <summary>
/// The Amodus window, drawn with Unity's IMGUI. Everything is drawn by hand (no GUI.Window),
/// which keeps the code free of IL2CPP delegate conversions. <see cref="Draw"/> is called from OnGUI.
/// </summary>
internal static class MenuWindow
{
    public static bool Visible { get; private set; }

    /// <summary>True while the menu waits for the player to press a new hotkey.</summary>
    public static bool IsRebinding => _rebinding != null;

    // ---- state ----
    private static Vector2 _position = new Vector2(40f, 40f);
    private static bool _dragging;
    private static Vector2 _dragOffset;

    private static Feature _rebinding;
    private static string _status = string.Empty;
    private static float _statusUntil;

    // ---- look ----
    private static readonly Color WindowColor = new Color(0.07f, 0.08f, 0.11f, 0.96f);
    private static readonly Color HeaderColor = new Color(0.11f, 0.13f, 0.20f, 1f);
    private static readonly Color AccentColor = new Color(0.36f, 0.55f, 1f, 1f);
    private static readonly Color OnColor = new Color(0.14f, 0.52f, 0.34f, 1f);
    private static readonly Color OffColor = new Color(0.17f, 0.19f, 0.25f, 1f);
    private static readonly Color HoverTint = new Color(1f, 1f, 1f, 0.10f);
    private static readonly Color TextColor = new Color(0.93f, 0.95f, 1f, 1f);
    private static readonly Color DimTextColor = new Color(0.62f, 0.67f, 0.78f, 1f);

    private static GUIStyle _titleStyle;
    private static GUIStyle _versionStyle;
    private static GUIStyle _nameStyle;
    private static GUIStyle _stateStyle;
    private static GUIStyle _centerStyle;
    private static GUIStyle _hintStyle;
    private static GUIStyle _statusStyle;

    public static void Toggle()
    {
        Visible = !Visible;

        if (!Visible)
        {
            _rebinding = null;
            _dragging = false;
        }
    }

    public static void Draw()
    {
        var e = Event.current;
        if (!Visible || e == null)
            return;

        EnsureStyles();

        // Everything is sized relative to a 1080p screen, so the menu stays readable on 1440p / 4K.
        float s = Mathf.Clamp(Screen.height / 1080f, 0.8f, 2f);
        ApplyFontSizes(s);

        float width = 340f * s;
        float pad = 12f * s;
        float headerH = 40f * s;
        float rowH = 38f * s;
        float gap = 8f * s;
        float footerH = 50f * s;

        var features = FeatureRegistry.All;
        float height = headerH + pad + features.Length * (rowH + gap) + footerH;

        // Keep the window on the screen.
        _position.x = Mathf.Clamp(_position.x, 0f, Mathf.Max(0f, Screen.width - width));
        _position.y = Mathf.Clamp(_position.y, 0f, Mathf.Max(0f, Screen.height - height));
        var window = new Rect(_position.x, _position.y, width, height);

        // 1. A key pressed while we wait for a new hotkey.
        if (_rebinding != null && e.type == EventType.KeyDown && e.keyCode != KeyCode.None)
        {
            OnKeyWhileBinding(e.keyCode);
            e.Use();
        }

        // 2. Background and header.
        Fill(window, WindowColor);

        var header = new Rect(window.x, window.y, width, headerH);
        Fill(header, HeaderColor);
        Fill(new Rect(header.x, header.yMax - 2f * s, width, 2f * s), AccentColor);

        GUI.Label(new Rect(header.x + pad, header.y, width * 0.5f, headerH), "AMODUS", _titleStyle);
        GUI.Label(new Rect(header.x, header.y, width - headerH - 4f * s, headerH), "v" + ModInfo.Version, _versionStyle);

        var closeButton = new Rect(header.xMax - headerH + 6f * s, header.y + 6f * s, headerH - 12f * s, headerH - 12f * s);
        if (DrawButton(closeButton, "X", OffColor, _centerStyle))
        {
            Toggle();
            return;
        }

        // The window can be dragged by its header.
        HandleDrag(e, new Rect(header.x, header.y, width - headerH, headerH));

        // 3. One row per feature.
        float y = header.yMax + pad;
        for (int i = 0; i < features.Length; i++)
        {
            DrawRow(features[i], new Rect(window.x + pad, y, width - 2f * pad, rowH), s);
            y += rowH + gap;
        }

        // 4. Hints and status line.
        DrawFooter(new Rect(window.x + pad, y, width - 2f * pad, footerH - 4f * s));
    }

    // ------------------------------------------------------------------ rows

    private static void DrawRow(Feature feature, Rect row, float s)
    {
        var e = Event.current;
        bool listening = _rebinding == feature;

        float bindWidth = 56f * s;
        float spacing = 6f * s;
        var main = new Rect(row.x, row.y, row.width - bindWidth - spacing, row.height);
        var bind = new Rect(main.xMax + spacing, row.y, bindWidth, row.height);

        // The button body: green when the feature is on.
        Fill(main, feature.Enabled ? OnColor : OffColor);
        if (main.Contains(e.mousePosition))
            Fill(main, HoverTint);

        // Name on the left; state and hotkey on the right.
        GUI.Label(new Rect(main.x + 12f * s, main.y, main.width * 0.5f, main.height), feature.Title, _nameStyle);

        string keyText = listening ? "press a key" : "[" + PrettyKey(feature.Hotkey.Value) + "]";
        string state = feature.Enabled ? "ON" : "OFF";
        GUI.Label(new Rect(main.x, main.y, main.width - 12f * s, main.height), state + "   " + keyText, _stateStyle);

        // Right click on the button: choose a new hotkey. This must come before GUI.Button,
        // because GUI.Button also reacts to the right mouse button.
        if (e.type == EventType.MouseDown && e.button == 1 && main.Contains(e.mousePosition))
        {
            StartBinding(feature);
            e.Use();
        }

        // Left click on the button: switch the feature on / off.
        if (GUI.Button(main, string.Empty, GUIStyle.none) && Event.current.button == 0)
        {
            if (listening)
                _rebinding = null;
            else
                feature.Toggle();
        }

        // The small "Bind" button does the same as the right click.
        if (DrawButton(bind, listening ? "..." : "Bind", listening ? AccentColor : OffColor, _centerStyle))
        {
            if (listening)
                _rebinding = null;
            else
                StartBinding(feature);
        }
    }

    private static void DrawFooter(Rect area)
    {
        string first;
        var firstStyle = _hintStyle;

        if (_rebinding != null)
        {
            first = "Press a key for " + _rebinding.Title + "...";
            firstStyle = _statusStyle;
        }
        else if (Time.unscaledTime < _statusUntil)
        {
            first = _status;
            firstStyle = _statusStyle;
        }
        else
        {
            first = "Click: on/off    Right-click / Bind: set key";
        }

        string second = _rebinding != null
            ? "Esc: cancel    Backspace: no hotkey"
            : PrettyKey(AmodusPlugin.MenuKey.Value) + ": hide the menu";

        float half = area.height * 0.5f;
        GUI.Label(new Rect(area.x, area.y, area.width, half), first, firstStyle);
        GUI.Label(new Rect(area.x, area.y + half, area.width, half), second, _hintStyle);
    }

    // ------------------------------------------------------------ rebinding

    private static void StartBinding(Feature feature)
    {
        _rebinding = feature;
        _statusUntil = 0f;
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

    // -------------------------------------------------------------- dragging

    private static void HandleDrag(Event e, Rect dragArea)
    {
        if (e.type == EventType.MouseDown && e.button == 0 && dragArea.Contains(e.mousePosition))
        {
            _dragging = true;
            _dragOffset = e.mousePosition - _position;
            e.Use();
        }
        else if (e.type == EventType.MouseDrag && _dragging)
        {
            _position = e.mousePosition - _dragOffset;
            e.Use();
        }
        else if (e.rawType == EventType.MouseUp)
        {
            _dragging = false;
        }
    }

    // --------------------------------------------------------------- drawing

    /// <summary>A flat button with a hover effect. Returns true when it was clicked with the left mouse button.</summary>
    private static bool DrawButton(Rect rect, string text, Color fill, GUIStyle style)
    {
        Fill(rect, fill);
        if (rect.Contains(Event.current.mousePosition))
            Fill(rect, HoverTint);

        bool clicked = GUI.Button(rect, text, style);
        return clicked && Event.current.button == 0;
    }

    /// <summary>Draws a filled rectangle.</summary>
    private static void Fill(Rect rect, Color color)
    {
        var previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private static void EnsureStyles()
    {
        // GUI.skin can only be used inside OnGUI, so the styles are created on the first draw.
        if (_titleStyle != null)
            return;

        _titleStyle = MakeStyle(TextAnchor.MiddleLeft, FontStyle.Bold, TextColor);
        _versionStyle = MakeStyle(TextAnchor.MiddleRight, FontStyle.Normal, DimTextColor);
        _nameStyle = MakeStyle(TextAnchor.MiddleLeft, FontStyle.Bold, TextColor);
        _stateStyle = MakeStyle(TextAnchor.MiddleRight, FontStyle.Normal, TextColor);
        _centerStyle = MakeStyle(TextAnchor.MiddleCenter, FontStyle.Bold, TextColor);
        _hintStyle = MakeStyle(TextAnchor.MiddleLeft, FontStyle.Normal, DimTextColor);
        _statusStyle = MakeStyle(TextAnchor.MiddleLeft, FontStyle.Bold, AccentColor);
    }

    private static GUIStyle MakeStyle(TextAnchor alignment, FontStyle fontStyle, Color color)
    {
        var style = new GUIStyle(GUI.skin.label);
        style.alignment = alignment;
        style.fontStyle = fontStyle;
        style.wordWrap = false;
        style.normal.textColor = color;
        style.hover.textColor = color;
        style.active.textColor = color;
        style.focused.textColor = color;
        return style;
    }

    private static void ApplyFontSizes(float s)
    {
        _titleStyle.fontSize = Mathf.RoundToInt(18f * s);
        _versionStyle.fontSize = Mathf.RoundToInt(12f * s);
        _nameStyle.fontSize = Mathf.RoundToInt(15f * s);
        _stateStyle.fontSize = Mathf.RoundToInt(13f * s);
        _centerStyle.fontSize = Mathf.RoundToInt(13f * s);
        _hintStyle.fontSize = Mathf.RoundToInt(12f * s);
        _statusStyle.fontSize = Mathf.RoundToInt(12f * s);
    }
}
