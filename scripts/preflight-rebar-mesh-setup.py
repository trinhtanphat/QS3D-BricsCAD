#!/usr/bin/env python3
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
errors = []
xaml = ROOT / "src/QS3D.BricsCAD.V25/UI/RebarMeshSetupWindow.xaml"
code = ROOT / "src/QS3D.BricsCAD.V25/UI/RebarMeshSetupWindow.xaml.cs"
command = ROOT / "src/QS3D.BricsCAD.V25/RebarMeshSetupCommands.cs"
for path in (xaml, code, command):
    if not path.is_file(): errors.append("missing rebar mesh setup file: " + str(path.relative_to(ROOT)))

if xaml.is_file():
    try: ET.parse(xaml)
    except ET.ParseError as exc: errors.append("RebarMeshSetupWindow.xaml is not well formed: " + str(exc))
    text = xaml.read_text(encoding="utf-8")
    for needle in ("Direction1Text", "Direction2Text", "CoverText", "FacesCombo", "ClosestToFaceCheck", "không tính toán hay đề xuất thiết kế kết cấu", "OnSave"):
        if needle not in text: errors.append("Rebar Mesh Setup XAML missing: " + needle)

if code.is_file():
    text = code.read_text(encoding="utf-8")
    for needle in (
        "ElementCategory.Slab", "ElementCategory.StructuralWall", "RebarNotationParser.Parse",
        "RebarSlabXNotation", "RebarSlabYNotation", "RebarWallHorizontalNotation", "RebarWallVerticalNotation",
        "RebarSlabCoverM", "RebarWallCoverM", "SetProperty", "_project.Touch()",
        "không được đồng thời có count và spacing", "cùng đường kính",
    ):
        if needle not in text: errors.append("Rebar Mesh Setup validation missing: " + needle)

if command.is_file():
    text = command.read_text(encoding="utf-8")
    for needle in ('CommandMethod("QS3DREBARMESHSETUP"', "EntitySnapshotReader.ReadCurrentSelection", "matches.Count != 1", "new RebarMeshSetupWindow", "ShowModelessWindow"):
        if needle not in text: errors.append("Rebar Mesh Setup command missing: " + needle)

owners = []
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    if re.search(r'\[CommandMethod\("QS3DREBARMESHSETUP"', path.read_text(encoding="utf-8"), re.IGNORECASE): owners.append(str(path.relative_to(ROOT)))
if len(owners) != 1: errors.append("QS3DREBARMESHSETUP must have exactly one owner; found: " + ", ".join(owners))

print("QS3D Rebar Mesh Setup preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: explicit Slab/StructuralWall mesh input UI, no-auto-design notice, validation, stale-aware semantic mutation and command wiring are present.")
