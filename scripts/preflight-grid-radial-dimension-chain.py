#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Geometry" / "GridRadialDimensionChainPlanner.cs"


def require(text: str, marker: str, label: str) -> None:
    if marker not in text:
        raise SystemExit(f"ERROR: Grid radial dimension guard missing {label}: {marker}")


def forbid(text: str, marker: str, label: str) -> None:
    if marker in text:
        raise SystemExit(f"ERROR: Grid radial dimension guard forbids {label}: {marker}")


def main() -> None:
    if not SOURCE.exists():
        raise SystemExit(f"ERROR: missing source {SOURCE.relative_to(ROOT)}")
    text = SOURCE.read_text(encoding="utf-8")

    require(text, "BuildAdjacentSpans(", "radial adjacent dimension API")
    require(text, "GridRadialOrderingPlanner.OrderConcentricArcs(", "canonical radial ordering reuse")
    require(text, "materialized.Count < 2", "minimum radial chain cardinality")
    require(text, "var spacing = Math.Abs(signedDelta);", "positive radial spacing")
    require(text, "spacing > radiusTolerance", "radial spacing ambiguity rejection")
    require(text, "spans.Count != ordered.Count - 1", "N-to-N-minus-one radial cardinality")
    require(text, "return spans.AsReadOnly();", "immutable radial plan")

    forbid(text, "GridSystemPlanner", "system planner takeover")
    forbid(text, "GridIntersectionPlanner", "intersection planner takeover")
    forbid(text, "new AlignedDimension", "native aligned Dimension entity creation")
    forbid(text, "new RadialDimension", "native radial Dimension entity creation")
    forbid(text, "DimensionStyleTable", "native Dimension style mutation")
    forbid(text, "Teigha.", "CAD/vendor dependency in Core")
    forbid(text, "Bricscad.", "BricsCAD dependency in Core")

    print("PASS Grid radial dimension-chain source guard")


if __name__ == "__main__":
    main()
