#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src/QS3D.BricsCAD.V25"
errors = []

owners = []
for path in SRC.rglob("*.cs"):
    text = path.read_text(encoding="utf-8")
    if re.search(r'\[CommandMethod\("QS3DBUILD3D"', text, re.IGNORECASE):
        owners.append(str(path.relative_to(ROOT)))

expected = "src/QS3D.BricsCAD.V25/Build3DCommands.cs"
if owners != [expected]:
    errors.append("QS3DBUILD3D must have exactly one canonical registration in Build3DCommands.cs; found: " + ", ".join(owners))

build = ROOT / expected
if not build.is_file():
    errors.append("missing canonical Build3DCommands.cs")
else:
    text = build.read_text(encoding="utf-8")
    required = (
        "SemanticReferenceHandles.MatchesSelection(x, handles)",
        "ValidateWallSourceBatch(selectedElements, sourceSnapshots, category",
        "RegenerateDirty(project)",
        "BuildCategory(document, project, category, sourceType)",
        "if (sourceTypes.Count == 0)",
        "FinalizeUi(document, selectedElements, sourceHandles, built, regenerated, category)",
        '"UI sync warning: " + ex.Message',
        "Report(document, \"QS3DBUILD3D lỗi: \" + ex.Message)",
        'string.Equals(sourceType, "Line", StringComparison.OrdinalIgnoreCase)',
        "category == ElementCategory.WallPier",
        "WallPierProfileSolidBuilder.BuildSelectedLinePiers(document, project)",
        'string.Equals(sourceType, "Polyline", StringComparison.OrdinalIgnoreCase)',
        "PolylineWallSolidBuilder.BuildSelected(document, project, category)",
    )
    for token in required:
        if token not in text:
            errors.append("canonical Build3D missing contract: " + token)

    body = text[text.find("private static int BuildCategory"):text.find("private static void FinalizeUi")]
    if "CurtainWallFrameSolidBuilder" in body or "CurtainWallPathFrameSolidBuilder" in body:
        errors.append("canonical host Build3D must not append curtain detail transactions without a shared rollback contract; use QS3DCURTAIN3D for frame overlays")

    finalize = text[text.find("private static void FinalizeUi"):text.find("private static bool IsWallCategory")]
    if "catch (Exception ex)" not in finalize or "TryWriteMessage" not in finalize:
        errors.append("post-commit Build3D UI synchronization must be non-fatal and best-effort")

review = SRC / "ReviewCommands.cs"
if review.is_file() and 'CommandMethod("QS3DBUILD3D"' in review.read_text(encoding="utf-8"):
    errors.append("legacy ReviewCommands must not register QS3DBUILD3D")

print("QS3D canonical Build3D preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DBUILD3D has one canonical owner, validates one wall source type, dispatches WallPier deterministically and keeps post-commit UI failures non-fatal.")
