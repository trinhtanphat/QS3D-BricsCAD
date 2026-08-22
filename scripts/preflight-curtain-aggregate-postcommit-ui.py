#!/usr/bin/env python3
from pathlib import Path
import re
import sys


ROOT = Path(__file__).resolve().parents[1]
CURTAIN = ROOT / "src" / "QS3D.BricsCAD.V25" / "CurtainWallBuildCommands.cs"
LINE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad" / "WallSolidBuilder.cs"
PATH = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad" / "PolylineWallSolidBuilder.cs"
errors: list[str] = []

curtain = CURTAIN.read_text(encoding="utf-8")
line = LINE.read_text(encoding="utf-8")
path = PATH.read_text(encoding="utf-8")

builder_contracts = (
    ("LINE", line, "BuildSelectedLineWalls", "LINE wall native 3D"),
    ("path", path, "BuildSelected", "Polyline wall native 3D"),
)
for label, text, method, reason in builder_contracts:
    signature = re.compile(
        rf'public static int {method}\(\s*Document document,\s*ProjectState project,\s*'
        rf'ElementCategory category,\s*bool allowPostCommitUi = true\s*\)',
        re.DOTALL,
    )
    if not signature.search(text):
        errors.append(f"{label} host builder must preserve standalone UI with a default-true optional flag")
    guarded = re.compile(
        rf'if \(pending\.Count > 0 && allowPostCommitUi\)\s*'
        rf'CadPostCommitUi\.TryRegen\(document, "{re.escape(reason)}"\);'
    )
    if not guarded.search(text):
        errors.append(f"{label} host builder post-commit Regen must be guarded by allowPostCommitUi")
    if text.count("CadPostCommitUi.TryRegen(") != 1:
        errors.append(f"{label} host builder must retain exactly one guarded post-commit UI call")

start = curtain.find("public void BuildCurtain3D()")
end = curtain.find("private static void ApplySelection", start)
body = curtain[start:end] if start >= 0 and end > start else curtain
for call in (
    r'WallSolidBuilder\.BuildSelectedLineWalls\(\s*document,\s*project,\s*ElementCategory\.GlassWall,\s*allowPostCommitUi:\s*false\s*\)',
    r'PolylineWallSolidBuilder\.BuildSelected\(\s*document,\s*project,\s*ElementCategory\.GlassWall,\s*allowPostCommitUi:\s*false\s*\)',
):
    if not re.search(call, body, re.DOTALL):
        errors.append("Curtain aggregate must suppress each host-builder UI refresh")

commit = body.find("commandTransaction.Commit();")
finalize = body.find("FinalizeUi(", commit + 1)
finalize_method = curtain.find("private static void FinalizeUi")
regen = curtain.find("document.Editor.Regen();", finalize_method)
if commit < 0 or finalize < commit or finalize_method < 0 or regen < finalize_method:
    errors.append("Curtain aggregate must retain its own UI refresh only after the outer commit")
if "CadPostCommitUi.TryRegen" in body:
    errors.append("Curtain aggregate command must not directly invoke builder-level post-commit UI")

print("QS3D Curtain aggregate post-commit UI preflight")
if errors:
    for error in errors:
        print("ERROR: " + error)
    sys.exit(1)

print("PASS: standalone LINE/path host builders retain default post-commit UI, while QS3DCURTAIN3D suppresses nested host Regen and owns the final UI refresh after its outer commit.")
