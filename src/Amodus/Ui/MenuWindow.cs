using System;
using System.Collections.Generic;
using Amodus.Features;
using UnityEngine;

namespace Amodus.Ui;

/// <summary>
/// The Amodus window: a dark, flat window with a title bar, tabs on the left and one row per feature on the right.
/// It is split in two on purpose: <see cref="Update"/> reads the mouse and the keyboard from Unity's Input
/// (clicks, dragging, picking a new hotkey), <see cref="Draw"/> only draws, through <see cref="Painter"/>.
/// All sizes are in "layout units"; <see cref="Scale"/> turns them into pixels.
/// </summary>
internal static class MenuWindow
{
    public static bool Visible { get; private set; }

    /// <summary>True while the menu waits for the player to press a new key.</summary>
    public static bool IsRebinding => _rebindFeature != null || _rebindMenuKey;

    // ---- size ----
    private const float BaseScale = 1.25f;      // layout unit -> pixels on a 1080p screen, at the normal menu size
    private const float WindowWidth = 560f;
    private const float TitleHeight = 34f;
    private const float FooterHeight = 26f;
    private const float SidebarWidth = 132f;
    private const float Pad = 10f;
    private const float RowHeight = 50f;
    private const float RowGap = 6f;
    private const float TabHeight = 34f;
    private const float TabGap = 2f;
    private const float ChipWidth = 66f;
    private const float ChipHeight = 24f;
    private const int MinRows = 5;

    private const float TitleFont = 17f;
    private const float BodyFont = 15f;
    private const float SmallFont = 12.5f;
    private const float ChipFont = 13f;

    private const string SettingsTab = "Settings";
    private const int SettingsRows = 2;         // "Interface size" and "Menu key"

    // ---- colours ----
    private static readonly Color Panel = Rgb(0.067f, 0.067f, 0.078f);
    private static readonly Color TitleBar = Rgb(0.043f, 0.043f, 0.051f);
    private static readonly Color Sidebar = Rgb(0.055f, 0.055f, 0.065f);
    private static readonly Color SidebarHover = Rgb(0.082f, 0.082f, 0.097f);
    private static readonly Color Row = Rgb(0.105f, 0.105f, 0.122f);
    private static readonly Color RowHover = Rgb(0.135f, 0.135f, 0.157f);
    private static readonly Color Chip = Rgb(0.17f, 0.17f, 0.2f);
    private static readonly Color ChipHover = Rgb(0.23f, 0.23f, 0.27f);
    private static readonly Color TrackOff = Rgb(0.22f, 0.22f, 0.26f);
    private static readonly Color KnobOff = Rgb(0.62f, 0.62f, 0.68f);
    private static readonly Color Accent = Rgb(0.58f, 0.43f, 1f);
    private static readonly Color CloseHover = Rgb(0.78f, 0.22f, 0.27f);
    private static readonly Color White = Rgb(1f, 1f, 1f);
    private static readonly Color TextBright = Rgb(0.95f, 0.95f, 0.97f);
    private static readonly Color TextMid = Rgb(0.78f, 0.78f, 0.84f);
    private static readonly Color TextDim = Rgb(0.56f, 0.56f, 0.64f);

    // ---- state ----
    private static bool _built;
    private static string[] _tabs;
    private static Feature[][] _tabFeatures;
    private static float _height;
    private static int _tab;

    private static bool _placed;
    private static float _x;
    private static float _y;
    private static bool _dragging;
    private static float _dragDx;
    private static float _dragDy;
    private static float _mx = -10000f;
    private static float _my = -10000f;

    private static Feature _rebindFeature;
    private static bool _rebindMenuKey;
    private static string _status = string.Empty;
    private static float _statusUntil;
    private static KeyCode[] _bindableKeys;

    public static void Toggle()
    {
        Visible = !Visible;

        if (!Visible)
        {
            CancelBinding();
            _dragging = false;
        }
    }

