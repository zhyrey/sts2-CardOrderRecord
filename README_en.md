# Card Order Record

[中文](README.md)

A combat card-order tracking mod for *Slay the Spire 2*.

This mod adds two buttons to the pause menu:

- `Quick Restart`: quickly reload the current combat.
- `Card Order`: open an in-combat card-order viewer.

The `Card Order` screen lists combat card events by turn, including draws, played cards, exhausts, discards, generated cards, retained cards, enchantments, cost changes, and pile movement. The right-side `History` list keeps previous attempts from the current session so you can compare different lines after using Quick Restart.

## Features

- Adds a `Quick Restart` button to the pause menu.
- Adds a `Card Order` button to the pause menu.
- Records card events by turn during combat.
- Uses a card display style similar to the game's history/compendium screens.
- Lets you click recorded cards to open the game's built-in card inspect screen.
- Adds a right-side history list: `Current`, `History 1`, `History 2`, and so on.
- Filters stale events from the previous combat during Quick Restart so old draw events do not leak into the new record.
- DLL-only mod with a manifest JSON. No `.pck` file is required.

## Installation

1. Open this project's GitHub Releases page.
2. Download the latest `CardOrderRecord-vX.X.X.zip`.
3. Extract it. You should get a `CardOrderRecord` folder.
4. Put the whole `CardOrderRecord` folder into the game's `mods` folder.

The final layout should look like this:

```text
Slay the Spire 2/
  mods/
    CardOrderRecord/
      CardOrderRecord.dll
      CardOrderRecord.json
```

If the game directory does not already contain a `mods` folder, create it manually.

When the game asks to enter Modded mode, confirm it. You can enable or disable the mod from the game's mod/settings UI.

## Usage

1. Enter a combat.
2. Open the pause menu.
3. Click `Card Order` to view the current combat's card order.
4. On the right side of the `Card Order` screen, click `History` to expand previous records.
5. Click `Current` to view the current combat, or click `History 1`, `History 2`, etc. to view previous attempts.
6. Click a recorded card to inspect it.
7. Use the game's built-in back button or cancel/back input to close the screen.

`Quick Restart` is placed above `Card Order`. It reloads the game's autosave and restarts the current combat. It is intended for single-player runs.

## Recorded Events

The current version tries to record card events that are meaningful for reviewing play order:

- `Played`: a card was played.
- `Drew`: a card was drawn.
- `Discarded`: a card was discarded.
- `Exhausted`: a card was exhausted.
- `Generated`: a card was generated.
- `Retained`: a card was retained.
- `Enchanted`: a card received an enchantment.
- `Cost`: a card's cost changed.
- `Moved` / `To Hand` / `To Draw` / `To Discard`: a card moved between piles.
- `Upgraded` / `Downgraded`: a card was upgraded or downgraded.
- `Keyword+` / `Keyword-`: a keyword was applied or removed.
- `Afflicted` / `Afflict-`: an affliction was applied or cleared.
- `Enchant-`: an enchantment was cleared.

Repeated system draws and repeated events from the same source are grouped into compact rows where possible.

## Notes

- History is stored in memory only. It is cleared when the game closes.
- The mod is marked as `affects_gameplay: false`; it is intended as an information and review tool.
- `Quick Restart` depends on the game's autosave. If no autosave exists, the button may be unavailable or unable to restart.
- `Quick Restart` is designed around single-player flow and is not recommended for multiplayer.
- *Slay the Spire 2* is still changing, so internal API changes may require future compatibility updates.

## Building From Source

You need a working *Slay the Spire 2* C# modding setup and the .NET SDK.

Windows:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Build only, without copying to the game's `mods` folder:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1 -SkipModCopy
```

Linux / macOS:

```bash
./build.sh
```

The build scripts try to auto-detect the Steam Library that contains `Slay the Spire 2`. If detection fails, pass the game directory or Steam Library path manually.

## Credits and References

- [freude916/sts2-quickRestart](https://github.com/freude916/sts2-quickRestart): the Quick Restart button and autosave reload flow were inspired by this project.
- [Slay the Spire 2 Modding Tutorials](https://tutorials.sts2modding.com/): project structure, Harmony patching, local mod loading, and development workflow references.

## License

This project is released under the [MIT License](LICENSE).
