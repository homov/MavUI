#!/usr/bin/env python3
"""Linux (X11) counterpart of ``tools/win/md3ui.ps1``.

Start/stop/screenshot/click/key/wheel/info for the Md3 desktop demo, using
python-xlib (XTEST) for synthetic input and ``gnome-screenshot`` for captures.

Coordinates are **window-relative logical-free pixels of the client area**
(never the window frame), which is what ``md3ui.ps1`` uses as well.

Requirements (Linux Mint / Ubuntu, X11 session):

* ``python3-xlib``  (``python3 -c "import Xlib"``)
* ``Pillow``        (``python3 -c "import PIL"``)     - cropping the capture
* ``gnome-screenshot`` or ``scrot``                   - capturing the screen
* ``wmctrl``, ``x11-utils`` (``xwininfo``)

Examples::

    python3 tools/linux/md3ui.py start
    python3 tools/linux/md3ui.py start --window 400x800 --safe-area 24,48 --keyboard 300
    python3 tools/linux/md3ui.py shot  --out docs/handoff/linux-home.png
    python3 tools/linux/md3ui.py click --x 779 --y 313
    python3 tools/linux/md3ui.py key   --keys Escape
    python3 tools/linux/md3ui.py wheel --x 400 --y 300 --delta -600
    python3 tools/linux/md3ui.py info
    python3 tools/linux/md3ui.py stop
"""

from __future__ import annotations

import argparse
import os
import shutil
import subprocess
import sys
import time

try:
    from Xlib import X, XK, display
    from Xlib.ext import xtest
except ImportError:  # pragma: no cover
    sys.exit("python-xlib is required:  sudo apt install python3-xlib")

try:
    from PIL import Image
except ImportError:  # pragma: no cover
    sys.exit("Pillow is required:  sudo apt install python3-pil")

WINDOW_TITLE = os.environ.get("MD3_WINDOW_TITLE", "Md3 Navigation Demo")
REPO_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
PID_FILE = "/tmp/opencode/md3ui.pid"
LOG_FILE = "/tmp/opencode/md3ui.log"

_d: display.Display | None = None


def conn() -> display.Display:
    global _d
    if _d is None:
        _d = display.Display()
    return _d


def find_window(title: str = WINDOW_TITLE):
    d = conn()
    root = d.screen().root
    found = []

    def walk(win):
        try:
            name = win.get_wm_name()
            if name and title.lower() in str(name).lower():
                found.append(win)
                return
        except Exception:
            pass
        try:
            for child in win.query_tree().children:
                walk(child)
        except Exception:
            pass

    walk(root)
    if not found:
        return None
    return found[-1]


def client_origin(win) -> tuple[int, int]:
    """Return the root coordinates of the client area's top-left corner."""
    d = conn()
    root = d.screen().root
    tr = win.translate_coords(root, 0, 0)
    return -tr.x, -tr.y


def capture_full(path: str) -> None:
    if shutil.which("scrot"):
        subprocess.run(["scrot", "-o", path], check=True, capture_output=True)
    elif shutil.which("gnome-screenshot"):
        subprocess.run(["gnome-screenshot", "-f", path], check=True, capture_output=True)
    else:
        sys.exit("no screenshot tool: install gnome-screenshot or scrot")


def cmd_start(args) -> int:
    if find_window():
        print(f"already running: {WINDOW_TITLE}")
        return 0
    env = os.environ.copy()
    if args.window:
        env["MD3_WINDOW"] = args.window
    if args.safe_area:
        env["MD3_SAFE_AREA"] = args.safe_area
    if args.keyboard:
        env["MD3_KEYBOARD"] = args.keyboard
    if args.smoke:
        env["MD3_SMOKE"] = "1"
    os.makedirs(os.path.dirname(LOG_FILE) or ".", exist_ok=True)
    log = open(LOG_FILE, "w")
    proc = subprocess.Popen(
        ["dotnet", "run", "--project", "samples/Demo/Md3.Demo.csproj", *args.dotnet_args],
        cwd=REPO_ROOT,
        env=env,
        stdout=log,
        stderr=subprocess.STDOUT,
        start_new_session=True,
    )
    os.makedirs(os.path.dirname(PID_FILE), exist_ok=True)
    with open(PID_FILE, "w") as fh:
        fh.write(str(proc.pid))
    deadline = time.time() + (args.timeout or 60)
    while time.time() < deadline:
        if find_window():
            time.sleep(1.0)  # let the first frame settle
            print(f"started: pid={proc.pid} log={LOG_FILE}")
            return 0
        if proc.poll() is not None:
            print(f"process exited with {proc.returncode}; log:")
            print(open(LOG_FILE).read())
            return 1
        time.sleep(0.5)
    print(f"timeout waiting for window; log={LOG_FILE}")
    return 1


