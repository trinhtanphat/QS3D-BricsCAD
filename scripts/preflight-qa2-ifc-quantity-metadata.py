#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
QA2 = ROOT / "src" / "QS3D.Core" / "BenchmarkParity" / "QsQaGate2.cs"
text = QA2.read_text(encoding="utf-8")

required = (
    '{ "QA2.MISSING_IFC_ENTITY", QsQaSeverity.Error }',
    '{ "QA2.MISSING_QUANTITY_UNIT", QsQaSeverity.Error }',
    '!element.Properties.TryGetValue("IfcEntity", out ifcEntity) || string.IsNullOrWhiteSpace(ifcEntity)',
    '"QA2.MISSING_IFC_ENTITY"',
    '!element.Properties.TryGetValue("QuantityUnit", out quantityUnit) || string.IsNullOrWhiteSpace(quantityUnit)',
    '"QA2.MISSING_QUANTITY_UNIT"',
    '"Quantity unit is required before takeoff, BOQ and estimate workflows can run safely."',
)
for token in required:
    if token not in text:
        raise SystemExit(f"QA2 IFC quantity metadata completeness missing: {token}")

for structural_token in (
    'string.Equals(ruleId, "QA2.MISSING_IFC_ENTITY", StringComparison.OrdinalIgnoreCase)',
    'string.Equals(ruleId, "QA2.MISSING_QUANTITY_UNIT", StringComparison.OrdinalIgnoreCase)',
):
    if structural_token in text:
        raise SystemExit(f"QA2 completeness finding unexpectedly became structural/non-waivable: {structural_token}")

print("PASS QA2 requires IFC entity and quantity-unit metadata while preserving configurable waiver semantics")
