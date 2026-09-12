#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
QA2 = ROOT / "src" / "QS3D.Core" / "BenchmarkParity" / "QsQaGate2.cs"
text = QA2.read_text(encoding="utf-8")

required = (
    'private static bool IsStructuralIdentityRule(string ruleId)',
    '"QA2.DUPLICATE_ELEMENT_ID"',
    '"QA2.MISSING_IFC_GUID"',
    '"QA2.DUPLICATE_IFC_GUID"',
    'var structuralIdentityConflict = IsStructuralIdentityRule(finding.RuleId);',
    'var severity = IsStructuralIdentityRule(ruleId)',
    '? QsQaSeverity.Critical',
    ': profile.SeverityFor(ruleId, fallback);',
)
for token in required:
    if token not in text:
        raise SystemExit(f"QA2 structural identity severity floor missing: {token}")

for forbidden in (
    'var structuralIdentityConflict =\n                    string.Equals(finding.RuleId, "QA2.DUPLICATE_ELEMENT_ID"',
    'result.Add(new QsQaFinding(ruleId, profile.SeverityFor(ruleId, fallback), elementId, message));',
):
    if forbidden in text:
        raise SystemExit(f"QA2 structural identity severity floor regressed: {forbidden}")

print("PASS QA2 structural identity findings remain Critical and non-waivable")
