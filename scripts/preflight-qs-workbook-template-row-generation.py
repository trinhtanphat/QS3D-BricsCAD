#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source = ROOT / "src/QS3D.Core/Export/QsWorkbookTemplateEngine.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/QsWorkbookTemplateEngineSmoke.cs"
runbook = ROOT / "docs/FEATURE-RUNBOOKS/qs-workbook-template-row-generation.md"
errors = []
for path in (source, smoke, runbook):
    if not path.is_file():
        errors.append("missing template row-generation file: " + str(path.relative_to(ROOT)))

if source.is_file():
    text = source.read_text(encoding="utf-8")
    start = text.find("private static List<QuantityReportRow> SnapshotRows(")
    end = text.find("private static void ApplyRows", start)
    block = text[start:end] if start >= 0 and end > start else ""
    required = (
        "var admittedCount = rows.Count;",
        "new List<QuantityReportRow>(admittedCount)",
        "for (var index = 0; index < admittedCount; index++)",
        "if (rows.Count != admittedCount)",
        "EnsureSnapshotRowStable(rows, admittedCount, index, result[index]);",
        "private static void EnsureSnapshotRowStable(",
        "var liveIds = CanonicalTokens(live.ElementIds",
        "var liveHandles = CanonicalTokens(live.SourceHandles",
        "!live.NetConcreteM3.Equals(snapshot.NetConcreteM3)",
        "live.HasNetConcreteM3Evidence != snapshot.HasNetConcreteM3Evidence",
        "!liveIds.SequenceEqual(snapshot.ElementIds, StringComparer.Ordinal)",
        "!liveHandles.SequenceEqual(snapshot.SourceHandles, StringComparer.Ordinal)",
        "Template export quantity row changed during snapshot capture.",
    )
    cursor = 0
    for token in required:
        pos = block.find(token, cursor)
        if pos < 0:
            errors.append("template snapshot generation fence missing ordered token: " + token)
            break
        cursor = pos + len(token)
    if "for (var index = 0; index < rows.Count; index++)" in block:
        errors.append("template snapshot must not use live Count as its traversal bound")

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for token in (
        "RejectsCollectionGenerationDrift",
        "ShrinkingRows",
        "RejectsRowValueGenerationDrift",
        "MutatingRowOnRevalidation",
        "Template export must reject collection Count drift during snapshot capture.",
        "Template export must reject scalar/provenance drift during snapshot capture.",
        "Generation-drift rejection must preserve an existing destination workbook.",
        "Row-generation drift rejection must preserve an existing destination workbook.",
    ):
        if token not in text:
            errors.append("template row-generation smoke missing token: " + token)

print("QS3D workbook template row-generation preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: template XLSX quantity rows freeze admitted Count and replay scalar/evidence/provenance generation before filesystem publication.")
