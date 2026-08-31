#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "RoomFinishScheduleCommands.cs"
V26 = ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise AssertionError(f"missing {label}: {token}")


def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        raise AssertionError(f"forbidden {label}: {token}")


def main() -> int:
    source = SOURCE.read_text(encoding="utf-8")
    v26 = V26.read_text(encoding="utf-8")

    require(source, "var primaryAccumulator = new QuantityReportMath.FiniteAccumulator();", "compensated primary accumulator")
    require(source, "QuantityReportMath.NonNegative(row.PrimaryQuantity, \"HT_Phòng export primary quantity\")", "non-negative primary validation")
    require(source, "primaryAccumulator.Add(primaryQuantity, \"HT_Phòng export primary quantity\");", "compensated row accumulation")
    require(source, "primaryAccumulator.Value(\"HT_Phòng export primary quantity\")", "single finalized primary total")
    require(source, "count = QuantityReportMath.AddCount(count, row.Count);", "checked count aggregation")
    forbid(source, "primary = QuantityReportMath.Add(primary, row.PrimaryQuantity", "pairwise strict status folding")

    require(v26, "..\\QS3D.BricsCAD.V25\\**\\*.cs", "V26 shared V25 source inclusion")

    print("PASS: Room Finish export status uses compensated finite aggregation with non-negative validation and V25/V26 shared-source parity.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:
        print(f"ERROR: room finish export status aggregation precision preflight failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
