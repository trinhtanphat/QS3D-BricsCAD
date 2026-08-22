#!/usr/bin/env python3
from pathlib import Path
import sys

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "src" / "QS3D.BricsCAD.V25" / "DirectDrawReferenceWallCommands.cs"
errors = []

if not TARGET.is_file():
    errors.append("missing DirectDrawReferenceWallCommands.cs")
else:
    text = TARGET.read_text(encoding="utf-8")
    required = [
        '[CommandMethod("QS3DDRAWWALLREF", CommandFlags.Modal | CommandFlags.UsePickSet)]',
        'AcquireReferenceLine(document)',
        'Chọn LINE tham chiếu cho Tường KT',
        'ReadReferenceLine(document, result.ObjectId, failIfNotLine: true)',
        'transaction.GetObject(objectId, OpenMode.ForRead) as Line',
        'PromptPositiveMeters(document.Editor, "Chiều dài Tường (m)", lengthM)',
        '"Bề dày Tường (m)"',
        '"Chiều cao Tường (m)"',
        'reference.CreateCenteredEndpoints(document, lengthM)',
        'CreateWcsLine(document, endpoints.Start, endpoints.End)',
        'SemanticCaptureService.Capture(document, ElementCategory.ArchitecturalWall)',
        '.RegenerateDirtySubset(project, new[] { createdElementId })',
        'WallSolidBuilder.BuildSelectedLineWalls(document, project, ElementCategory.ArchitecturalWall)',
        'GeneratedSolidHandle',
        'ProjectStateSnapshot.Capture(project)',
        'EraseCreatedCad(document, project, createdElement, sourceId, generatedHandles)',
        'rollback.Restore(project)',
        'GeneratedGeometryService.RequireMatchingOwnership(',
        'element.SetProperty("QS3D.DirectDraw.Mode", "ReferenceLine")',
        'CadGeometryGuard.Hypot(dx, dy, "Reference wall / planar length drawing units")',
    ]
    for needle in required:
        if needle not in text:
            errors.append("missing reference-wall contract token: " + needle)

    reference_index = text.find('AcquireReferenceLine(document)')
    length_index = text.find('PromptPositiveMeters(document.Editor, "Chiều dài Tường (m)", lengthM)')
    thickness_index = text.find('"Bề dày Tường (m)"')
    height_index = text.find('"Chiều cao Tường (m)"')
    create_index = text.find('CreateWcsLine(document, endpoints.Start, endpoints.End)')
    capture_index = text.find('SemanticCaptureService.Capture(document, ElementCategory.ArchitecturalWall)')
    regen_index = text.find('.RegenerateDirtySubset(project, new[] { createdElementId })')
    build_index = text.find('WallSolidBuilder.BuildSelectedLineWalls(document, project, ElementCategory.ArchitecturalWall)')
    if min(reference_index, length_index, thickness_index, height_index, create_index, capture_index, regen_index, build_index) < 0:
        errors.append("cannot verify reference -> dimensions -> source -> capture -> regenerate -> build ordering")
    elif not reference_index < length_index < thickness_index < height_index < create_index < capture_index < regen_index < build_index:
        errors.append("reference-wall workflow order regressed")

    helper_index = text.find('private static ReferenceLinePlan? ReadReferenceLine')
    reference_read = text.find('transaction.GetObject(objectId, OpenMode.ForRead) as Line', helper_index)
    source_create = text.find('private static ObjectId CreateWcsLine')
    if helper_index < 0 or reference_read < 0 or source_create < 0 or not reference_read < source_create:
        errors.append("reference LINE helper must read the selected/preselected LINE read-only and remain distinct from the created source LINE")

    forbidden = [
        'ReferenceHandle',
        'reference.Handle',
        'result.ObjectId.Handle',
        'OpenMode.ForWrite) as Line',
        'new Vector3d(dx, dy, 0d).Length',
    ]
    for needle in forbidden:
        if needle in text:
            errors.append("reference-wall must not persist/mutate CAD reference identity or use unstable planar-length arithmetic: " + needle)

    rollback_index = text.find('var rollback = ProjectStateSnapshot.Capture(project);')
    create_source_index = text.find('sourceId = createSource();')
    if rollback_index < 0 or create_source_index < 0 or not rollback_index < create_source_index:
        errors.append("semantic rollback snapshot must be captured before source CAD is created")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: reference-driven wall flow supports PICKFIRST/prompt fallback through one read-only helper, remains dimension-first after selection, numerically guarded, ownership-safe, and rollback-covered.")
