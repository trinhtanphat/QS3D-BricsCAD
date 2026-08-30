#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Geometry" / "GridRadialOrderingPlanner.cs"


def require(text: str, marker: str, label: str) -> None:
    if marker not in text:
        raise SystemExit(f"ERROR: Grid radial ordering guard missing {label}: {marker}")


def forbid(text: str, marker: str, label: str) -> None:
    if marker in text:
        raise SystemExit(f"ERROR: Grid radial ordering guard forbids {label}: {marker}")


def main() -> None:
    if not SOURCE.exists():
        raise SystemExit(f"ERROR: missing source {SOURCE.relative_to(ROOT)}")
    text = SOURCE.read_text(encoding="utf-8")

    require(text, "OrderConcentricArcs(", "public concentric ARC ordering API")
    require(text, "GridReferenceCurveKind.Arc", "ARC-only policy")
    require(text, "new HashSet<string>(StringComparer.OrdinalIgnoreCase)", "case-insensitive unique identity guard")
    require(text, "distance > centerTolerance", "concentric-center rejection")
    require(text, "curve.Radius > radiusTolerance", "positive finite radius boundary")
    require(text, "sweep > TwoPi + AngleTolerance", "bounded ARC sweep")
    require(text, "entries.Sort", "deterministic radius ordering")
    require(text, "Math.Abs(delta) <= radiusTolerance", "near-equal radius ambiguity rejection")
    require(text, "if (descending) entries.Reverse();", "explicit descending ordering")
    require(text, 'GridSnapInputMaterializer.Materialize(curves, MaxCurves, "Grid radial ordering input")', "Count-aware bounded input materialization")

    forbid(text, "Take(MaxCurves + 1)", "legacy one-past LINQ traversal")
    forbid(text, "GridSystemPlanner", "system-plan engine takeover")
    forbid(text, "GridIntersectionPlanner", "intersection-engine takeover")
    forbid(text, "Teigha.", "CAD/vendor dependency in Core")
    forbid(text, "Bricscad.", "BricsCAD dependency in Core")

    print("PASS Grid concentric ARC ordering source guard")


if __name__ == "__main__":
    main()