def cmd_stop(_args) -> int:
    pid = None
    if os.path.exists(PID_FILE):
        pid = open(PID_FILE).read().strip()
    if pid:
        subprocess.run(["kill", "-TERM", pid], check=False)
        subprocess.run(["pkill", "-TERM", "-P", pid], check=False)
    win = find_window()
    if win:
        try:
            win.kill_client()
            conn().sync()
        except Exception:
            pass
    time.sleep(1.0)
    print("stopped")
    return 0


def cmd_shot(args) -> int:
    win = find_window()
    if win is None:
        print(f"window not found: {WINDOW_TITLE}")
        return 1
    x, y = client_origin(win)
    geo = win.get_geometry()
    if args.frame:
        # include the window manager decoration
        parent = win.query_tree().parent
        try:
            px, py = client_origin(parent)
            fx, fy = px, py
            fw = parent.get_geometry().width
            fh = parent.get_geometry().height
        except Exception:
            fx, fy, fw, fh = x, y, geo.width, geo.height
        x, y, w, h = fx, fy, max(fw, geo.width), max(fh, geo.height)
    else:
        x, y, w, h = x, y, geo.width, geo.height

    tmp = "/tmp/opencode/_full.png"
    os.makedirs(os.path.dirname(tmp), exist_ok=True)
    capture_full(tmp)
    img = Image.open(tmp)
    img.crop((x, y, x + w, y + h)).save(args.out)
    print(f"saved {args.out} ({w}x{h} from root {x},{y})")
    return 0


def cmd_info(_args) -> int:
    win = find_window()
    if win is None:
        print(f"window not found: {WINDOW_TITLE}")
        return 1
    x, y = client_origin(win)
    geo = win.get_geometry()
    print(f"client origin: {x},{y}")
    print(f"size: {geo.width}x{geo.height}")
    print(f"root resolution: {conn().screen().width_in_pixels}x"
          f"{conn().screen().height_in_pixels}")
    return 0


def _point(args) -> tuple[int, int]:
    win = find_window()
    if win is None:
        sys.exit(f"window not found: {WINDOW_TITLE}")
    x, y = client_origin(win)
    return x + args.x, y + args.y


def cmd_click(args) -> int:
    d = conn()
    px, py = _point(args)
    subprocess.run(["wmctrl", "-a", WINDOW_TITLE], check=False, capture_output=True)
    time.sleep(0.3)
    xtest.fake_input(d, X.MotionNotify, x=px, y=py)
    d.sync()
    time.sleep(0.15)
    button = args.button
    for _ in range(args.repeat):
        xtest.fake_input(d, X.ButtonPress, button)
        d.sync()
        time.sleep(0.06)
        xtest.fake_input(d, X.ButtonRelease, button)
        d.sync()
        time.sleep(0.12)
    if args.settle:
        time.sleep(args.settle)
    return 0


