#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Reporting/QuantityReportTotals.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QuantityReportTotalsBoundSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "internal const int MaxRows = 10000;",
    "if (knownCount.HasValue && knownCount.Value > MaxRows)",
    "if (rowIndex >= MaxRows)",
    "Quantity report totals support at most \" + MaxRows + \" rows.",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"ERROR: quantity-report totals bound guard missing production token: {token}")

known_admission = source.index("if (knownCount.HasValue && knownCount.Value > MaxRows)")
enumerator_start = source.index("using (var enumerator = rows.GetEnumerator())")
if known_admission > enumerator_start:
    raise SystemExit("ERROR: over-limit known Count must fail before row enumeration starts")

stream_bound = source.index("if (rowIndex >= MaxRows)")
current_read = source.index("var row = enumerator.Current;")
if stream_bound > current_read:
    raise SystemExit("ERROR: streaming row bound must fail before enumerator.Current is observed")

known_overrun = source.index("if (knownCount.HasValue && rowIndex >= knownCount.Value)")
if known_overrun > stream_bound:
    raise SystemExit("ERROR: admitted known-Count overrun contract must retain precedence over the generic row ceiling")

required_smoke = [
    "[ModuleInitializer]",
    "RejectOverLimitKnownCountBeforeEnumeration();",
    "RejectFirstStreamingRowBeyondLimitBeforeCurrent();",
    "AcceptExactStreamingLimit();",
    "new OverLimitKnownCountRows(10001)",
    "new StreamingRows(10001)",
    "source.MoveNextCalls != 10001 || source.CurrentReads != 10000",
    "new StreamingRows(10000)",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"ERROR: quantity-report totals bound smoke missing token: {token}")

print("PASS quantity report totals hostile row bound")
