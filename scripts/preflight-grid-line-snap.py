#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Geometry" / "GridLineSnapPlanner.cs"


def require(text: str, marker: str, label: str) -> None:
    if marker not in text:
        raise SystemExit(f"ERROR: Grid line snap guard missing {label}: {marker}")


def forbid(text: str, marker: str, label: str) -> None:
    if marker in text:
        raise SystemExit(f"ERROR: Grid line snap guard forbids {label}: {marker}")


def main() -> None:
    if not SOURCE.exists():
        raise SystemExit(f"ERROR: missing source {SOURCE.relative_to(ROOT)}")
    text = SOURCE.read_text(encoding="utf-8")

    require(text, "TryFindNearest(", "nearest snap API")
    require(text, "GridReferenceCurveKind.Line", "LINE-only policy")
    require(text, "GridSnapInputMaterializer.Materialize(curves, MaxCurves, \"Grid line snap input\")", "shared bounded input admission")
    require(text, "new HashSet<string>(StringComparer.OrdinalIgnoreCase)", "duplicate identity rejection")
    require(text, "NearestOnFiniteSegment", "finite-segment projection")
    require(text, "if (along < 0.0) along = 0.0;", "start clamp")
    require(text, "else if (along > length) along = length;", "end clamp")
    require(text, "if (first.Distance > maxDistance) return false;", "explicit no-match range")
    require(text, "Math.Abs(delta) <= ambiguityTolerance", "near-tie fail closed")
    require(text, "result = first;", "stable accepted result")

    forbid(text, "Take(MaxCurves + 1)", "caller Current materialization before bounded admission")
    forbid(text, "GridSystemPlanner", "system planner takeover")
    forbid(text, "GridIntersectionPlanner", "intersection planner takeover")
    forbid(text, "Teigha.", "CAD/vendor dependency in Core")
    forbid(text, "Bricscad.", "BricsCAD dependency in Core")

    print("PASS Grid LINE snap planner source guard")


if __name__ == "__main__":
    main()
