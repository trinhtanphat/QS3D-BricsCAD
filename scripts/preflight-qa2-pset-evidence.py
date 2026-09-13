#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/BenchmarkParity/QsQaGate2.cs"
DOC = ROOT / "docs/benchmark-parity/solibri-qa2-pset-evidence-validation.md"

for path in (SOURCE, DOC):
    if not path.is_file():
        raise SystemExit("QA2 Pset evidence file is missing: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
required = (
    "InvalidPsetEvidence",
    "HasUsablePsetEvidence(value)",
    '"0", "false", "missing", "none", "n/a", "na", "null", "absent", "no"',
    '"QA2.MISSING_PSET"',
    "missing or has unusable evidence",
)
for token in required:
    if token not in source:
        raise SystemExit("QA2 Pset evidence validation is missing: " + token)

negative = {"0", "false", "missing", "none", "n/a", "na", "null", "absent", "no"}
for value in ("", "   ", "FALSE", " missing ", "N/A", "Null", "0"):
    usable = bool(value.strip()) and value.strip().lower() not in negative
    if usable:
        raise SystemExit("negative Pset evidence fixture unexpectedly passed: " + repr(value))
for value in ("present", "true", "1", "IfcPropertySet#42", '{"Count":3}'):
    usable = bool(value.strip()) and value.strip().lower() not in negative
    if not usable:
        raise SystemExit("positive Pset evidence fixture unexpectedly failed: " + repr(value))

text = DOC.read_text(encoding="utf-8")
for phrase in ("QA2.MISSING_PSET", "Takeoff", "BOQ", "Estimate", "compatibility", "waiver"):
    if phrase.lower() not in text.lower():
        raise SystemExit("QA2 Pset evidence runbook missing contract: " + phrase)

print("PASS QA2 IFC Pset evidence validation guard")
