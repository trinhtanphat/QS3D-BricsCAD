#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/ZoneManagerCommands.cs"
source = COMMAND.read_text(encoding="utf-8") if COMMAND.exists() else ""
errors = []


def require(condition: bool, message: str) -> None:
    if not condition:
        errors.append(message)


require("private static PublishedManager? _published;" in source,
        "Zone Manager ownership must be represented by one atomic modeless owner")
for token in (
    "private readonly WeakReference<Document> _document;",
    "public ZoneManagerWindow Window { get; }",
    "public IntPtr NativeDatabaseIdentity { get; }",
    "database.UnmanagedObject == NativeDatabaseIdentity",
    "public bool Matches(Document document)",
    "public bool MatchesManagedWrapper(Document document)",
    "_document.TryGetTarget(out var ownedDocument)",
    "ReferenceEquals(ownedDocument, document)",
):
    require(token in source, "published owner missing native/wrapper provenance token: " + token)
require("public Document Document { get; }" not in source,
        "static publication metadata must not add a strong managed Document owner")

method = re.search(
    r"public void ShowZoneManager\(\)\s*\{(?P<body>.*?)\n\s*\}\n\s*\}\n\}",
    source,
    re.S,
)
require(method is not None, "ShowZoneManager method was not found")
if method is not None:
    body = method.group("body")
    capture = body.find("var previous = _published;")
    live = body.find("if (previous.Window.IsLoaded)")
    same_owner = body.find("if (previous.Matches(document) && previous.MatchesManagedWrapper(document))")
    activate = body.find("previous.Window.Activate();")
    same_return = body.find("return;", activate if activate >= 0 else 0)
    close = body.find("previous.Window.Close();")
    retained = body.find("if (ReferenceEquals(_published, previous))", close if close >= 0 else 0)
    construct = body.find("candidate = new ZoneManagerWindow(document);")
    require(min(capture, live, same_owner, activate, same_return, close, retained, construct) >= 0,
            "single-instance native/wrapper arbitration structure is incomplete")
    require(capture < live < same_owner < activate < same_return < close < retained < construct,
            "exact-wrapper reuse and terminal-close proof must precede candidate construction")
    require("if (previous.Matches(document))\n                        {" not in body,
            "native-database identity alone must not reuse a window bound to an older managed wrapper")
    require("try { previous.Window.Close(); } catch { }" not in body,
            "replacement close failures must not be swallowed")
    require("if (ReferenceEquals(_published, previous))\n                            throw new InvalidOperationException" in body,
            "Close() return must prove terminal Closed released prior ownership")
    show = body.find("Application.ShowModelessWindow(IntPtr.Zero, publishedWindow, true);")
    publish = body.find("_published = published;")
    require(show >= 0 and publish >= 0 and show < publish,
            "new ownership must publish only after host show succeeds")
    require("if (candidate != null)" in body and "try { candidate.Close(); } catch { }" in body,
            "failed candidate must be cleaned up best-effort")

closed = re.search(
    r"publishedWindow\.Closed \+= \(_, __\) =>\s*\{(?P<body>.*?)\n\s*\};",
    source,
    re.S,
)
require(closed is not None, "instance-safe Closed ownership release handler was not found")
if closed is not None:
    require("if (ReferenceEquals(_published, published)) _published = null;" in closed.group("body"),
            "only terminal Closed for the same published owner may release static ownership")

if errors:
    print("Zone Manager single-instance veto-safe preflight FAILED:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("PASS Zone Manager retains one native-database owner, reuses only the exact wrapper, and replaces wrapper drift only after terminal close")
