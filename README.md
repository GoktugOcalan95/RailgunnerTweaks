# RailgunnerTweaks

Quality-of-life tweaks for Railgunner.

Every feature can be turned off individually.

## Features

### Variable scope zoom

While scoped in, scroll the mouse wheel to switch between zoom levels. Default and alternate snipers can have different zooms:

| Scope | Level 1 | Level 2 | Level 3 | Vanilla (at 60 FOV) |
|---|---|---|---|---|
| Default Sniper | 2x | 4x | 8x | about 3.6x |
| Alternate Sniper | 2x | 3x | 5x | about 2.2x |

- Zoom amounts are true magnification relative to your normal unscoped view, so they stay accurate with any FOV setting or FOV mod.
- Levels can be changed, and each one can be turned off. Use one, two or three levels per scope.

### Consistent scoped sensitivity

- **0% monitor match:** while scoped, mouse sensitivity is scaled by focal length (also known as 0% monitor match), so aiming feels the same at every zoom level. The game's default scaling is linear in FOV, which feels faster the further you zoom in. Unscoped sensitivity is never changed.
- **Scoped sensitivity percentage:** an extra multiplier for scoped sensitivity. It works independently from the 0% monitor match.

### Cleaner HUD

- Hide the decorative yellow frame, status text and rotating icon around Railgunner's crosshair, including the variant that appears when your primary is replaced. The center brackets stay.
- Hide the pink frame and zoom text inside the default secondary's scope. The center dot, reticle and weak point markers stay.

## Configuration

Settings are available in the in-game mod options menu when [Risk Of Options](https://thunderstore.io/package/Rune580/Risk_Of_Options/) is installed (recommended, not required). Otherwise, edit `BepInEx/config/Bishey.RailgunnerTweaks.cfg`.

**General**

| Setting | Default | Description |
|---|---|---|
| HideCrosshairFlavor | On | Hides the yellow frame, text and icon around the crosshair. |
| HideDefaultScopeFrame | On | Hides the pink frame and zoom text in the default secondary's scope. |
| EnableVariableZoom | On | Mouse-wheel zoom levels while scoped. |
| InvertScroll | Off | When on, scrolling down zooms in. |
| ZoomTransitionTime | 0.1 | Seconds a zoom change takes. 0 is instant. |
| ZeroMonitorMatch | On | 0% monitor match scaling for scoped sensitivity. |
| ScopedSensitivityPercent | 100 | Extra scoped sensitivity multiplier. 100 means no change. |

**Zoom - Default** and **Zoom - Alternate**

Each level has an on/off toggle and a zoom amount from 1x to 10x. Levels are always sorted by zoom amount, so scrolling up zooms in regardless of the order they're entered in, and duplicate values count as one level. A scope starts on its middle level, or the higher one when only two are enabled.

Most settings apply immediately. HUD settings take effect the next time the crosshair is redrawn, such as after scoping in and out.

## Compatibility

- **Client-side.** Works in multiplayer, and other players don't need the mod.
- **Only affects Railgunner.** Other survivors are untouched.
- **Other scope sensitivity mods** (such as ScopeSensitivity) overlap with the sensitivity features. Using both may stack their effects.
- **Mods that change Railgunner's scope zoom** may conflict with the variable zoom feature.
