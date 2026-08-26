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

    require(text, "BuildAdjacentSpans(", "straight Grid adjacent dimension API")
    require(text, "GridSpatialOrderingPlanner.Order(curves, alongAxis, positionTolerance)", "canonical bounded spatial-ordering reuse")
    require(text, "ordered.Count < 2", "minimum ordered chain cardinality")
    require(text, "var spacing = Math.Abs(signedSpacing);", "positive Grid spacing")
    require(text, "spacing > positionTolerance", "spacing ambiguity rejection")
    require(text, "plans.Count != ordered.Count - 1", "N-to-N-minus-one dimension cardinality")
    require(text, "first.AnchorPoint", "first witness point")
    require(text, "second.AnchorPoint", "second witness point")
    require(text, "dimensionLineOrigin", "requested dimension-line origin")
    require(text, "return plans.AsReadOnly();", "immutable dimension plan")

    forbid(text, "curves.ToList()", "unbounded pre-materialization before canonical ordering")
    forbid(text, "new AlignedDimension", "native aligned Dimension entity creation")
    forbid(text, "new RotatedDimension", "native rotated Dimension entity creation")
    forbid(text, "DimensionStyleTable", "native Dimension style mutation")
    forbid(text, "Teigha.", "CAD/vendor dependency in Core")
    forbid(text, "Bricscad.", "BricsCAD dependency in Core")

    print("PASS Grid straight dimension-chain source guard")


if __name__ == "__main__":
    main()
