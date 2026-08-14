#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "WallJunctionCommands.cs"


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"missing {label}: {needle}")


def reject(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"forbidden {label}: {needle}")


def main() -> int:
    if not SOURCE.is_file():
        raise AssertionError(f"missing wall junction source: {SOURCE.relative_to(ROOT)}")
    source = SOURCE.read_text(encoding="utf-8")

    require(source, "out int skippedClosedCount", "closed-polyline skip counter")
    require(source, "skippedClosedCount++;", "closed-polyline skip increment")
    require(source, "if (polyline.Closed)", "closed-polyline detection")
    require(source, "continue;", "mixed-selection continuation")
    require(source, "closed POLYLINE không phải wall centerline", "nonfatal closed-polyline diagnostic")
    reject(source, "closed polyline cần tách trước", "selection-wide closed-polyline abort")

    closed_pos = source.find("if (polyline.Closed)")
    increment_pos = source.find("skippedClosedCount++;", closed_pos)
    continue_pos = source.find("continue;", increment_pos)
    normal_pos = source.find("var normal = polyline.Normal;", closed_pos)
    if min(closed_pos, increment_pos, continue_pos, normal_pos) < 0 or not (closed_pos < increment_pos < continue_pos < normal_pos):
        raise AssertionError("closed POLYLINE must be counted and skipped before open-centerline validation")

    print("PASS: wall junction mixed selections skip closed polylines without aborting usable open centerlines.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
