#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Geometry" / "GridDimensionChainPlanner.cs"


def require(text: str, marker: str, label: str) -> None:
    if marker not in text:
        raise SystemExit(f"ERROR: Grid dimension-chain guard missing {label}: {marker}")


def forbid(text: str, marker: str, label: str) -> None:
    if marker in text:
        raise SystemExit(f"ERROR: Grid dimension-chain guard forbids {label}: {marker}")


def main() -> None:
    if not SOURCE.exists():
        raise SystemExit(f"ERROR: missing source {SOURCE.relative_to(ROOT)}")
    text = SOURCE.read_text(encoding="utf-8")

    require(text, "BuildAdjacentSpans(", "straight Grid adjacent spacing API")
    require(text, "GridSpatialOrderingPlanner.OrderParallelLines(", "canonical bounded spatial-ordering reuse")
    require(text, "orderingAxis", "explicit ordering axis")
    require(text, "descending", "ascending/descending review order")
    require(text, "ordered.Count < 2", "minimum ordered chain cardinality")
    require(text, "var spacing = Math.Abs(signedSpacing);", "positive Grid spacing")
    require(text, "spacing > coordinateTolerance", "spacing ambiguity rejection")
    require(text, "spans.Count != ordered.Count - 1", "N-to-N-minus-one dimension cardinality")
    require(text, "first.ElementId", "first stable identity")
    require(text, "second.ElementId", "second stable identity")
    require(text, "first.Coordinate", "first ordered coordinate")
    require(text, "second.Coordinate", "second ordered coordinate")
    require(text, "return spans.AsReadOnly();", "immutable dimension plan")

    forbid(text, "curves.ToList()", "parallel unbounded materialization outside canonical ordering")
    forbid(text, "new AlignedDimension", "native aligned Dimension entity creation")
    forbid(text, "new RotatedDimension", "native rotated Dimension entity creation")
    forbid(text, "DimensionStyleTable", "native Dimension style mutation")
    forbid(text, "Teigha.", "CAD/vendor dependency in Core")
    forbid(text, "Bricscad.", "BricsCAD dependency in Core")

    print("PASS Grid straight dimension-chain source guard")


if __name__ == "__main__":
    main()
