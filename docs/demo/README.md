# Mav Desktop Demo

Small desktop harness for the custom Mav navigation stack.

## Run

```bash
dotnet run --project samples/Demo/Mav.Demo.csproj
```

## Mobile preview

The demo can simulate phone chrome on the desktop:

```bash
# 400 x 800 dp window, 24 dp status bar and 48 dp navigation bar
MAV_WINDOW=400x800 MAV_SAFE_AREA=24,48 dotnet run --project samples/Demo/Mav.Demo.csproj

# also simulate an open soft keyboard with a 300 dp inset
MAV_WINDOW=400x800 MAV_SAFE_AREA=24,48 MAV_KEYBOARD=300 dotnet run --project samples/Demo/Mav.Demo.csproj
```

## Smoke test

Starts the app, pushes two pages, pops one, returns to the root and exits:

```bash
MAV_SMOKE=1 dotnet run --project samples/Demo/Mav.Demo.csproj
```

Expected output:

```text
push controls: depth=2, canGoBack=True, command=True
dialog open: overlay=True, depth=2
back closes overlay: overlay=False, depth=2
dialog for page leave: overlay=True
push inbox: depth=3, title=Inbox, overlay=False
pop: depth=2, title=Controls
pop to root: depth=1, title=Mav Navigation, command=False
MAV_SMOKE_OK
```

## What to check manually

- App bar title, subtitle and back arrow change with the current page.
- Actions on the right side of the app bar.
- Bottom app bar and FAB.
- Dialog, bottom sheet and snackbar.
- Dark/light theme switch.
- List → detail → back.
- `Escape` / `Alt+Left` closes the top overlay first, then goes back.
- Opening a dialog and pushing another page closes the dialog with its owner.

## Automated GUI checks (Linux/X11)

`tools/linux/mavui.py` starts the demo, takes client-area screenshots and sends
clicks, keys, typed text and wheel scrolling. Coordinates are client-area pixels, so
they match the screenshot 1:1.

```bash
python3 tools/linux/mavui.py start --no-build
python3 tools/linux/mavui.py shot  --out /tmp/opencode/home.png
python3 tools/linux/mavui.py click --x 779 --y 285     # Show dialog
python3 tools/linux/mavui.py key   --keys Escape
python3 tools/linux/mavui.py wheel --x 400 --y 300 --delta -600
python3 tools/linux/mavui.py stop
```

Mobile preview works the same way:

```bash
python3 tools/linux/mavui.py start --window 400x800 --safe-area 24,48 --keyboard 300
python3 tools/linux/mavui.py start --window 640x360 --safe-area 48,0,0,24
```

On Windows use `tools/win/mavui.ps1` instead (`-Action start|stop|shot|click|key|wheel`).
