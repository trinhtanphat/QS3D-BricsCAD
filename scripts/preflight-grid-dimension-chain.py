#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Geometry" / "GridDimensionChainPlanner.cs"


def require(text: str, marker: str, label: str) -> None:
    if marker not in text:
        raise SystemExit(f"ERROR: Grid dimension chain guard missing {label}: {marker}")


def forbid(text: str, marker: str, label: str) -> None:
    if marker in text:
        raise SystemExit(f"ERROR: Grid dimension chain guard forbids {label}: {marker}")


def main() -> None:
    if not SOURCE.exists():
        raise SystemExit(f"ERROR: missing source {SOURCE.relative_to(ROOT)}")
    text = SOURCE.read_text(encoding="utf-8")

    require(text, "BuildAdjacentSpans(", "adjacent dimension planning API")
    require(text, "GridSpatialOrderingPlanner.OrderParallelLines(", "canonical Grid ordering reuse")
    require(text, "materialized.Count < 2", "minimum chain cardinality")
    require(text, "var spacing = Math.Abs(signedDelta);", "positive spacing projection")
    require(text, "spacing > coordinateTolerance", "spacing ambiguity rejection")
    require(text, "spans.Count != ordered.Count - 1", "N-to-N-minus-one cardinality contract")
    require(text, "return spans.AsReadOnly();", "immutable plan handoff")

    forbid(text, "GridSystemPlanner", "system planner takeover")
    forbid(text, "GridIntersectionPlanner", "intersection planner takeover")
    forbid(text, "AlignedDimension", "native aligned Dimension entity creation")
    forbid(text, "RotatedDimension", "native rotated Dimension entity creation")
    forbid(text, "DimensionStyleTable", "native Dimension style mutation")
    forbid(text, "Teigha.", "CAD/vendor dependency in Core")
    forbid(text, "Bricscad.", "BricsCAD dependency in Core")

    print("PASS Grid adjacent dimension-chain source guard")


if __name__ == "__main__":
    main()
