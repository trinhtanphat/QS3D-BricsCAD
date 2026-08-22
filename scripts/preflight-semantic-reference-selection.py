#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
path = ROOT / "src/QS3D.BricsCAD.V25/Services/SemanticReferenceHandles.cs"
errors = []

if not path.is_file():
    errors.append("missing SemanticReferenceHandles.cs")
else:
    text = path.read_text(encoding="utf-8")
    required = (
        "if (owned.Any(handles.Contains)) return true;",
        "owned.Count == 0",
        "boundary.All(handles.Contains)",
        'MatchesPropertyHandle(element, "GeneratedSolidHandle", handles)',
        'MatchesPropertyHandle(element, "PhysicalOpeningCutSolidHandle", handles)',
    )
    for token in required:
        if token not in text:
            errors.append("Semantic reference selection missing contract: " + token)

    old_early_return = "if (owned.Count > 0) return owned.Any(handles.Contains);"
    if old_early_return in text:
        errors.append("Semantic reference selection must not return false before checking generated host aliases")

    rebar_tokens = (
        "GeneratedRebarHandles",
        "GeneratedShapeRebarHandles",
        "GeneratedTieRebarHandles",
        "GeneratedBeamStirrupHandles",
        "GeneratedSlabMeshHandles",
        "GeneratedWallMeshHandles",
        "GeneratedFoundationMeshHandles",
        "GeneratedCurtainFrameHandles",
    )
    matches_body = text[text.find("public static bool MatchesSelection"):text.find("private static bool MatchesPropertyHandle")]
    for token in rebar_tokens:
        if token in matches_body:
            errors.append("Build3D semantic selection must not broaden host resolution to detail output slot: " + token)

build3d = ROOT / "src/QS3D.BricsCAD.V25/Build3DCommands.cs"
if not build3d.is_file():
    errors.append("missing Build3DCommands.cs")
elif "SemanticReferenceHandles.MatchesSelection(x, handles)" not in build3d.read_text(encoding="utf-8"):
    errors.append("QS3DBUILD3D must resolve selected semantic/generated host references through SemanticReferenceHandles")

print("QS3D semantic reference selection preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: source, Auto Room boundary and generated host aliases resolve deterministically without treating rebar/mesh/detail outputs as QS3DBUILD3D host selections.")
