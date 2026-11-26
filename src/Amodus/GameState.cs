using UnityEngine;

namespace Amodus;

/// <summary>Small questions about what the game is doing right now.</summary>
internal static class GameState
{
    /// <summary>
    /// True while the chat text box has the keyboard focus.
    /// The members are looked up by name at runtime (see <see cref="Reflect"/>) on purpose: if a game update
    /// renames one of them, hotkeys simply keep working instead of the whole mod failing to load.
    /// </summary>
    public static bool IsTypingInChat()
    {
        var hud = HudManager.Instance;
        if (hud == null)
            return false;

        var chat = hud.Chat;
        if (chat == null)
            return false;

        var field = Reflect.Get(chat, "freeChatField");
        var textArea = Reflect.Get(field, "textArea");
        if (textArea == null)
            return false;

        if (Reflect.Get(textArea, "hasFocus") is not bool hasFocus || !hasFocus)
            return false;

        // A hidden text box cannot receive typing.
        return Reflect.Get(textArea, "gameObject") is not GameObject box || box.activeInHierarchy;
    }
}
