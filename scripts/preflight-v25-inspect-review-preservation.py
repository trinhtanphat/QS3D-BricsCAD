#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
commands = (ROOT / "src/QS3D.BricsCAD.V25/Commands.cs").read_text(encoding="utf-8")
palette = (ROOT / "src/QS3D.BricsCAD.V25/PaletteCoordinator.cs").read_text(encoding="utf-8")
errors = []

inspect_match = re.search(
    r'\[CommandMethod\("QS3DINSPECT"[^\]]*\)\].*?public void InspectSelection\(\).*?\{(?P<body>.*?)\n\s*\}\n\s*\n\s*\[CommandMethod',
    commands,
    re.S,
)
if not inspect_match:
    errors.append("QS3DINSPECT command body could not be located")
else:
    body = inspect_match.group("body")
    set_pos = body.find("PaletteCoordinator.SetInspection(snapshots);")
    show_pos = body.find("PaletteCoordinator.Show();")
    if set_pos < 0 or show_pos < 0 or set_pos >= show_pos:
        errors.append("QS3DINSPECT must set inspection state before showing the BIM workspace")

required_palette_tokens = (
    "private static bool _preserveInspectionStatusOnNextShow;",
    "var preserveInspectionStatus = _preserveInspectionStatusOnNextShow;",
    "_preserveInspectionStatusOnNextShow = false;",
    "if (!preserveInspectionStatus)",
    "_preserveInspectionStatusOnNextShow = true;",
    'public static void Show() => ShowBimWorkspace();',
)
for token in required_palette_tokens:
    if token not in palette:
        errors.append("inspection/BIM status preservation contract missing token: " + token)

set_inspection = re.search(
    r'public static void SetInspection\(IReadOnlyList<EntitySnapshot> snapshots\)\s*\{(?P<body>.*?)\n\s*\}',
    palette,
    re.S,
)
if not set_inspection or "_preserveInspectionStatusOnNextShow = true;" not in set_inspection.group("body"):
    errors.append("SetInspection must arm one-shot status preservation after writing Instance/Family review")

show_bim = re.search(
    r'public static bool ShowBimWorkspace\(\)\s*\{(?P<body>.*?)\n\s*\}\n\s*\n\s*public static void ShowDrawingManagement',
    palette,
    re.S,
)
if not show_bim:
    errors.append("ShowBimWorkspace body could not be located")
else:
    body = show_bim.group("body")
    status_pos = body.find('_workspacePanel?.SetStatus("MÔ HÌNH BIM')
    guard_pos = body.rfind("if (!preserveInspectionStatus)", 0, status_pos if status_pos >= 0 else len(body))
    if status_pos < 0 or guard_pos < 0:
        errors.append("BIM banner must remain present but be guarded when QS3DINSPECT just produced review status")

if errors:
    for error in errors:
        print("ERROR:", error)
    raise SystemExit(1)

print("PASS: QS3DINSPECT preserves Instance/Family review while explicit QS3D retains BIM activation banner.")
