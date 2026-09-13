#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/BenchmarkParity/QsQaGate2.cs"
DOC = ROOT / "docs/benchmark-parity/solibri-qa2-ifc-metadata-evidence.md"

for path in (SOURCE, DOC):
    if not path.is_file():
        raise SystemExit("QA2 IFC metadata evidence file is missing: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
required = (
    'TryGetValue("IfcEntity", out ifcEntity) || !HasUsableMetadataEvidence(ifcEntity)',
    'TryGetValue("QuantityUnit", out quantityUnit) || !HasUsableMetadataEvidence(quantityUnit)',
    'private static bool HasUsableMetadataEvidence(string value)',
    '"QA2.MISSING_IFC_ENTITY"',
    '"QA2.MISSING_QUANTITY_UNIT"',
)
for token in required:
    if token not in source:
        raise SystemExit("QA2 IFC metadata evidence validation is missing: " + token)

negative = {"0", "false", "missing", "none", "n/a", "na", "null", "absent", "no"}
for value in ("", "   ", "FALSE", " missing ", "N/A", "Null", "0", " No "):
    usable = bool(value.strip()) and value.strip().lower() not in negative
    if usable:
        raise SystemExit("negative IFC metadata fixture unexpectedly passed: " + repr(value))
for value in ("IfcWall", "IfcBeam", "m", "m2", "m3", "kg", "true", "1"):
    usable = bool(value.strip()) and value.strip().lower() not in negative
    if not usable:
        raise SystemExit("positive IFC metadata fixture unexpectedly failed: " + repr(value))

text = DOC.read_text(encoding="utf-8")
for phrase in ("QA2.MISSING_IFC_ENTITY", "QA2.MISSING_QUANTITY_UNIT", "Takeoff", "BOQ", "Estimate", "compatibility", "waiver"):
    if phrase.lower() not in text.lower():
        raise SystemExit("QA2 IFC metadata evidence runbook missing contract: " + phrase)

print("PASS QA2 IFC entity/quantity-unit evidence validation guard")
