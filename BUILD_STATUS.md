# Mega Hub +: Build Status

Last updated: 2026-11-24. It builds and runs on Jake's Windows laptop.
Fixed along the way: Program.cs startup, and RollForward=LatestMajor so it runs on the installed .NET.

## Built and TESTED (Claude, on the laptop)
- Builds and opens from "Run Mega Hub +.bat"
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
- New empty repo: https://github.com/jjrussellite28-art/Mega-hub- (for Mega Hub + only).
- Not uploaded yet: laptop browser and this session are not signed in to GitHub. Jake will sign in later, then upload the project files (skip bin/ and obj/).
- License not chosen yet (leaning MIT; GPL-3.0 if forks must stay open).
- Controller support (Xbox/XInput) built but untested with a real controller.

## Tested (2026-11-25)
- Play / Enter launches Lego Batman through Steam. Works (Steam asked for a one-time permission first).
- Automatic Windows build on GitHub passes (artifact: MegaHubPlus-windows). Downloaded .exe not yet tested.
- Still untested with a real controller (Xbox-style via XInput).

## Tested (2026-11-25, installed copy)
- Installer (MegaHubPlus-Setup.exe from GitHub Releases, "latest") installs per-user to %LocalAppData%\Programs\MegaHubPlus and starts. Windows did not block the installed copy.
- New Console Row look (big game art background, glass bar, glowing tile) works.
- Settings: Mode section works. Shell mode goes full screen; App mode returns to a window. Quit button present.
- Not tested: controller hardware, Ctrl+Alt+Q / Back+Start hold exits, in-app Uninstall on the installed copy (should run the installer's uninstaller).
- Known: running straight from source (dotnet run) can be blocked by Windows Smart App Control. App is unsigned.

## Built since (2026-11-25), UNTESTED where noted
- Controller button hint bar (tested, keyboard keys). Controller glyphs untested.
- Real in-app Update from GitHub (tested).
- Real Shell mode: sets Mega Hub + as the sign-in shell (HKCU), Desktop mode switch, auto-restores desktop on exit/crash/uninstall. Warning panel tested; real sign-in as shell UNTESTED.
- PlayStation / Nintendo controllers via SDL, with matching button names. UNTESTED (no hardware).

## Later (Jake asked to save for later)
- Manual games: add any game/emulator by picking its .exe (retro and other launchers), plus extra Steam library folders. Button placement not chosen yet (Settings Games section, the ••• button on home, or both).
- Epic / GOG games, per-game controller screen, restyle Blades and Wide Home to match the concept images.

## Stability pass (2026-11-25)
- Built: safe start screen (after 2 failed starts), settings file repair, logs in %AppData%\MegaHubPlus\logs + "Copy log" button, controller unplug/replug handling, update download checks (size, Windows program, GitHub checksum), smoke test in the GitHub build (app must start and exit cleanly or no release).
- Tested on the laptop: Copy log, checked Update (restarts into the new build), GitHub smoke test passes.
- UNTESTED: safe start screen (needs two failed starts), broken-settings repair, controller unplug with real hardware.

## Wanted (from Jake, Playnite feature list, Nov 25) - not built yet
- Import popular libraries: Steam (done), Epic, GOG, Origin/EA, Battle.net, Ubisoft; also games that are not installed
- Emulation support (retro consoles, add any .exe/emulator)
- Themes / skins (full look changes)
- Extensions: add-ons and plugins (Add-ons & Plugins category exists; signing tool, Weather, News still to build)
- Fullscreen mode with full controller support (in progress)
- Playtime counter (any game, emulated too)
- Fast and light, portable (no install needed), safe and private (everything stored locally)
- Metadata from online (IGDB / SteamGridDB / Steam store): covers, wallpapers, descriptions, also for custom games
- Tabs (Media, Store, Friends) still only labels
- Wallpaper: wide art from online sources (Steam 1920x1080 or SteamGridDB, needs free key)

## Wanted (from Jake, CouchHUD-style look, Nov 25) - not built yet. Copy the look, use Mega Hub + name/logo, only real features
- First-run setup screens like his reference photos: step dots on top, big glowing cards (selected = cyan glow, "Current" badge), big title + one line under it, "Change it later: Settings > ..." note, round A / B / Y buttons and Exit setup at the bottom
  - Steps: Welcome + language (English only for now), tile shape, wallpaper style, App (Desktop) or Shell mode
  - Mode screen: App (alongside Windows) vs Shell (as a console) with short bullet lists
- After setup, restyle the whole app (home, Settings, bar) to match that look
- Bumpers: LB / RB switch tabs (show LB and RB badges at each end of the tab bar)
- Bottom button hints all clickable with the mouse/touch (A and B done; add Y = Menu, Power, and any new ones)
- Home like his photo: tab bar (Favorites, Games, Apps, Downloads, Browser, Settings), Continue playing with the game logo, big poster card, wallpaper behind, bell badge with a count, clock, A Open / Y Menu / Power hints

## Wanted (from Jake, CouchHUD-style Settings, Nov 25) - not built yet
- Settings look: tab bar stays on top (Settings underlined, LB / RB badges), categories with icons on the left (selected = cyan outline), right side rows are wide cards: bold title, one small description line under it, current value + arrow on the right
- A highlighted first card "Setup assistant" (run the setup again any time)
- Soft purple / pink glow over the dark blue background
- Pressing a row opens a popup panel over the blurred page (example: Mode with Shell and App option cards, chosen one lit cyan, each with an icon and a short description); A chooses, B closes
- Bottom hints on Settings: A Open, B Home (clickable)

## Wanted (from Jake, Playnite first-time setup, Nov 25) - not built yet
- A setup step "Library Integration": tick boxes for the stores to connect: Amazon, Battle.net, Epic Games Store, EA, GOG, itch.io, Steam, Ubisoft Connect, Xbox Game Pass / MS Store
- Text: "More integrations can be installed later from the Add-ons menu" (our Settings > Add-ons & Plugins)
- Next button at the bottom right
- Only Steam works today; the others need to be built one by one
