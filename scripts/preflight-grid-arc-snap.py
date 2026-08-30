#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Geometry" / "GridArcSnapPlanner.cs"


def require(text: str, marker: str, label: str) -> None:
    if marker not in text:
        raise SystemExit(f"ERROR: Grid ARC snap guard missing {label}: {marker}")


def forbid(text: str, marker: str, label: str) -> None:
    if marker in text:
        raise SystemExit(f"ERROR: Grid ARC snap guard forbids {label}: {marker}")


def main() -> None:
    if not SOURCE.exists():
        raise SystemExit(f"ERROR: missing source {SOURCE.relative_to(ROOT)}")
    text = SOURCE.read_text(encoding="utf-8")

    require(text, "TryFindNearest(", "nearest ARC snap API")
    require(text, "GridReferenceCurveKind.Arc", "ARC-only policy")
    require(text, "GridSnapInputMaterializer.Materialize(curves, MaxCurves, \"Grid ARC snap input\")", "shared bounded input admission")
    require(text, "new HashSet<string>(StringComparer.OrdinalIgnoreCase)", "duplicate identity rejection")
    require(text, "AngleWithinSweep", "finite sweep membership")
    require(text, "arc.Radius / radialDistance", "support-circle radial projection")
    require(text, "arc.Start, startDistance", "finite endpoint fallback")
    require(text, "arc.End, endDistance", "finite endpoint fallback")
    require(text, "if (first.Distance > maxDistance) return false;", "explicit no-match range")
    require(text, "Math.Abs(delta) <= ambiguityTolerance", "cross-Grid near-tie fail closed")
    require(text, "endpointDelta <= geometryTolerance", "same-ARC endpoint ambiguity rejection")
    require(text, "sweep >= TwoPi - angleTolerance", "full/over-sweep rejection")

    forbid(text, "Take(MaxCurves + 1)", "caller Current materialization before bounded admission")
    forbid(text, "GridSystemPlanner", "system planner takeover")
    forbid(text, "GridIntersectionPlanner", "intersection planner takeover")
    forbid(text, "Teigha.", "CAD/vendor dependency in Core")
    forbid(text, "Bricscad.", "BricsCAD dependency in Core")

    print("PASS Grid finite ARC snap planner source guard")


if __name__ == "__main__":
    main()
