from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Reporting/QuantityReportTotals.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QuantityReportTotalsKnownCountIntegritySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/quantity-report-totals-known-count-integrity.md"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "using (var enumerator = rows.GetEnumerator())",
    "while (true)",
    "RequireKnownRowCountStable(rows, knownCount, knownCountSources);",
    "if (!enumerator.MoveNext())",
    "if (knownCount.HasValue && rowIndex >= knownCount.Value)",
    "var row = enumerator.Current;",
    "SnapshotKnownRowCount(rows, out var currentKnownCountSources)",
    "expectedKnownCountSources != currentKnownCountSources",
    "Quantity report row input Count changed during enumeration.",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"QuantityReportTotals Count-integrity source guard missing token: {token}")

pre_move = source.index("RequireKnownRowCountStable(rows, knownCount, knownCountSources);")
move = source.index("if (!enumerator.MoveNext())", pre_move)
post_move = source.index("RequireKnownRowCountStable(rows, knownCount, knownCountSources);", move + 1)
overrun = source.index("if (knownCount.HasValue && rowIndex >= knownCount.Value)", post_move)
current = source.index("var row = enumerator.Current;", overrun)
if not (pre_move < move < post_move < overrun < current):
    raise SystemExit("QuantityReportTotals must rebind Count around MoveNext and reject known-Count overrun before IEnumerator.Current")
if "foreach (var row in rows)" in source or "while (enumerator.MoveNext())" in source:
    raise SystemExit("QuantityReportTotals must retain explicit pre/post-MoveNext Count admission")

required_smoke = [
    "[ModuleInitializer]",
    "RejectKnownCountOverrunBeforeCurrent",
    "RejectPostTraversalCountDrift",
    "RejectPostTraversalNegativeCount",
    "RejectPostTraversalCountConflict",
    "AcceptStableMultiInterfaceCount",
    "AcceptPureStreamingRows",
    "CurrentReads != 1",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"QuantityReportTotals Count-integrity smoke guard missing token: {token}")

if not RUNBOOK.exists():
    raise SystemExit("QuantityReportTotals Count-integrity runbook is missing")

print("PASS quantity report totals known-Count integrity source guard")
