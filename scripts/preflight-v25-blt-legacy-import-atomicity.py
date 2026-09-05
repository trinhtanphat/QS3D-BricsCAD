#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BLT_REL = "src/QS3D.BricsCAD.V25/BltLegacyCommands.cs"
CAPTURE_REL = "src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs"


def fail(message: str) -> None:
    raise SystemExit("FAIL: " + message)


def require(source: str, needle: str, message: str) -> int:
    pos = source.find(needle)
    if pos < 0:
        fail(message)
    return pos


def main() -> int:
    blt_path = ROOT / BLT_REL
    capture_path = ROOT / CAPTURE_REL
    if not blt_path.exists():
        fail(f"missing required source: {BLT_REL}")
    if not capture_path.exists():
        fail(f"missing required source: {CAPTURE_REL}")

    blt = blt_path.read_text(encoding="utf-8")
    capture = capture_path.read_text(encoding="utf-8")

    import_start = require(blt, '[CommandMethod("QS3DBLTIMPORT"', "missing BLT import command")
    import_end = require(blt, "private static void ApplyLegacyEvidence", "missing BLT evidence method boundary")
    import_method = blt[import_start:import_end]

    require(import_method, "SemanticCaptureService.CaptureSnapshot(", "BLT import must use semantic capture service")
    require(import_method, "project => ApplyLegacyEvidence(project, candidate)", "BLT evidence mutation must run as post-capture mutation inside capture rollback boundary")
    if "ApplyLegacyEvidence(document, candidate);" in import_method:
        fail("BLT import must not mutate evidence out-of-band after semantic capture returns")

    apply_start = require(blt, "private static void ApplyLegacyEvidence", "missing BLT evidence application method")
    apply_end = require(blt, "private static void WriteSummary", "missing BLT evidence application boundary")
    apply_method = blt[apply_start:apply_end]
    require(apply_method, "ProjectState project", "BLT evidence mutation must consume the project already protected by capture rollback")
    if "ExistingProjectMutationContext.Require" in apply_method:
        fail("atomic BLT evidence mutation must not reacquire an out-of-band project mutation context")

    overload = "public static bool CaptureSnapshot(Document document, EntitySnapshot snapshot, ElementCategory category, Action<ProjectState> postCaptureMutation)"
    overload_pos = require(capture, overload, "missing atomic single-snapshot capture overload with bounded post-capture mutation")
    next_boundary = capture.find("private static void EnsureCapturePreflight", overload_pos)
    if next_boundary < 0:
        fail("missing semantic capture overload boundary")
    method = capture[overload_pos:next_boundary]

    for needle, message in (
        ("postCaptureMutation == null", "atomic capture overload must reject a null post-capture mutation"),
        ("var projectExistedBeforeCapture = ProjectContextCoordinator.TryGetReadOnly", "atomic capture must remember whether project context existed"),
        ("var rollback = ProjectStateSnapshot.Capture(project)", "atomic capture must snapshot project state before mutation"),
        ("var captured = CaptureSnapshotCore(document, project, snapshot, category)", "atomic capture must run normal capture core"),
        ("RefreshStructuralWallConcreteContacts(document, project)", "atomic capture must preserve structural-wall contact refresh semantics"),
        ("postCaptureMutation(project)", "post-capture mutation must run inside atomic capture method"),
        ("RestoreCaptureOrThrow(document, project, rollback, projectExistedBeforeCapture, operationError", "post-capture failures must use the existing exact restore/forget path"),
    ):
        require(method, needle, message)

    capture_pos = require(method, "var captured = CaptureSnapshotCore", "missing capture core")
    refresh_pos = require(method, "RefreshStructuralWallConcreteContacts(document, project)", "missing structural-wall contact refresh")
    callback_pos = require(method, "postCaptureMutation(project)", "missing post-capture callback")
    catch_pos = require(method, "catch (Exception operationError)", "missing shared rollback catch")
    if not capture_pos < refresh_pos < callback_pos < catch_pos:
        fail("atomic capture must preserve capture -> structural refresh -> BLT evidence override order inside the shared rollback boundary")

    print("PASS: V25 BLT semantic capture and legacy evidence mutation share one rollback boundary without changing override order.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
