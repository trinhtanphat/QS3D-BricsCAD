#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
path = ROOT / "src/QS3D.BricsCAD.V25/Reporting/SnapshotQuantityAdapter.cs"
errors = []

if not path.is_file():
    errors.append("missing SnapshotQuantityAdapter.cs")
else:
    text = path.read_text(encoding="utf-8")
    for needle in (
        "checked(row.Count + 1)",
        "AddFinite(row.LengthM",
        "AddFinite(row.SideAreaM2",
        "AddFinite(row.NetConcreteM3",
        "throw new OverflowException(\"Snapshot quantity total overflow",
        "StringComparison.OrdinalIgnoreCase",
        "Snapshot quantity input cannot contain null items",
    ):
        if needle not in text:
            errors.append("SnapshotQuantityAdapter missing numeric/integrity guard: " + needle)
    for unsafe in ("row.Count++;", "row.LengthM +=", "row.SideAreaM2 +=", "row.NetConcreteM3 +="):
        if unsafe in text:
            errors.append("SnapshotQuantityAdapter reintroduced unchecked aggregation: " + unsafe)

print("QS3D snapshot quantity adapter preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: raw CAD snapshot quantity grouping uses checked counts, finite-safe totals and case-insensitive handle dedupe.")
