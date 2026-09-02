#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "GridDirectDrawCommands.cs"
V26 = ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj"


def require(text: str, marker: str, label: str) -> None:
    if marker not in text:
        raise SystemExit(f"ERROR: Grid Direct Draw guard missing {label}: {marker}")


def forbid(text: str, marker: str, label: str) -> None:
    if marker in text:
        raise SystemExit(f"ERROR: Grid Direct Draw guard forbids {label}: {marker}")


def main() -> None:
    if not SOURCE.exists():
        raise SystemExit(f"ERROR: missing Grid Direct Draw source: {SOURCE.relative_to(ROOT)}")
    text = SOURCE.read_text(encoding="utf-8")

    require(text, '[CommandMethod("QS3DGRIDDRAW", CommandFlags.Modal)]', "dedicated repeated command")
    require(text, 'while (true)', "repeated authoring loop")
    require(text, 'AllowNone = true', "Enter-to-finish start prompt")
    require(text, 'family.Category != ElementCategory.Grid', "active Grid Family gate")
    require(text, 'FamilyNameHasSubtype(family.Name, "Lưới Cong")', "straight-vs-curved fail-closed gate")
    require(text, 'RequireModelSpace(document);', "Model Space gate")
    require(text, 'CurrentUserCoordinateSystem', "UCS freshness capture")
    require(text, 'ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)', "active-DWG freshness gate")
    require(text, 'project.ChangeVersion != expected.ChangeVersion', "project revision freshness gate")
    require(text, 'EntitySnapshotReader.ReadHandles(document, new[] { source.Handle })', "canonical native snapshot reread")
    require(text, 'SemanticCaptureService.CaptureSnapshot(document, snapshots[0], ElementCategory.Grid)', "existing semantic capture authority")
    require(text, 'CompensateSourceOrThrow(document, source, captureError)', "exact native-source compensation")
    require(text, 'var ownerId = line.OwnerId;', "created-source owner capture")
    require(text, 'entity.Handle.ToString(), source.Handle', "compensation handle revalidation")
    require(text, 'entity.OwnerId != source.OwnerId', "compensation owner-space revalidation")
    require(text, 'as Line;', "compensation LINE type revalidation")
    require(text, 'entity.Erase();', "failed-capture source cleanup")
    require(text, 'SetImpliedSelection(new[] { sourceId })', "accepted-source review selection")
    require(text, 'native/semantic state đã được fail-closed', "stable redacted operation failure")
    require(text, 'native + semantic source đã commit; một phần UI review không thể đồng bộ', "post-commit UI isolation warning")

    forbid(text, '"QS3DGRIDDRAW lỗi: " + ex.Message', "raw command exception detail")
    forbid(text, '"\\nQS3D Grid UI sync warning: " + uiError.Message', "raw post-commit UI exception detail")

    # This lane must remain a source-authoring adapter only. It must not grow a competing
    # system/intersection/numbering engine or generated Grid 3D authority.
    forbid(text, 'GridSystemPlanner', "rectangular/radial system planner takeover")
    forbid(text, 'GridIntersectionPlanner', "intersection engine takeover")
    forbid(text, 'GridNamingService', "numbering engine takeover")
    forbid(text, 'Solid3d', "generated Grid 3D geometry")

    if not V26.exists():
        raise SystemExit("ERROR: missing V26 project")
    v26 = V26.read_text(encoding="utf-8")
    require(v26, '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"', "V26 shared-source parity")

    print("PASS Grid Direct Draw repeated-authoring compensation/redaction source guard")


if __name__ == "__main__":
    main()
