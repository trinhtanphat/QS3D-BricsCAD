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
    "public ZoneManagerWindow Window { get; }",
    "public IntPtr NativeDatabaseIdentity { get; }",
    "database.UnmanagedObject == NativeDatabaseIdentity",
    "public bool Matches(Document document)",
):
    require(token in source, "published owner missing wrapper-drift-safe native affinity token: " + token)
require("public Document Document { get; }" not in source,
        "published ownership must not retain a managed Document wrapper")

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
    same_doc = body.find("if (previous.Matches(document))")
    activate = body.find("previous.Window.Activate();")
    same_return = body.find("return;", activate if activate >= 0 else 0)
    close = body.find("previous.Window.Close();")
    retained = body.find("if (ReferenceEquals(_published, previous))", close if close >= 0 else 0)
    construct = body.find("candidate = new ZoneManagerWindow(document);")
    require(min(capture, live, same_doc, activate, same_return, close, retained, construct) >= 0,
            "single-instance reuse/cross-document arbitration structure is incomplete")
    require(capture < live < same_doc < activate < same_return < close < retained < construct,
            "same-native-database reuse and terminal-close proof must precede candidate construction")
    require("ReferenceEquals(previous.Document, document)" not in body,
            "same-document reuse must not depend on managed Document wrapper identity")
    require("try { previous.Window.Close(); } catch { }" not in body,
            "cross-document close failures must not be swallowed")
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

print("PASS Zone Manager retains one native-database-bound owner across wrapper drift and vetoed cross-document close")
