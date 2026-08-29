from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/XlsxQuantityEvidenceExporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QuantityEvidenceXlsxHardeningSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "var snapshot = SnapshotExplanations(explanations);",
    "ValidateProjectedRowCapacity(snapshot);",
    "QuantityEvidenceExportProjection.CreateMany(snapshot);",
    "var count = explanations.Count;",
    "if (explanations.Count != count)",
    "Quantity evidence XLSX explanation count changed during snapshot.",
]
forbidden_source = [
    "ValidateProjectedRowCapacity(explanations);",
    "QuantityEvidenceExportProjection.CreateMany(explanations);",
    "for (var index = 0; index < explanations.Count; index++)",
]
required_smoke = [
    "ExplanationSnapshotReadsEachCallerEntryOnce();",
    "ExplanationCountDriftFailsClosedBeforePublication();",
    "SingleReadExplanationList",
    "CountDriftingExplanationList",
    "count changed during snapshot",
    "existing-quantity-evidence-workbook",
]

missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]
forbidden = [token for token in forbidden_source if token in source]

if missing:
    raise SystemExit("Quantity evidence XLSX snapshot-integrity preflight failed; missing: " + ", ".join(missing))
if forbidden:
    raise SystemExit("Quantity evidence XLSX snapshot-integrity preflight failed; live caller list traversal remains: " + ", ".join(forbidden))

snapshot_index = source.index("var snapshot = SnapshotExplanations(explanations);")
capacity_index = source.index("ValidateProjectedRowCapacity(snapshot);")
projection_index = source.index("QuantityEvidenceExportProjection.CreateMany(snapshot);")
if not snapshot_index < capacity_index < projection_index:
    raise SystemExit(
        "Quantity evidence XLSX snapshot-integrity preflight failed; "
        "export order must be detached snapshot -> capacity validation -> projection"
    )

print("PASS quantity evidence XLSX detached snapshot integrity source guard")
