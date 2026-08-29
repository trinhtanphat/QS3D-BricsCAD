#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/CoordinationManagerCommands.cs"
source = COMMAND.read_text(encoding="utf-8") if COMMAND.exists() else ""
errors = []


def require(condition: bool, message: str) -> None:
    if not condition:
        errors.append(message)


require("private static PublishedManager? _published;" in source,
        "manager ownership must be represented by one atomic window/document owner")
require("public CoordinationManagerWindow Window { get; }" in source and
        "public Document Document { get; }" in source,
        "published owner must retain both modeless window and document affinity")

method = re.search(
    r"public void ShowCoordinationManager\(\)\s*\{(?P<body>.*?)\n\s*\}\n\s*\}\n\}",
    source,
    re.S,
)
require(method is not None, "ShowCoordinationManager method was not found")
if method is not None:
    body = method.group("body")
    capture = body.find("var previous = _published;")
    live = body.find("if (previous.Window.IsLoaded)")
    same_doc = body.find("if (ReferenceEquals(previous.Document, document))")
    activate = body.find("previous.Window.Activate();")
    same_return = body.find("return;", activate if activate >= 0 else 0)
    close = body.find("previous.Window.Close();")
    retained = body.find("if (ReferenceEquals(_published, previous))", close if close >= 0 else 0)
    blocked = body.find("still đang mở", retained if retained >= 0 else 0)
    construct = body.find("candidate = new CoordinationManagerWindow")
    require(min(capture, live, same_doc, activate, same_return, close, retained, construct) >= 0,
            "single-instance reuse/cross-document arbitration structure is incomplete")
    require(capture < live < same_doc < activate < same_return < close < retained < construct,
            "live same-document reuse and cross-document terminal-close proof must happen before candidate construction")
    require("_published = null;\n\s*try { previous.Window.Close();" not in body,
            "static ownership must never be pre-cleared before requesting close")
    require("try { previous.Window.Close(); } catch { }" not in body,
            "cross-document close failures must not be swallowed")
    require("if (ReferenceEquals(_published, previous))\n                            throw new InvalidOperationException" in body,
            "Close() return must be followed by proof that terminal Closed released prior ownership")
    require("_published = published;" in body and
            body.find("Application.ShowModelessWindow(IntPtr.Zero, publishedWindow, true);") < body.find("_published = published;"),
            "new manager ownership must publish only after host show succeeds")

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
    print("Coordination Manager single-instance veto-safe preflight FAILED:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("PASS Coordination Manager retains one document-bound owner across same-document reuse and vetoed cross-document close")
