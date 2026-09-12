#!/usr/bin/env python3
from pathlib import Path

SOURCE = Path("src/QS3D.Core/BenchmarkParity/QsQaGate2.cs")
text = SOURCE.read_text(encoding="utf-8")

required = {
    "strict profile severity": '{ "QA2.MISSING_CLASSIFICATION", QsQaSeverity.Error }',
    "classification predicate": 'element.Classification.Length == 0',
    "classification rule id": '"QA2.MISSING_CLASSIFICATION"',
    "blocking fallback severity": 'QsQaSeverity.Error, element.Id, "Classification is required before quantity workflows can run."',
}

missing = [name for name, token in required.items() if token not in text]
if missing:
    raise SystemExit("QA2 classification completeness contract missing: " + ", ".join(missing))

if text.count('"QA2.MISSING_CLASSIFICATION"') < 2:
    raise SystemExit("QA2.MISSING_CLASSIFICATION must be wired in both strict profile and analyzer")

print("PASS: QA2 strict classification completeness is fail-closed")
