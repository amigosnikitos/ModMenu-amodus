# Amodus

**English** · [Русский](README.ru.md)

A small mod menu for **Among Us** (Steam, PC), built as a [BepInEx](https://github.com/BepInEx/BepInEx) plugin.

Press **Delete** in game to show or hide the menu. Every function has a button, and the hotkey of the function is shown right on its button.

> Amodus is a fan-made mod. It is not affiliated with or endorsed by Innersloth. Use it in private lobbies with friends: cheating in public games spoils the game for other people.

## Features

| Function | What it does | Default key |
|----------|--------------|-------------|
| **Noclip** | Walk through walls | `F1` |
| **Chat** | Keeps the chat available all the time, so you can talk during the round and not only in meetings | `F2` |

The menu itself opens and closes with `Delete`.

### Changing hotkeys

- Right-click a function button (or click **Bind** next to it), then press the key you want.
- `Esc` cancels, `Backspace` removes the hotkey.
- Hotkeys are saved in the config file (see below).

## Install (Steam)

1. In Steam: right-click Among Us, then **Manage**, then **Browse local files**.
2. Download **BepInEx 6 IL2CPP** for **Windows x64** (build **735 or newer**) from <https://builds.bepinex.dev/projects/bepinex_be> and extract it into the game folder, next to `Among Us.exe`. (If the folder contains `UnityCrashHandler32.exe`, the game is 32-bit and you need the x86 build instead.)
3. Start the game once and close it. BepInEx creates its folders.
4. Download `Amodus-vX.Y.Z.zip` from the [Releases](../../releases) page and extract it into the game folder, so that the file `BepInEx/plugins/Amodus.dll` exists.
5. Start the game and press **Delete**.

Steam Deck / Linux (Proton): set the launch option `WINEDLLOVERRIDES="winhttp=n,b" %command%`.

## Configuration

Settings are stored in `BepInEx/config/com.amodus.menu.cfg`. The file is created on the first start. Hotkeys can be edited there too; key names are Unity `KeyCode` names (`F1`, `Alpha5`, `LeftControl`, `None`, ...).

## Troubleshooting

- Open `BepInEx/LogOutput.log`. The line `Amodus 0.1.0 loaded` means that the plugin has started.
- No `LogOutput.log` at all: BepInEx is installed in the wrong place (`winhttp.dll` must be next to `Among Us.exe`) or it is the wrong build.
- The plugin stopped working after a game update: it has to be rebuilt for the new game version (see "Updating for a new game version").
- Errors from the plugin are written to the same log, each one only once.

## Development

You need the [.NET SDK](https://dotnet.microsoft.com/download) (8 is fine) and an internet connection. You do **not** need the game files: the game libraries come from NuGet (`AmongUs.GameLibs.Steam`).

```
dotnet build src/Amodus/Amodus.csproj -c Release
```

The result is `src/Amodus/bin/Release/net6.0/Amodus.dll`. Copy it to `<game folder>/BepInEx/plugins/`.

### Project layout

```
src/Amodus/
  AmodusPlugin.cs      entry point (the BepInEx plugin)
  AmodusBehaviour.cs   thin MonoBehaviour: Update / FixedUpdate / OnGUI
  Hotkeys.cs           hotkey handling
  GameState.cs         small helpers about the game state
  Features/            one class per function
  Patches/             Harmony patches
  Ui/MenuWindow.cs     the menu (IMGUI)
```

### Adding a function

1. Create a class in `src/Amodus/Features/` that derives from `Feature`. Set `Id` and `Title`, optionally `DefaultKey`, and override `OnEnabled`, `OnDisabled`, `OnUpdate` or `OnFixedUpdate` as needed.
2. Add it to `FeatureRegistry` (a static field and an entry in `All`).

That is all. The button, the hotkey, the config entry and the rebinding appear automatically.

### Updating for a new game version

Set the newest version of the `AmongUs.GameLibs.Steam` package in `src/Amodus/Amodus.csproj` and build again.

### Releasing

1. Change `Version` in `src/Amodus/ModInfo.cs`.
2. Run `git tag v0.1.0` and `git push origin v0.1.0`.
3. GitHub Actions builds the plugin and attaches `Amodus-v0.1.0.zip` to a new release.

## Notes

- This is an early version (0.1.0).
- The safest hotkeys are the F-keys. A letter key could also fire while you type in the chat, because the check for an open chat box is best-effort.
- The game also reads the mouse, so clicking the menu may make the character walk towards the cursor. Hotkeys avoid that.
- Players without the mod may see your chat messages only later, in the lobby or in a meeting.
- Do not switch noclip off while you are inside a wall or outside the map.

## License

[MIT](LICENSE)
