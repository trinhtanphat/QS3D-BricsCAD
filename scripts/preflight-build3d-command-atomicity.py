#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/Build3DCommands.cs"
errors = []

if not COMMAND.is_file():
    errors.append("missing Build3DCommands.cs")
else:
    text = COMMAND.read_text(encoding="utf-8")
    required = (
        "using QS3D.Core.Persistence;",
        "var semanticRollback = ProjectStateSnapshot.Capture(project);",
        "var ownershipBefore = CaptureGeneratedSolidHandles(project, elementIds);",
        "if (GeneratedSolidHandlesMatch(project, ownershipBefore))",
        "semanticRollback.Restore(project);",
        "native ownership đã thay đổi trước lỗi post-commit",
        "private static Dictionary<string, string> CaptureGeneratedSolidHandles",
        "private static bool GeneratedSolidHandlesMatch",
        'element.Properties.TryGetValue("GeneratedSolidHandle", out var handle)',
        "FinalizeUi(document, elementIds, sourceHandles, built, regenerated, category, project);",
        ".Select(project.FindElement)",
    )
    for token in required:
        if token not in text:
            errors.append("Build3DCommands.cs missing atomicity token: " + token)

    if "FinalizeUi(document, selectedElements" in text:
        errors.append("Build3DCommands.cs reuses pre-build ProjectElement references in FinalizeUi")

    snapshot_index = text.find("var semanticRollback = ProjectStateSnapshot.Capture(project);")
    regen_index = text.find("RegenerateDirty(project)")
    build_index = text.find("built = BuildCategory(document, project, category, sourceType);")
    restore_index = text.find("semanticRollback.Restore(project);")
    ownership_guard_index = text.find("if (GeneratedSolidHandlesMatch(project, ownershipBefore))")
    if min(snapshot_index, regen_index, build_index, restore_index, ownership_guard_index) >= 0:
        if not snapshot_index < regen_index < build_index:
            errors.append("Build3D snapshot must be captured before regeneration/native build")
        if not ownership_guard_index < restore_index:
            errors.append("Build3D semantic restore must remain guarded by unchanged generated ownership")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] QS3DBUILD3D guards pre-commit semantic rollback with generated-solid ownership and avoids stale ProjectElement reuse")
