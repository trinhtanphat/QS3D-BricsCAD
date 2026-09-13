#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/BenchmarkParity/QsQaGate2.cs"
DOC = ROOT / "docs/benchmark-parity/solibri-qa2-relationship-evidence-validation.md"

for path in (SOURCE, DOC):
    if not path.is_file():
        raise SystemExit("QA2 relationship evidence file is missing: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
required = (
    "HasUsableRelationshipEvidence(value)",
    "HasUsableRelationshipEvidence(spatialContainer)",
    "HasUsableRelationshipEvidence(typeAssignment)",
    '"QA2.MISSING_RELATIONSHIP"',
    "missing or has unusable evidence",
)
for token in required:
    if token not in source:
        raise SystemExit("QA2 relationship evidence validation is missing: " + token)

negative = {"0", "false", "missing", "none", "n/a", "na", "null", "absent", "no"}
for value in ("", "   ", "FALSE", " missing ", "N/A", "Null", "0", " No "):
    usable = bool(value.strip()) and value.strip().lower() not in negative
    if usable:
        raise SystemExit("negative relationship evidence fixture unexpectedly passed: " + repr(value))
for value in ("Level 01", "IfcBuildingStorey#42", "WallType-A", "true", "1"):
    usable = bool(value.strip()) and value.strip().lower() not in negative
    if not usable:
        raise SystemExit("positive relationship evidence fixture unexpectedly failed: " + repr(value))

text = DOC.read_text(encoding="utf-8")
for phrase in ("QA2.MISSING_RELATIONSHIP", "SpatialContainer", "TypeAssignment", "Takeoff", "BOQ", "Estimate", "compatibility", "waiver"):
    if phrase.lower() not in text.lower():
        raise SystemExit("QA2 relationship evidence runbook missing contract: " + phrase)

print("PASS QA2 IFC relationship evidence validation guard")
