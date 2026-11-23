# RetroSchematic viewer

A window that shows a sheet file the way `retroschematic render` writes it, and lets you click
on it: a part, a gate or a signal name shows what it is in the side panel. The drawing on screen
and the SVG file come from the same layout code (`RetroSchematic.Render/SheetLayout.cs`), so they
always match.

## Start it

```powershell
# from the src folder
dotnet run --project RetroSchematic.Desktop -- sheets\nord-10\2A50-sync-modem-buffer-1050.json
```

The program is `RetroSchematic.exe`. With no file named it opens the sheet that was open last time.

## Using it

| Do this | To get |
|---|---|
| File > Open sheet | open another sheet file (`*.json`) |
| File > Save as SVG | the drawing as shown, as an SVG file |
| View > ND 1976 symbols / Modern symbols | switch the symbol set; what is picked stays picked |
| View > Fit to window | the whole sheet in view |
| View > Theme | Light, Dark, Solarized Dark, Nord, Retro Green, Amber CRT, Synthwave '84, High Contrast |
| Mouse wheel | zoom round the pointer |
| Right or middle button drag | move the sheet |
| Left click | pick a part, gate or signal; its details show on the right |
| Parts / Signals lists | pick by name |

Colours: what was READ off the drawing is in the theme's ink colour, anything INFERRED is in
its own colour, and the picked item is in the theme's selection colour.

## Themes

The themes and their colours are RetroCommander's, so both programs look the same under one
theme name. `Themes/ThemeManager.cs` adds six colours for the drawing itself (paper, ink, chip
frame, inferred, note, picked item). The choice is saved in `%APPDATA%\RetroSchematic\ui-settings.json`.

## Tests

`tests/RetroSchematic.Desktop.Tests` opens the real window without a screen, draws it, and
counts pixels: the drawing area must show the theme's paper and ink colours, and must change
when the theme changes. The window's logic (picking, details text) is tested without a window
in `tests/RetroSchematic.Tests/SheetSessionTests.cs`.