    private static Color Rgb(float r, float g, float b)
    {
        return new Color(r, g, b, 1f);
    }

    // ------------------------------------------------------------------ layout

    /// <summary>Layout units to pixels. Follows the screen height, so the menu looks the same on 1080p, 1440p and 4K.</summary>
    private static float Scale()
    {
        float s = Screen.height / 1080f;
        if (s < 0.5f)
            s = 0.5f;
        else if (s > 3f)
            s = 3f;

        return s * BaseScale * AmodusPlugin.UiScale.Value;
    }

    /// <summary>Groups the features into tabs and works out the height of the window. Done once.</summary>
    private static void Build()
    {
        if (_built)
            return;

        _built = true;

        var names = new List<string>();
        foreach (var feature in FeatureRegistry.All)
        {
            if (!names.Contains(feature.Category))
                names.Add(feature.Category);
        }

        if (!names.Contains(SettingsTab))
            names.Add(SettingsTab);

        _tabs = names.ToArray();
        _tabFeatures = new Feature[_tabs.Length][];

        int rows = MinRows;
        for (int i = 0; i < _tabs.Length; i++)
        {
            var list = new List<Feature>();
            foreach (var feature in FeatureRegistry.All)
            {
                if (feature.Category == _tabs[i])
                    list.Add(feature);
            }

            _tabFeatures[i] = list.ToArray();

            int count = list.Count + (_tabs[i] == SettingsTab ? SettingsRows : 0);
            if (count > rows)
                rows = count;
        }

        // The window is as high as its fullest tab (but never lower than MinRows rows).
        _height = TitleHeight + 1f + Pad + rows * RowHeight + (rows - 1) * RowGap + Pad + FooterHeight;
    }

    /// <summary>Puts the window in the middle of the screen the first time, and keeps it on the screen after that.</summary>
    private static void KeepOnScreen(float scale)
    {
        float maxX = Math.Max(0f, Screen.width / scale - WindowWidth);
        float maxY = Math.Max(0f, Screen.height / scale - _height);

        if (!_placed)
        {
            _x = maxX * 0.5f;
            _y = maxY * 0.5f;
            _placed = true;
        }

        _x = Math.Min(Math.Max(_x, 0f), maxX);
        _y = Math.Min(Math.Max(_y, 0f), maxY);
    }

    private static Rect WindowRect() => new Rect(_x, _y, WindowWidth, _height);

    private static Rect TitleRect(Rect w) => new Rect(w.x, w.y, WindowWidth, TitleHeight);

    private static Rect CloseRect(Rect w) => new Rect(w.x + WindowWidth - TitleHeight, w.y, TitleHeight, TitleHeight);

    private static float BodyTop(Rect w) => w.y + TitleHeight + 1f + Pad;

    private static Rect TabRect(Rect w, int index)
    {
        return new Rect(w.x, BodyTop(w) + index * (TabHeight + TabGap), SidebarWidth, TabHeight);
    }

    private static Rect RowRect(Rect w, int index)
    {
        return new Rect(w.x + SidebarWidth + Pad, BodyTop(w) + index * (RowHeight + RowGap), WindowWidth - SidebarWidth - 2f * Pad, RowHeight);
    }

    private static Rect ChipRect(Rect row)
    {
        return new Rect(row.x + row.width - ChipWidth - 12f, row.y + (RowHeight - ChipHeight) * 0.5f, ChipWidth, ChipHeight);
    }

    private static Rect SwitchRect(Rect row)
    {
        return new Rect(row.x + 14f, row.y + (RowHeight - 18f) * 0.5f, 34f, 18f);
    }

    // The three small controls of the "Interface size" row: [-] 100% [+]
    private static Rect PlusRect(Rect row) => new Rect(row.x + row.width - 12f - 28f, row.y + (RowHeight - ChipHeight) * 0.5f, 28f, ChipHeight);

