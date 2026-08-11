#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Rules/QuantityRuleEngine.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QuantityRuleProvenanceCanonicalReadSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing quantity-rule provenance canonical-read contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    source = SOURCE.read_text(encoding="utf-8")
    start = source.find("private static List<string> GetStaleManagedOutputs(ProjectElement element, ISet<string> activeOutputs)")
    end = source.find("private static void CleanupStaleOutputs", start)
    if start < 0 or end <= start:
        errors.append("cannot isolate QuantityRuleEngine.GetStaleManagedOutputs")
    else:
        body = source[start:end]
        for token in (
            "var output = key.Substring(ProvenancePrefix.Length);",
            "string.IsNullOrWhiteSpace(output)",
            "!string.Equals(output, output.Trim(), StringComparison.Ordinal)",
            'throw new InvalidOperationException("Element " + element.Id + " contains malformed quantity-rule provenance key: " + key + ".")',
            "if (activeOutputs.Contains(output)) continue;",
        ):
            if token not in body:
                errors.append("quantity-rule stale provenance reader missing canonical-read guard: " + token)
        if "Substring(ProvenancePrefix.Length).Trim()" in body:
            errors.append("quantity-rule engine must not silently trim persisted provenance output names")
        throw_pos = body.find("throw new InvalidOperationException")
        active_pos = body.find("if (activeOutputs.Contains(output))")
        add_pos = body.find("result.Add(output)")
        if min(throw_pos, active_pos, add_pos) < 0 or not (throw_pos < active_pos < add_pos):
            errors.append("malformed provenance must fail before active/stale classification")

if SMOKE.is_file():
    smoke = SMOKE.read_text(encoding="utf-8")
    for token in (
        "PaddedStaleProvenanceFailsBeforeMutation();",
        "BlankProvenanceFailsBeforeMutation();",
        "PaddedActiveProvenanceFailsBeforeRuleApply();",
        "CanonicalStaleProvenanceStillCleansExactly();",
        'element.Properties["Rule: Ghost"]',
        'element.Properties["Rule:   "]',
        'True(!element.Properties.ContainsKey("Rule:Ghost"));',
        "[ModuleInitializer]",
    ):
        if token not in smoke:
            errors.append("quantity-rule provenance smoke missing regression contract: " + token)

print("QS3D quantity-rule provenance canonical-read preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: persisted Rule:<Output> provenance is canonical-read only; malformed blank/padded keys fail before cleanup or rule mutation while canonical stale provenance remains cleanable.")
