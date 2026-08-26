#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PLAN = ROOT / "src" / "QS3D.Core" / "Geometry" / "GridSystemMaterializationPlan.cs"
NATIVE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad" / "GridSystemNativeMaterializer.cs"
COMMANDS = ROOT / "src" / "QS3D.BricsCAD.V25" / "GridSystemCreationCommands.cs"
V26 = ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj"


def require(text: str, marker: str, label: str) -> None:
    if marker not in text:
        raise SystemExit(f"ERROR: Grid system materialization guard missing {label}: {marker}")


def forbid(text: str, marker: str, label: str) -> None:
    if marker in text:
        raise SystemExit(f"ERROR: Grid system materialization guard forbids {label}: {marker}")


def read(path: Path) -> str:
    if not path.exists():
        raise SystemExit(f"ERROR: missing source {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def main() -> None:
    plan = read(PLAN)
    native = read(NATIVE)
    commands = read(COMMANDS)
    v26 = read(V26)

    require(plan, "private const int MaxCurves = 2000;", "bounded plan cardinality")
    require(plan, "new HashSet<string>(StringComparer.OrdinalIgnoreCase)", "case-insensitive semantic id uniqueness")
    require(plan, "ValidateCurve(curve, index);", "fail-closed geometry validation")
    require(plan, "degenerate LINE", "degenerate LINE rejection")
    require(plan, "ARC sweep must be in (0, 2π]", "ARC sweep validation")

    require(native, "GridSystemMaterializationPlan.Create(plannedCurves)", "canonical plan validation")
    require(native, "EnsureSemanticIdsAvailable(project, plan);", "idempotent existing-id rejection")
    require(native, "transaction.Commit();", "native batch commit boundary")
    require(native, "SemanticCaptureService.CaptureSnapshot", "canonical Grid capture reuse")
    require(native, "EraseCreatedSources(document, created);", "native rollback cleanup")
    require(native, "rollback.Restore(project);", "semantic rollback")
    require(native, "new AggregateException(failures)", "rollback failure fail-closed aggregation")
    forbid(native, "GridIntersectionPlanner", "intersection-marker lane takeover")

    require(commands, '[CommandMethod("QS3DGRIDSYSTEMRECT")]', "reviewed rectangular creation route")
    require(commands, '[CommandMethod("QS3DGRIDSYSTEMRADIAL")]', "reviewed radial creation route")
    require(commands, "GridSystemPlanner.PlanRectangular", "canonical rectangular planner reuse")
    require(commands, "GridSystemPlanner.PlanRadial", "canonical radial planner reuse")
    require(commands, "GridSystemNativeMaterializer.Materialize", "native materializer route")
    require(commands, "Create? [Yes/No] <No>", "explicit review confirmation")
    require(commands, "AllowNone = true", "safe default confirmation/cancel behavior")
    require(commands, "MaxStationsPerFamily = 1000", "bounded family cardinality")
    forbid(commands, "GridIntersectionPlanner", "intersection-marker command takeover")

    require(v26, '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"', "V25/V26 shared-source parity")

    print("PASS Grid system native materialization source guard")


if __name__ == "__main__":
    main()