    private static Rect ValueRect(Rect row) => new Rect(row.x + row.width - 12f - 28f - 4f - 52f, row.y + (RowHeight - ChipHeight) * 0.5f, 52f, ChipHeight);

    private static Rect MinusRect(Rect row) => new Rect(row.x + row.width - 12f - 28f - 4f - 52f - 4f - 28f, row.y + (RowHeight - ChipHeight) * 0.5f, 28f, ChipHeight);

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

        Build();
        float s = Scale();
        KeepOnScreen(s);

        // Unity's mouse position starts in the bottom left corner, the menu's in the top left one.
        var mouse = Input.mousePosition;
        _mx = mouse.x / s;
        _my = (Screen.height - mouse.y) / s;

        if (IsRebinding)
            PollKeyForBinding();

        if (_dragging)
        {
            if (Input.GetMouseButton(0))
            {
                _x = _mx - _dragDx;
                _y = _my - _dragDy;
                KeepOnScreen(s);
            }
            else
            {
                _dragging = false;
            }
        }

        bool left = Input.GetMouseButtonDown(0);
        bool right = Input.GetMouseButtonDown(1);
        if (!left && !right)
            return;

        var window = WindowRect();
        if (!Hit(window, _mx, _my))
            return;

        if (left)
        {
            if (Hit(CloseRect(window), _mx, _my))
            {
                Toggle();
                return;
            }

            // The window can be dragged by its title bar.
            if (Hit(TitleRect(window), _mx, _my))
            {
                _dragging = true;
                _dragDx = _mx - _x;
                _dragDy = _my - _y;
                return;
            }

            for (int i = 0; i < _tabs.Length; i++)
            {
                if (Hit(TabRect(window, i), _mx, _my))
                {
                    _tab = i;
                    CancelBinding();
                    return;
                }
            }
        }

