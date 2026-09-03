#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
COORDINATOR = ROOT / "src/QS3D.BricsCAD.V25/StartCenterPaletteCoordinator.cs"
PANEL = ROOT / "src/QS3D.BricsCAD.V25/UI/BltStartCenterPanel.cs"

coordinator = COORDINATOR.read_text(encoding="utf-8")
panel = PANEL.read_text(encoding="utf-8")

# The panel must support an explicit document-bound refresh so activation callbacks do not
# re-query a process-global active document after the host has already supplied event affinity.
for token in (
    "RefreshFromDocument(Document? document)",
    "RefreshFromDocument(Application.DocumentManager.MdiActiveDocument);",
):
    if token not in panel:
        raise SystemExit(f"Start Center explicit document refresh contract missing: {token}")

# The modeless coordinator must use the event document, with MDI active document only as the
# null fallback. Calling RefreshFromActiveDocument from DocumentActivated is a stale-affinity risk.
if "panel.RefreshFromDocument(e.Document ?? Application.DocumentManager.MdiActiveDocument);" not in coordinator:
    raise SystemExit("Start Center DocumentActivated must refresh from the event document")

handler_match = re.search(
    r"private\s+static\s+void\s+OnDocumentActivated\s*\([^)]*\)\s*\{(?P<body>.*?)\n\s*\}\n\s*\}",
    coordinator,
    re.DOTALL,
)
if handler_match is None:
    raise SystemExit("Start Center DocumentActivated handler boundary missing")
handler_body = handler_match.group("body")
if "RefreshFromActiveDocument()" in handler_body:
    raise SystemExit("Start Center DocumentActivated must not re-query active document")

# PaletteSet.Visible is a native-host boundary. During shutdown/dispose races even the getter can
# fail, so the activation callback must not read it before entering its fail-soft try/catch. Accept
# either the coordinator field or a captured local, while still enforcing ordering relative to try.
visibility_positions = [
    position
    for token in ("_palette.Visible", "palette.Visible")
    if (position := handler_body.find(token)) >= 0
]
try_index = handler_body.find("try")
if not visibility_positions:
    raise SystemExit("Start Center DocumentActivated visibility gate missing")
if try_index < 0 or min(visibility_positions) < try_index:
    raise SystemExit("Start Center DocumentActivated must guard PaletteSet.Visible inside try/catch")

# Hide must release native event ownership even if PaletteSet.Visible throws. This intentionally
# checks the semantic try/finally relationship rather than one whitespace-specific formatting.
hide_match = re.search(
    r"public\s+static\s+void\s+Hide\s*\(\s*\)\s*\{(?P<body>.*?)\n\s*\}\n\s*\n\s*public\s+static\s+void\s+Dispose",
    coordinator,
    re.DOTALL,
)
if hide_match is None:
    raise SystemExit("Start Center Hide() boundary missing")
hide_body = hide_match.group("body")
if re.search(
    r"try\s*\{(?P<try>.*?)\}\s*finally\s*\{(?P<finally>.*?)\}",
    hide_body,
    re.DOTALL,
) is None:
    raise SystemExit("Start Center Hide() must guarantee cleanup with try/finally")

cleanup = re.search(
    r"finally\s*\{(?P<body>.*?)\}",
    hide_body,
    re.DOTALL,
)
if cleanup is None or "UnsubscribeFromDocumentActivation();" not in cleanup.group("body"):
    raise SystemExit("Start Center Hide() finally must unsubscribe document activation")

# Preserve lifecycle redaction/fail-soft boundaries.
for forbidden in ("ex.Message", "error.Message", "Exception.Message"):
    if forbidden in coordinator or forbidden in panel:
        raise SystemExit(f"Start Center lifecycle must not expose raw exception detail: {forbidden}")

print("PASS Start Center document-affinity and modeless lifecycle contract")
