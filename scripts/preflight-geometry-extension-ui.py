#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.BricsCAD.V25/UI/GeometryExtensionsWindow.xaml",
    "src/QS3D.BricsCAD.V25/UI/GeometryExtensionsWindow.xaml.cs",
    "src/QS3D.BricsCAD.V25/GeometryExtensionsCommands.cs",
]
for relative in required:
    if not (ROOT / relative).is_file():
        errors.append("missing geometry extension UI file: " + relative)

xaml = ROOT / required[0]
if xaml.is_file():
    text = xaml.read_text(encoding="utf-8")
    for tag in (
        'Tag="QS3DWALLJUNCTIONS"', 'Tag="QS3DWALLSNAPPREVIEW"', 'Tag="QS3DWALLSNAPAPPLY"',
        'Tag="QS3DAUTOLINKHOSTS"', 'Tag="QS3DCUTOPENINGS"', 'Tag="QS3DCUTOPENINGSCURVED"',
        'Tag="QS3DREBAR3D"', 'Tag="QS3DREBARTIES3D"', 'Tag="QS3DREBAR3DSHAPE"',
        'Tag="QS3DREBARHEALTHALL"', 'Click="OnCommandClick"'):
        if tag not in text:
            errors.append("GeometryExtensionsWindow missing tag/handler: " + tag)

code = ROOT / required[1]
if code.is_file():
    text = code.read_text(encoding="utf-8")
    for needle in ("OnCommandClick", "SendStringToExecute", "StatusText.Text", "Application.DocumentManager.MdiActiveDocument", "ex.GetType().Name"):
        if needle not in text:
            errors.append("GeometryExtensionsWindow code-behind missing: " + needle)
    if "ex.Message" in text:
        errors.append("Geometry Extensions must not expose raw host exception messages in modeless UI/command-line status")

command = ROOT / required[2]
if command.is_file():
    text = command.read_text(encoding="utf-8")
    for needle in (
        'CommandMethod("QS3DGEOMETRYEXT"', "private static GeometryExtensionsWindow? _published;",
        "private static GeometryExtensionsWindow? _pending;", "var pending = _pending;",
        "if (pending != null && !TryClosePendingWindow(pending))", "var previous = _published;",
        "if (previous.IsLoaded)", "previous.Activate();", "ReleasePublishedWindow(previous);",
        "candidate = new GeometryExtensionsWindow();", "_pending = window;",
        "window.Closed += (_, __) => ReleaseWindow(window);", "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
        "if (!window.IsLoaded)", "_published = window;", "ReleasePendingWindow(window);", "candidate = null;",
        "finally", "if (candidate != null)", "TryClosePendingWindow(candidate);",
        "private static void ReleaseWindow(GeometryExtensionsWindow window)",
        "private static void ReleasePublishedWindow(GeometryExtensionsWindow window)",
        "if (!ReferenceEquals(_published, window)) return;",
        "private static void ReleasePendingWindow(GeometryExtensionsWindow window)",
        "if (!ReferenceEquals(_pending, window)) return;",
        "private static bool TryClosePendingWindow(GeometryExtensionsWindow window)",
        "if (!ReferenceEquals(_pending, window)) return true;", "if (ReferenceEquals(_published, window))",
        "if (window.IsLoaded) return false;", "ex.GetType().Name"):
        if needle not in text:
            errors.append("Geometry Extensions command missing lifecycle contract: " + needle)

    release_pending = text.find("ReleasePendingWindow(window);")
    clear_candidate = text.find("candidate = null;", release_pending) if release_pending >= 0 else -1
    positions = [text.find(token) for token in (
        "var pending = _pending;", "if (pending != null && !TryClosePendingWindow(pending))",
        "var previous = _published;", "candidate = new GeometryExtensionsWindow();", "_pending = window;",
        "window.Closed += (_, __) => ReleaseWindow(window);", "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
        "if (!window.IsLoaded)", "_published = window;", "ReleasePendingWindow(window);")]
    positions.extend([clear_candidate, text.find("finally"), text.find("TryClosePendingWindow(candidate);")])
    if min(positions) < 0 or positions != sorted(positions):
        errors.append("Geometry Extensions must drain failed pending ownership before construct, then pending -> Closed -> show -> Loaded -> publish -> release pending -> finally cleanup")

    helper_start = text.find("private static bool TryClosePendingWindow")
    helper = text[helper_start:] if helper_start >= 0 else ""
    non_owner = helper.find("if (!ReferenceEquals(_pending, window)) return true;")
    published_owner = helper.find("if (ReferenceEquals(_published, window))", non_owner + 1)
    published_release = helper.find("ReleasePendingWindow(window);", published_owner + 1)
    published_return = helper.find("return true;", published_release + 1)
    close_if_loaded = helper.find("if (window.IsLoaded)", published_return + 1)
    close_call = helper.find("window.Close();", close_if_loaded + 1)
    live_failure = helper.find("if (window.IsLoaded) return false;", close_call + 1)
    terminal_release = helper.find("ReleasePendingWindow(window);", live_failure + 1)
    helper_positions = [
        non_owner, published_owner, published_release, published_return,
        close_if_loaded, close_call, live_failure, terminal_release,
    ]
    if min(helper_positions) < 0 or helper_positions != sorted(helper_positions):
        errors.append("Geometry Extensions pending cleanup must refuse non-owner cleanup, release pending ownership for an already-published owner, close failed pending candidates best-effort, retain live failures, and release only terminal pending ownership")
    if "ex.Message" in text:
        errors.append("Geometry Extensions launcher must not expose raw host exception messages")

adapter = ROOT / "src/QS3D.BricsCAD.V25"
commands = []
if adapter.is_dir():
    for path in adapter.rglob("*.cs"):
        commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8"))
for required_command in ("QS3DGEOMETRYEXT", "QS3DCUTOPENINGSCURVED", "QS3DREBARTIES3D", "QS3DREBARHEALTHALL", "QS3DWALLSNAPPREVIEW", "QS3DWALLSNAPAPPLY"):
    if commands.count(required_command) != 1:
        errors.append(required_command + " must be declared exactly once")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: Geometry Extensions keeps active-document dispatch while failed publication remains pending-owned until terminal cleanup, preventing duplicate windows and raw host-error disclosure.")