        ClickRows(window, left, right);
    }

    private static void ClickRows(Rect window, bool left, bool right)
    {
        var features = _tabFeatures[_tab];

        for (int i = 0; i < features.Length; i++)
        {
            var row = RowRect(window, i);
            if (!Hit(row, _mx, _my))
                continue;

            var feature = features[i];
            bool onKey = Hit(ChipRect(row), _mx, _my);

            if (right || (left && onKey))
            {
                // Right click on the row, or a click on the key: choose a new hotkey.
                if (_rebindFeature == feature)
                    CancelBinding();
                else
                    StartBinding(feature);
            }
            else if (IsRebinding)
            {
                CancelBinding();
            }
            else
            {
                feature.Toggle();
            }

            return;
        }

        if (_tabs[_tab] != SettingsTab)
            return;

        // The built-in rows of the Settings tab come after the features of that tab.
        var sizeRow = RowRect(window, features.Length);
        if (left && Hit(sizeRow, _mx, _my))
        {
            if (Hit(MinusRect(sizeRow), _mx, _my))
                ChangeScale(-0.1f);
            else if (Hit(PlusRect(sizeRow), _mx, _my))
                ChangeScale(0.1f);

            return;
        }

        var keyRow = RowRect(window, features.Length + 1);
        if ((left || right) && Hit(keyRow, _mx, _my))
        {
            if (_rebindMenuKey)
                CancelBinding();
            else
                StartMenuKeyBinding();
        }
    }

    private static void ChangeScale(float delta)
    {
        float value = (float)Math.Round((AmodusPlugin.UiScale.Value + delta) * 10f) / 10f;
        value = Math.Min(Math.Max(value, 0.6f), 2f);
        AmodusPlugin.UiScale.Value = value;
    }

    // ------------------------------------------------------------------ drawing

    /// <summary>Called from OnGUI. Draws the window; does nothing while the menu is hidden.</summary>
    public static void Draw()
    {
        if (!Visible)
            return;

        Build();
        Painter.Begin(Scale());

        try
        {
            DrawWindow();
        }
        finally
        {
            Painter.End();
        }
    }

    private static void DrawWindow()
    {
        var w = WindowRect();

        // background, title bar, a thin accent line under it, sidebar, footer
        Painter.Fill(w, Panel);
        Painter.Fill(TitleRect(w), TitleBar);
        Painter.Fill(new Rect(w.x, w.y + TitleHeight, WindowWidth, 1f), Accent);
        Painter.Fill(new Rect(w.x, w.y + TitleHeight + 1f, SidebarWidth, _height - TitleHeight - 1f - FooterHeight), Sidebar);
        Painter.Fill(new Rect(w.x, w.y + _height - FooterHeight, WindowWidth, FooterHeight), TitleBar);

        Painter.Text(new Rect(w.x + 14f, w.y, 90f, TitleHeight), "AMODUS", Accent, TitleFont, Painter.Align.Left);
        Painter.Text(new Rect(w.x + 102f, w.y + 2f, 120f, TitleHeight), "v" + ModInfo.Version, TextDim, SmallFont, Painter.Align.Left);

        var close = CloseRect(w);
        bool closeHover = Hit(close, _mx, _my);
        if (closeHover)
            Painter.Fill(close, CloseHover);

        Painter.Text(close, "X", closeHover ? White : TextDim, BodyFont, Painter.Align.Center);

        DrawTabs(w);
        DrawRows(w);
        DrawFooter(w);
    }

    private static void DrawTabs(Rect w)
    {
        for (int i = 0; i < _tabs.Length; i++)
        {
            var r = TabRect(w, i);
            bool selected = i == _tab;

            if (selected)
            {
                Painter.Fill(r, Row);
                Painter.Fill(new Rect(r.x, r.y, 3f, r.height), Accent);
            }
            else if (Hit(r, _mx, _my))
            {
                Painter.Fill(r, SidebarHover);
            }

            Painter.Text(new Rect(r.x + 16f, r.y, r.width - 20f, r.height), _tabs[i], selected ? TextBright : TextDim, BodyFont, Painter.Align.Left);
        }
    }

    private static void DrawRows(Rect w)
    {
        var features = _tabFeatures[_tab];

        for (int i = 0; i < features.Length; i++)
            DrawFeatureRow(RowRect(w, i), features[i]);

        if (_tabs[_tab] == SettingsTab)
        {
            DrawSizeRow(RowRect(w, features.Length));
            DrawMenuKeyRow(RowRect(w, features.Length + 1));
        }
    }

    private static void DrawFeatureRow(Rect row, Feature feature)
    {
        Painter.Fill(row, Hit(row, _mx, _my) ? RowHover : Row);
        DrawSwitch(SwitchRect(row), feature.Enabled);

        float textX = row.x + 62f;
        float textWidth = row.width - 62f - ChipWidth - 24f;
        Painter.Text(new Rect(textX, row.y + 5f, textWidth, 22f), feature.Title, feature.Enabled ? TextBright : TextMid, BodyFont, Painter.Align.Left);
        Painter.Text(new Rect(textX, row.y + 27f, textWidth, 18f), feature.Description, TextDim, SmallFont, Painter.Align.Left);

        bool listening = _rebindFeature == feature;
        DrawChip(ChipRect(row), listening ? "..." : PrettyKey(feature.Hotkey.Value), listening);
    }

    private static void DrawSizeRow(Rect row)
    {
        Painter.Fill(row, Hit(row, _mx, _my) ? RowHover : Row);

        Painter.Text(new Rect(row.x + 16f, row.y + 5f, row.width - 160f, 22f), "Interface size", TextBright, BodyFont, Painter.Align.Left);
        Painter.Text(new Rect(row.x + 16f, row.y + 27f, row.width - 160f, 18f), "Size of this menu", TextDim, SmallFont, Painter.Align.Left);

        DrawChip(MinusRect(row), "-", false);
        Painter.Text(ValueRect(row), Math.Round(AmodusPlugin.UiScale.Value * 100f) + "%", TextBright, ChipFont, Painter.Align.Center);
        DrawChip(PlusRect(row), "+", false);
    }

    private static void DrawMenuKeyRow(Rect row)
    {
        Painter.Fill(row, Hit(row, _mx, _my) ? RowHover : Row);

        Painter.Text(new Rect(row.x + 16f, row.y + 5f, row.width - 120f, 22f), "Menu key", TextBright, BodyFont, Painter.Align.Left);
        Painter.Text(new Rect(row.x + 16f, row.y + 27f, row.width - 120f, 18f), "Shows and hides this menu", TextDim, SmallFont, Painter.Align.Left);

        DrawChip(ChipRect(row), _rebindMenuKey ? "..." : PrettyKey(AmodusPlugin.MenuKey.Value), _rebindMenuKey);
    }

    private static void DrawSwitch(Rect track, bool on)
    {
        Painter.Fill(track, on ? Accent : TrackOff);

        float knob = track.height - 6f;
        float x = on ? track.x + track.width - knob - 3f : track.x + 3f;
        Painter.Fill(new Rect(x, track.y + 3f, knob, knob), on ? White : KnobOff);
    }

    private static void DrawChip(Rect r, string text, bool active)
    {
        Painter.Fill(r, active ? Accent : (Hit(r, _mx, _my) ? ChipHover : Chip));
        Painter.Text(r, text, active ? White : TextMid, ChipFont, Painter.Align.Center);
    }

    private static void DrawFooter(Rect w)
    {
        string text;
        var color = TextDim;

        if (_rebindFeature != null)
        {
            text = "Press a key for " + _rebindFeature.Title + "     Esc: cancel     Backspace: no key";
            color = Accent;
        }
        else if (_rebindMenuKey)
        {
            text = "Press the new key for the menu     Esc: cancel";
            color = Accent;
        }
        else if (Time.unscaledTime < _statusUntil)
        {
            text = _status;
            color = Accent;
        }
        else
        {
            text = "Click a row: on / off     Right-click a row or click its key: change the key";
        }

        Painter.Text(new Rect(w.x + 14f, w.y + _height - FooterHeight, WindowWidth - 28f, FooterHeight), text, color, SmallFont, Painter.Align.Left);
    }

    // ------------------------------------------------------------ rebinding

    private static void StartBinding(Feature feature)
    {
        _rebindFeature = feature;
        _rebindMenuKey = false;
        _statusUntil = 0f;
    }

    private static void StartMenuKeyBinding()
    {
        _rebindFeature = null;
        _rebindMenuKey = true;
        _statusUntil = 0f;
    }

    private static void CancelBinding()
    {
        _rebindFeature = null;
        _rebindMenuKey = false;
    }

    /// <summary>Looks for a key that was pressed this frame while the menu waits for a new key.</summary>
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
        if (key == KeyCode.Escape)
        {
            CancelBinding();
            return;
        }

        var feature = _rebindFeature;

        if (_rebindMenuKey)
        {
            if (key == KeyCode.Backspace)
            {
                SetStatus("The menu needs a key");
                return;
            }

            var owner = FeatureWithHotkey(key, null);
            if (owner != null)
            {
                SetStatus(PrettyKey(key) + " is used by " + owner.Title);
                return;
            }

            AmodusPlugin.MenuKey.Value = key;
            FinishBinding("Menu key: " + PrettyKey(key));
            return;
        }

        if (feature == null)
            return;

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

        var other = FeatureWithHotkey(key, feature);
        if (other != null)
        {
            SetStatus(PrettyKey(key) + " is used by " + other.Title);
            return;
        }

        feature.Hotkey.Value = key;
        FinishBinding(feature.Title + ": " + PrettyKey(key));
    }

    private static Feature FeatureWithHotkey(KeyCode key, Feature except)
    {
        var all = FeatureRegistry.All;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != except && all[i].Hotkey.Value == key)
                return all[i];
        }

        return null;
    }

    private static void FinishBinding(string message)
    {
        CancelBinding();
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
