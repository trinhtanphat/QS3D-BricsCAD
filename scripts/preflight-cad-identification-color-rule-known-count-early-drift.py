#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Recognition/CadIdentificationOptions.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CadIdentificationColorRuleCountTraversalSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing CAD identification Count-integrity file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    source = SOURCE.read_text(encoding="utf-8")
    for token in (
        "if (knownCount.HasValue && index >= knownCount.Value)",
        '"Identification color-rule source traversal produced more entries than its known Count reported " + knownCount.Value + "."',
        "if (index == MaximumColorRules)",
        "if (knownCount.HasValue && index != knownCount.Value)",
        "TryGetKnownColorRuleCount(",
    ):
        if token not in source:
            errors.append("CAD identification source missing known-Count contract: " + token)

    early = source.find("if (knownCount.HasValue && index >= knownCount.Value)")
    cap = source.find("if (index == MaximumColorRules)")
    null_check = source.find("if (rule == null)")
    duplicate = source.find("if (byColor.ContainsKey(rule.ColorIndex))")
    if min(early, cap, null_check, duplicate) < 0 or not (early < cap < null_check < duplicate):
        errors.append("known-Count overrun guard must precede cap and rule semantic validation")

if SMOKE.is_file():
    smoke = SMOKE.read_text(encoding="utf-8")
    for token in (
        "OverEnumerationRejectsEarly();",
        "KnownCountOverrunPrecedesUnexpectedRuleValidation();",
        "Rule(1), null!",
        'Contains("traversal produced more entries than its known Count reported 1", error.Message,',
        "UnderEnumerationRejects();",
        "ExactKnownCountRemainsAccepted();",
        "PureStreamingRemainsAccepted();",
        "NegativeKnownCountRejectsBeforeTraversal();",
        "ConflictingKnownCountsRejectBeforeTraversal();",
        "OversizedKnownCountRejectsBeforeTraversal();",
        "ExistingNullAndDuplicateValidationRemain();",
    ):
        if token not in smoke:
            errors.append("CAD identification smoke missing Count-integrity assertion/control: " + token)

print("QS3D CAD identification color-rule known-count early-drift preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: color-rule traversal rejects the first known-count overrun before rule validation while preserving bounds, under-yield checks, and streaming behavior.")
