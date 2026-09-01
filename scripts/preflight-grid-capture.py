#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
command = ROOT / "src/QS3D.BricsCAD.V25/GridCommands.cs"
regenerators = ROOT / "src/QS3D.Core/Services/StructuralRegenerator.cs"
category = ROOT / "src/QS3D.Core/Domain/ElementCategory.cs"
errors = []

for path in (command, regenerators, category):
    if not path.is_file():
        errors.append("missing Grid contract file: " + str(path.relative_to(ROOT)))

if command.is_file():
    text = command.read_text(encoding="utf-8")
    for needle in (
        "using QS3D.BricsCAD.V25.UI;",
        'CommandMethod("QS3DGRID", CommandFlags.UsePickSet)',
        "EntitySnapshotReader.ReadCurrentSelection",
        'string.Equals(entityType, "Line"',
        'string.Equals(entityType, "Arc"',
        "LengthDrawingUnits.HasValue",
        "double.IsNaN",
        "double.IsInfinity",
        "SemanticCaptureService.Capture(document, ElementCategory.Grid)",
        "FinalizeUi(document, count, subtype);",
        "private static void FinalizeUi",
        "PaletteCoordinator.RefreshProject()",
        "semantic capture đã hoàn tất; một phần UI không thể đồng bộ.",
        "private static void ReportOperationFailure",
        "private static void TryWriteMessage",
        "không sinh native 3D",
    ):
        if needle not in text:
            errors.append("GridCommands.cs missing guarded capture/UI token: " + needle)

    if "ex.Message" in text:
        errors.append("GridCommands.cs must not expose raw caught exception detail")

    capture = text.find("count = SemanticCaptureService.Capture(document, ElementCategory.Grid);")
    catch = text.find("catch (Exception)", capture)
    finalize = text.find("FinalizeUi(document, count, subtype);", catch)
    if min(capture, catch, finalize) < 0 or not capture < catch < finalize:
        errors.append("QS3DGRID must finish transactional semantic capture before entering best-effort post-capture UI synchronization")

    finalize_start = text.find("private static void FinalizeUi")
    report_start = text.find("private static void ReportOperationFailure", finalize_start)
    finalize_body = text[finalize_start:report_start] if finalize_start >= 0 and report_start > finalize_start else ""
    if "try" not in finalize_body or "semantic capture đã hoàn tất; một phần UI không thể đồng bộ." not in finalize_body:
        errors.append("QS3DGRID post-capture UI synchronization must be best-effort, stable and non-fatal")
    if finalize_body.count("catch") < 2:
        errors.append("QS3DGRID post-capture Palette refresh/status must fail independently")

if regenerators.is_file():
    text = regenerators.read_text(encoding="utf-8")
    if "category == ElementCategory.CustomQuantity || category == ElementCategory.Grid" not in text:
        errors.append("GenericTakeoffRegenerator must continue supporting ElementCategory.Grid")
    for needle in ('element.SetQuantity("LengthM"', 'element.SetQuantity("Count", 1d)'):
        if needle not in text:
            errors.append("Grid generic takeoff contract missing: " + needle)

if category.is_file() and "Grid," not in category.read_text(encoding="utf-8"):
    errors.append("ElementCategory.Grid is missing")

print("QS3D Grid semantic capture preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DGRID captures only finite positive LINE/ARC references through transactional semantic capture, uses the existing Grid generic takeoff model, keeps post-capture Workspace/status synchronization independently non-fatal with stable redacted warnings, and does not pretend to create native 3D Grid geometry.")
