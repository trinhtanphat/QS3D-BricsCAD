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
        'CommandMethod("QS3DGEOMETRYEXT"', "private static PublishedWindow? _pending;",
        "private static PublishedWindow? _published;", "GetNativeDatabaseIdentity(document)",
        "PreparePublishedWindow(document, nativeDatabaseIdentity)", "var published = _published;",
        "published.Window.Activate();", "var window = new GeometryExtensionsWindow();",
        "owner = new PublishedWindow(window, document, nativeDatabaseIdentity);", "var releaseOwner = owner;",
        "window.Closed += (_, __) => ReleaseOwnedWindow(releaseOwner);", "_pending = owner;",
        "Application.ShowModelessWindow(IntPtr.Zero, window, true);", "if (!window.IsLoaded)",
        "if (!ReferenceEquals(_pending, owner))", "_pending = null;", "_published = owner;", "owner = null;",
        "if (owner != null) ClosePendingOnFailure(owner);",
        "private static bool PreparePublishedWindow(Document requestedDocument, IntPtr requestedNativeDatabaseIdentity)",
        "private static void ClosePendingOnFailure(PublishedWindow owner)",
        "if (!owner.Window.IsLoaded) ReleaseOwnedWindow(owner);",
        "private static void ReleaseOwnedWindow(PublishedWindow owner)",
        "if (ReferenceEquals(_pending, owner)) _pending = null;",
        "if (ReferenceEquals(_published, owner)) _published = null;",
        "private static bool IsActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)",
        "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)"):
        if needle not in text:
            errors.append("Geometry Extensions command missing generation-bound lifecycle contract: " + needle)

    def ordered(tokens, label):
        cursor = 0
        positions = []
        for token in tokens:
            pos = text.find(token, cursor)
            positions.append(pos)
            if pos >= 0:
                cursor = pos + len(token)
        if min(positions) < 0:
            errors.append("Geometry Extensions " + label + " ordering is incomplete")

    ordered((
        "GetNativeDatabaseIdentity(document)",
        "if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;",
        "PreparePublishedWindow(document, nativeDatabaseIdentity)",
        "if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;",
        "var window = new GeometryExtensionsWindow();",
        "owner = new PublishedWindow(window, document, nativeDatabaseIdentity);",
        "var releaseOwner = owner;",
        "window.Closed += (_, __) => ReleaseOwnedWindow(releaseOwner);",
        "_pending = owner;",
        "if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))",
        "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
        "if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))",
        "if (!window.IsLoaded)",
        "if (!ReferenceEquals(_pending, owner))",
        "_pending = null;",
        "_published = owner;",
        "owner = null;",
    ), "pending -> host-pump fence -> Loaded -> exact-owner publication")

    prepare_start = text.find("private static bool PreparePublishedWindow")
    prepare_end = text.find("private static void ClosePendingOnFailure", prepare_start + 1)
    prepare = text[prepare_start:prepare_end] if prepare_start >= 0 and prepare_end > prepare_start else ""
    for needle in (
        "var pending = _pending;", "if (!pending.Window.IsLoaded)", "ReleaseOwnedWindow(pending);",
        "try { pending.Window.Close(); } catch { return false; }", "if (pending.Window.IsLoaded) return false;",
        "var published = _published;", "if (!published.Window.IsLoaded)",
        "published.NativeDatabaseIdentity == requestedNativeDatabaseIdentity",
        "ReferenceEquals(published.Document, requestedDocument)",
        "try { published.Window.Close(); }", "if (published.Window.IsLoaded) return false;",
        "ReleaseOwnedWindow(published);",
    ):
        if needle not in prepare:
            errors.append("Geometry Extensions replacement cleanup missing: " + needle)

    close_start = text.find("private static void ClosePendingOnFailure")
    close_end = text.find("private static void ReleaseOwnedWindow", close_start + 1)
    close_helper = text[close_start:close_end] if close_start >= 0 and close_end > close_start else ""
    close_call = close_helper.find("owner.Window.Close();")
    terminal_release = close_helper.find("if (!owner.Window.IsLoaded) ReleaseOwnedWindow(owner);", close_call + 1)
    if close_call < 0 or terminal_release < 0 or close_call > terminal_release:
        errors.append("Geometry Extensions failed-candidate cleanup must retain ownership unless Close reaches terminal unloaded state")

    show = text.find("Application.ShowModelessWindow(IntPtr.Zero, window, true);")
    post_show_fence = text.find("if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))", show + 1)
    loaded = text.find("if (!window.IsLoaded)", post_show_fence + 1)
    exact_owner = text.find("if (!ReferenceEquals(_pending, owner))", loaded + 1)
    publish = text.find("_published = owner;", exact_owner + 1)
    if min(show, post_show_fence, loaded, exact_owner, publish) < 0 or not (show < post_show_fence < loaded < exact_owner < publish):
        errors.append("Geometry Extensions must revalidate exact document generation after host pumping and prove Loaded + pending ownership before publication")

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
