# Mega Hub Plus: Build Status

Last updated: 2026-11-24. It builds and runs on Jake's Windows laptop.
Fixed along the way: Program.cs startup, and RollForward=LatestMajor so it runs on the installed .NET.

## Built and TESTED (Claude, on the laptop)
- Builds and opens from "Run Mega Hub Plus.bat"
- Startup picker shows three looks, click picks one
- Console Row look opens, shows tiles, Play button, last-played panel
- Blades look opens (side panels, Recent Games, Library tiles, hints)
- Wide Home look opens (colored side tabs, wide featured tile, tall tiles, Library strip)
- Settings opens with S, switches look, switches tile shape, Esc goes back
- Left/Right arrows move the selection (checked in Wide Home)

- Settings > Power: Update and Uninstall buttons (placement is temporary, will move into columns later)
- Uninstall warning box shows and Cancel works (confirm NOT tried, on purpose)
- Settings are remembered between runs (look and tile shape came back after restart)

- Steam reading is written and runs without crashing; Settings > Games shows what it found

## Built but UNTESTED
- Steam game list, cover art and Play (steam://rungameid) - this laptop has no Steam installed, so it falls back to placeholder games
- Enter key launches the selected game
- Uninstall confirm: removes saved data; removes program files only for a real install, never a dev build
- Update button only shows "Not connected yet" until the project is on GitHub
- Picker keyboard (Left/Right + Enter)
- F11 fullscreen
- Left/Right in Console Row and Blades
- Tile shapes Wide and Short in every look (Poster checked in Wide Home only)

## Not matching the concept images yet
- Game tiles are color gradients, no cover art or background art
- No glow, glass or blur effects on the Blades panels
- Blades header and layout are rough (search box and title placement)
- No logo, no profile picture, no real icons on tabs or chips
- Sizes are fixed, not scaled to the window

## NOT built yet
- Gamepad/controller input (keyboard and mouse only)
- Real Update (needs GitHub) and a real installer for full uninstall
- Real game scanning (Steam, Epic, GOG) and cover art
- Profiles, Home button over a running game, Quit & switch to, status panel
- Media tab, Stremio-style add-ons, PC mode, emergency exit button
- Animations (tile grow, background crossfade, blade slide)

## Open question
- Settings opens with S or F2 for now. Where should the Settings button live?

## Update (controller + real games)
- Built, tested: placeholder games and last-played panel removed; only fully installed Steam games show (Lego Batman shows with cover art).
- Built, tested with keyboard: Settings navigation (Up/Down/Left/Right move focus, Enter presses, Esc backs out).
- Built, UNTESTED with a real controller: Xbox-style (XInput) support. D-pad/left stick = move, A = select/Play, B = back, Start = Settings. Controller is first-class, equal to keyboard and mouse.
- Not built yet: PlayStation/Switch pads, bumpers/triggers, X/Y actions, Home button, vibration, controller help bar on screen.
- Play/launch still untested.

## Pending: GitHub upload
- New empty repo: https://github.com/jjrussellite28-art/Mega-hub- (for Mega Hub Plus only).
- Not uploaded yet: laptop browser and this session are not signed in to GitHub. Jake will sign in later, then upload the project files (skip bin/ and obj/).
- License not chosen yet (leaning MIT; GPL-3.0 if forks must stay open).
- Controller support (Xbox/XInput) built but untested with a real controller.

## Tested (2026-11-25)
- Play / Enter launches Lego Batman through Steam. Works (Steam asked for a one-time permission first).
- Automatic Windows build on GitHub passes (artifact: MegaHubPlus-windows). Downloaded .exe not yet tested.
- Still untested with a real controller (Xbox-style via XInput).
