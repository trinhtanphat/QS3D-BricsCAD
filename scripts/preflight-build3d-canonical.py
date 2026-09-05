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
        owners.append(path.relative_to(ROOT).as_posix())

expected = "src/QS3D.BricsCAD.V25/Build3DCommands.cs"
if owners != [expected]:
    errors.append("QS3DBUILD3D must have exactly one canonical registration in Build3DCommands.cs; found: " + ", ".join(owners))

capability = SRC / "Cad/NativeBuildCapability.cs"
if not capability.is_file():
    errors.append("missing centralized native build capability: src/QS3D.BricsCAD.V25/Cad/NativeBuildCapability.cs")
else:
    capability_text = capability.read_text(encoding="utf-8")
    for token in (
        "internal static class NativeBuildCapability",
        "Supports(ElementCategory category)",
        "IsWallCategory(ElementCategory category)",
        "StructuralSolidBuilder.Supports(category)",
        "ElementCategory.ArchitecturalWall",
        "ElementCategory.GlassWall",
        "ElementCategory.WallPier",
    ):
        if token not in capability_text:
            errors.append("NativeBuildCapability missing contract: " + token)

build = ROOT / expected
if not build.is_file():
    errors.append("missing canonical Build3DCommands.cs")
else:
    text = build.read_text(encoding="utf-8")
    required = (
        "SemanticReferenceHandles.MatchesSelection(x, handles)",
        "NativeBuildCapability.Supports(x.Category)",
        "NativeBuildCapability.IsWallCategory(category)",
        "var sourceIds = CadHandleService.Resolve(document, sourceHandles)",
        "EntitySnapshotReader.ReadHandles(document, sourceHandles)",
        "ValidateWallSourceBatch(selectedElements, sourceSnapshots, category",
        ".RegenerateDirtySubset(project, regenerationScope)",
        "document.Editor.SetImpliedSelection(sourceIds.ToArray())",
        "BuildCategory(document, project, category, sourceType)",
        "if (sourceTypes.Count == 0)",
        "FinalizeUi(document, elementIds, sourceHandles, built, regenerated, category, project)",
        "CadHandleService.Select(document, generatedHandles)",
        "Build 3D UI sync warning: native rebuild đã commit; một phần viewport/selection/UI không thể đồng bộ.",
        "Report(document, \"QS3DBUILD3D lỗi: không thể hoàn tất native rebuild cho selection hiện tại.\")",
        "native ownership đã thay đổi trước lỗi post-commit; giữ trạng thái đã commit để tránh lệch CAD/semantic.",
        'string.Equals(sourceType, "Line", StringComparison.OrdinalIgnoreCase)',
        "category == ElementCategory.WallPier",
        "WallPierProfileSolidBuilder.BuildSelectedLinePiers(document, project)",
        'string.Equals(sourceType, "Polyline", StringComparison.OrdinalIgnoreCase)',
        "PolylineWallSolidBuilder.BuildSelected(document, project, category)",
    )
    for token in required:
        if token not in text:
            errors.append("canonical Build3D missing contract: " + token)

    for forbidden in ("operationError.Message", "ex.Message", "uiError.Message", "exception.Message", "Exception.Message"):
        if forbidden in text:
            errors.append("canonical Build3D must not expose raw caught exception detail: " + forbidden)

    resolve_sources = text.find("var sourceIds = CadHandleService.Resolve(document, sourceHandles)")
    direct_snapshots = text.find("var sourceSnapshots = EntitySnapshotReader.ReadHandles(document, sourceHandles)")
    validate_call = text.find("if (!ValidateWallSourceBatch(selectedElements, sourceSnapshots, category")
    regenerate = text.find(".RegenerateDirtySubset(project, regenerationScope)")
    select_sources = text.find("document.Editor.SetImpliedSelection(sourceIds.ToArray())")
    build_dispatch = text.find("built = BuildCategory(document, project, category, sourceType)")
    if min(resolve_sources, direct_snapshots, validate_call, regenerate, select_sources, build_dispatch) < 0 or not (
        resolve_sources < direct_snapshots < validate_call < regenerate < select_sources < build_dispatch
    ):
        errors.append("Build3D must keep source validation read-only, then hand resolved source IDs to implied-selection builders only after regeneration and immediately before native dispatch")
    if text.count("document.Editor.SetImpliedSelection(sourceIds.ToArray())") != 1:
        errors.append("Build3D must have exactly one resolved-source implied-selection handoff")
    if "EntitySnapshotReader.ReadImpliedSelection(document)" in text:
        errors.append("Build3D source preflight must read resolved source handles directly instead of depending on implied-selection mutation")

    if re.search(r"private\s+static\s+bool\s+IsNativeBuildCategory\s*\(", text):
        errors.append("Build3DCommands must not duplicate NativeBuildCapability.Supports")
    if re.search(r"private\s+static\s+bool\s+IsWallCategory\s*\(", text):
        errors.append("Build3DCommands must not duplicate NativeBuildCapability.IsWallCategory")

    body_start = text.find("private static int BuildCategory")
    finalize_start = text.find("private static void FinalizeUi")
    if body_start < 0 or finalize_start < 0 or finalize_start <= body_start:
        errors.append("canonical Build3D dispatch/finalize boundaries are missing or out of order")
    else:
        body = text[body_start:finalize_start]
        if "CurtainWallFrameSolidBuilder" in body or "CurtainWallPathFrameSolidBuilder" in body:
            errors.append("canonical host Build3D must not append curtain detail transactions without a shared rollback contract; use QS3DCURTAIN3D for frame overlays")

        report_start = text.find("private static void Report", finalize_start)
        finalize = text[finalize_start:report_start if report_start > finalize_start else len(text)]
        if "var uiSyncFailed = false;" not in finalize or "TryWriteMessage" not in finalize:
            errors.append("post-commit Build3D UI synchronization must remain non-fatal and best-effort")
        if finalize.count("catch") < 5:
            errors.append("post-commit Build3D Palette/Regen/selection/status/editor cells must fail independently")

review = SRC / "ReviewCommands.cs"
if review.is_file() and 'CommandMethod("QS3DBUILD3D"' in review.read_text(encoding="utf-8"):
    errors.append("legacy ReviewCommands must not register QS3DBUILD3D")

print("QS3D canonical Build3D preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DBUILD3D keeps source preflight read-only, late native dispatch, semantic rollback versus committed ownership truth, stable failure redaction, and independently non-fatal committed-state UI sync.")
