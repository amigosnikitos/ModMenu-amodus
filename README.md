# Amodus

**English** · [Русский](README.ru.md)

A small mod menu for **Among Us** (Steam, PC), built as a [BepInEx](https://github.com/BepInEx/BepInEx) plugin.

Press **Delete** in game to show or hide the menu: a dark, flat window with tabs on the left and one row per function.

> Amodus is a fan-made mod. It is not affiliated with or endorsed by Innersloth. Use it in private lobbies with friends: cheating in public games spoils the game for other people.

## Features

| Function | Tab | What it does | Default key |
|----------|-----|--------------|-------------|
| **Noclip** | Game | Walk through walls | `F1` |
| **See ghosts** | Game | While you are alive, you see dead players (ghosts) | `F4` |
| **Chat** | Chat | Keeps the chat button on screen during the round, not only in meetings | `F2` |
| **Ghost chat** | Chat | While you are alive, you read what ghosts write and can answer them. The chat button stays on screen too, so this works without **Chat** | `F3` |
| **Copy lobby code** | Lobby | When you leave a lobby or get thrown out of it (the host left, a kick, a lost connection), the lobby code is copied to the clipboard | on from the start, no key |

Every function is switched on and off on its own. The menu itself opens and closes with `Delete`.

### The menu

- **Click a row** to switch the function on or off. The key of the function is shown at the right end of the row.
- **Changing a key:** click the key in the row (or right-click the row), then press the new key. `Esc` cancels, `Backspace` removes the key.
- **Settings tab:** the size of the menu (`-` and `+` buttons, 60 % to 200 %) and the key that opens the menu.
- The window can be dragged by its top bar.
- Keys, the size, and what is switched on at the start of the game are saved in the config file (see below).

## Install (Steam)

1. In Steam: right-click Among Us, then **Manage**, then **Browse local files**.
2. Download **BepInEx 6 IL2CPP** for **Windows x64** (build **735 or newer**) from <https://builds.bepinex.dev/projects/bepinex_be> and extract it into the game folder, next to `Among Us.exe`. (If the folder contains `UnityCrashHandler32.exe`, the game is 32-bit and you need the x86 build instead.)
3. Start the game once and close it. BepInEx creates its folders.
4. Download `Amodus-vX.Y.Z.zip` from the [Releases](../../releases) page and extract it into the game folder, so that the file `BepInEx/plugins/Amodus.dll` exists. (While there are no releases, take a fresh build instead: the **Actions** tab, the latest green run, the `Amodus-dev-...` archive at the bottom.)
5. Start the game and press **Delete**.

Updating: close the game and replace `BepInEx/plugins/Amodus.dll` with the new file.

Steam Deck / Linux (Proton): set the launch option `WINEDLLOVERRIDES="winhttp=n,b" %command%`.

## Configuration

Settings are stored in `BepInEx/config/com.amodus.menu.cfg`. The file is created on the first start. Everything in it can also be changed from the menu, except the `[Startup]` section.

| Section | Content |
|---------|---------|
| `[General]` | `MenuKey` - the key of the menu, `UiScale` - the size of the menu (`0.6` to `2`, normal is `1`) |
| `[Hotkeys]` | The key of every function. `None` means no key |
| `[Startup]` | Which functions are switched on when the game starts (`true` / `false`) |

Key names are Unity `KeyCode` names (`F1`, `Alpha5`, `LeftControl`, `None`, ...).

## Troubleshooting

- Open `BepInEx/LogOutput.log`. The line `Amodus 0.2.0 loaded` means that the plugin has started.
- No `LogOutput.log` at all: BepInEx is installed in the wrong place (`winhttp.dll` must be next to `Among Us.exe`) or it is the wrong build.
- The plugin stopped working after a game update: it has to be rebuilt for the new game version (see "Updating for a new game version").
- Errors from the plugin are written to the same log, each one only once, marked `Amodus`. A line `Patch ... failed` means that one function could not attach itself to the game; the others still work.
- Lines that start with `UI:` are not errors. They say that a part of the drawing code is missing in this version of the game and the menu uses a simpler way.

## Development

You need the [.NET SDK](https://dotnet.microsoft.com/download) (8 is fine) and an internet connection. You do **not** need the game files: the game libraries come from NuGet (`AmongUs.GameLibs.Steam`).

```
dotnet build src/Amodus/Amodus.csproj -c Release
```

The result is `src/Amodus/bin/Release/net6.0/Amodus.dll`. Copy it to `<game folder>/BepInEx/plugins/`.

### Project layout

```
src/Amodus/
  AmodusPlugin.cs      entry point (the BepInEx plugin), config, applies the patches
  AmodusBehaviour.cs   thin MonoBehaviour: Update / LateUpdate / FixedUpdate / OnGUI
  Hotkeys.cs           hotkey handling
  Clipboard.cs         puts text on the clipboard
  GameState.cs         small helpers about the game state
  Features/            one class per function (and FeatureRegistry, the list)
  Patches/             Harmony patches
  Ui/MenuWindow.cs     the menu: mouse, keys, layout
  Ui/Painter.cs        the only code that draws (only the parts of IMGUI that the game keeps)
```

### Adding a function

1. Create a class in `src/Amodus/Features/` that derives from `Feature`. Set `Id` and `Title`. Optionally set `Category` (the menu tab; a new name makes a new tab), `Description` (the line under the title), `DefaultKey` and `EnabledByDefault`. Override `OnEnabled`, `OnDisabled`, `OnUpdate`, `OnLateUpdate` or `OnFixedUpdate` as needed.
2. Add it to `FeatureRegistry` (a static field and an entry in `All`).
3. If the function needs a Harmony patch, put a class with `[HarmonyPatch]` into `src/Amodus/Patches/`. It is applied automatically; if a patch fails, only the functions that need it stop working.

That is all. The row in the menu, the hotkey, the config entries and the rebinding appear automatically.

Among Us is an IL2CPP game: the parts of Unity that the game itself never uses are cut out of it, and calling such a part fails with `Method unstripping failed`. This is why everything that draws the menu lives in `Ui/Painter.cs` and uses only the parts of IMGUI that survived. When you add a new call into Unity or the game, test it in the game, not only in the build.

### Updating for a new game version

Set the newest version of the `AmongUs.GameLibs.Steam` package in `src/Amodus/Amodus.csproj` and build again.

### Releasing

1. Change `Version` in `src/Amodus/ModInfo.cs`.
2. Run `git tag v0.2.0` and `git push origin v0.2.0`.
3. GitHub Actions builds the plugin and attaches `Amodus-v0.2.0.zip` to a new release.

## Notes

- This is an early version (0.2.0).
- **See ghosts** and **Ghost chat** matter only while you are alive: a dead player sees ghosts and their chat anyway.
- The game itself hides the messages of ghosts from living players; **Ghost chat** switches that off.
- Players without the mod may see your chat messages only later, in the lobby or in a meeting.
- The lobby code is copied once, at the moment you leave. The last code is written under the name of the function in the menu.
- The safest hotkeys are the F-keys. A letter key could also fire while you type in the chat, because the check for an open chat box is best-effort.
- The game also reads the mouse, so clicking the menu may make the character walk towards the cursor. Hotkeys avoid that.
- Do not switch noclip off while you are inside a wall or outside the map.

## License

[MIT](LICENSE)