def cmd_wheel(args) -> int:
    d = conn()
    px, py = _point(args)
    xtest.fake_input(d, X.MotionNotify, x=px, y=py)
    d.sync()
    time.sleep(0.1)
    button = 4 if args.delta > 0 else 5
    steps = max(1, abs(args.delta) // 120)
    for _ in range(steps):
        xtest.fake_input(d, X.ButtonPress, button)
        d.sync()
        xtest.fake_input(d, X.ButtonRelease, button)
        d.sync()
        time.sleep(0.03)
    if args.settle:
        time.sleep(args.settle)
    return 0


_KEY_ALIASES = {
    "esc": "Escape", "escape": "Escape", "enter": "Return", "return": "Return",
    "tab": "Tab", "space": "space", "backspace": "BackSpace", "del": "Delete",
    "delete": "Delete", "up": "Up", "down": "Down", "left": "Left", "right": "Right",
    "home": "Home", "end": "End", "pgup": "Prior", "pgdn": "Next",
}
_MODS = {
    "ctrl": ("Control_L", X.ControlMask), "control": ("Control_L", X.ControlMask),
    "shift": ("Shift_L", X.ShiftMask), "alt": ("Alt_L", X.Mod1Mask),
    "super": ("Super_L", X.Mod4Mask), "win": ("Super_L", X.Mod4Mask),
}


def _keycode(name: str) -> int:
    d = conn()
    keysym = XK.string_to_keysym(name)
    if keysym == 0 and len(name) == 1:
        keysym = ord(name)
    return d.keysym_to_keycode(keysym)


def _send_key(name: str, shift: bool = False) -> None:
    d = conn()
    kc = _keycode(name)
    if kc == 0:
        return
    sh = _keycode("Shift_L") if shift else 0
    if shift:
        xtest.fake_input(d, X.KeyPress, sh)
    xtest.fake_input(d, X.KeyPress, kc)
    xtest.fake_input(d, X.KeyRelease, kc)
    if shift:
        xtest.fake_input(d, X.KeyRelease, sh)
    d.sync()
    time.sleep(0.02)


def cmd_key(args) -> int:
    subprocess.run(["wmctrl", "-a", WINDOW_TITLE], check=False, capture_output=True)
    time.sleep(0.2)
    d = conn()
    name = _KEY_ALIASES.get(args.keys.split("+")[-1].lower(), args.keys.split("+")[-1])
    held = []
    for part in args.keys.split("+")[:-1]:
        mod = _MODS.get(part.lower())
        if mod:
            kc = _keycode(mod[0])
            xtest.fake_input(d, X.KeyPress, kc)
            held.append(kc)
    d.sync()
    time.sleep(0.05)
    _send_key(name, shift=args.shift)
    for kc in reversed(held):
        xtest.fake_input(d, X.KeyRelease, kc)
    d.sync()
    if args.settle:
        time.sleep(args.settle)
    return 0


def cmd_type(args) -> int:
    subprocess.run(["wmctrl", "-a", WINDOW_TITLE], check=False, capture_output=True)
    time.sleep(0.2)
    for ch in args.text:
        if ch == " ":
            _send_key("space")
        elif ch.isupper():
            _send_key(ch.lower(), shift=True)
        else:
            _send_key(ch)
    if args.settle:
        time.sleep(args.settle)
    return 0


def cmd_move(args) -> int:
    d = conn()
    px, py = _point(args)
    xtest.fake_input(d, X.MotionNotify, x=px, y=py)
    d.sync()
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="action", required=True)

    p = sub.add_parser("start", help="start the desktop demo")
    p.add_argument("--window", help="MD3_WINDOW, e.g. 400x800")
    p.add_argument("--safe-area", help="MD3_SAFE_AREA, e.g. 24,48")
    p.add_argument("--keyboard", help="MD3_KEYBOARD, e.g. 300")
    p.add_argument("--smoke", action="store_true", help="MD3_SMOKE=1")
    p.add_argument("--no-build", action="store_true")
    p.add_argument("--timeout", type=int, default=60)
    p.set_defaults(func=cmd_start)

    p = sub.add_parser("stop", help="stop the demo")
    p.set_defaults(func=cmd_stop)

    p = sub.add_parser("shot", help="screenshot the demo client area")
    p.add_argument("--out", required=True)
    p.add_argument("--frame", action="store_true", help="include window decoration")
    p.set_defaults(func=cmd_shot)

    p = sub.add_parser("info", help="print window geometry")
    p.set_defaults(func=cmd_info)

    p = sub.add_parser("click", help="click at window-relative coordinates")
    p.add_argument("--x", type=int, required=True)
    p.add_argument("--y", type=int, required=True)
    p.add_argument("--button", type=int, default=1)
    p.add_argument("--repeat", type=int, default=1)
    p.add_argument("--settle", type=float, default=0.8)
    p.set_defaults(func=cmd_click)

    p = sub.add_parser("move", help="move the pointer without clicking")
    p.add_argument("--x", type=int, required=True)
    p.add_argument("--y", type=int, required=True)
    p.set_defaults(func=cmd_move)

    p = sub.add_parser("wheel", help="scroll under a window-relative point")
    p.add_argument("--x", type=int, default=100)
    p.add_argument("--y", type=int, default=100)
    p.add_argument("--delta", type=int, default=-120, help="negative scrolls down")
    p.add_argument("--settle", type=float, default=0.5)
    p.set_defaults(func=cmd_wheel)

    p = sub.add_parser("key", help="send a key combo, e.g. Escape or alt+Left")
    p.add_argument("--keys", required=True)
    p.add_argument("--shift", action="store_true")
    p.add_argument("--settle", type=float, default=0.8)
    p.set_defaults(func=cmd_key)

    p = sub.add_parser("type", help="type text into the focused control")
    p.add_argument("--text", required=True)
    p.add_argument("--settle", type=float, default=0.5)
    p.set_defaults(func=cmd_type)

    args = parser.parse_args()
    if getattr(args, "no_build", False):
        args.dotnet_args = ["--no-build"]
    else:
        args.dotnet_args = []
    return args.func(args)


if __name__ == "__main__":
    sys.exit(main())
