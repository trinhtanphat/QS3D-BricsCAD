#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Cost/DeepCostWorkflows.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RateReferenceGraphKnownCountStabilitySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/rate-reference-known-count-stability.md"
errors = []

for path, label in (
    (SOURCE, "RateReferenceGraph source"),
    (SMOKE, "RateReferenceGraph Count-stability smoke"),
    (RUNBOOK, "RateReferenceGraph Count-stability runbook"),
):
    if not path.is_file():
        errors.append("missing " + label + ": " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8") if SOURCE.is_file() else ""
smoke = SMOKE.read_text(encoding="utf-8") if SMOKE.is_file() else ""
runbook = RUNBOOK.read_text(encoding="utf-8") if RUNBOOK.is_file() else ""

for token in (
    "if (knownCount.HasValue && index == knownCount.Value)",
    "contains more entries than its known count.",
    "RequireKnownCountStableAfterTraversal(edges, knownCount.Value);",
    "var reboundKnownCount = ValidateKnownCount(edges);",
    "known count changed during traversal.",
    "if (index == MaximumEdges)",
    "known count does not match the observed traversal.",
    "snapshot.Sort(CompareEdges);",
):
    if token not in source:
        errors.append("RateReferenceGraph source missing contract token: " + token)

# The stability check must remain after under-yield detection but before canonical sorting/publication.
mismatch = source.find("known count does not match the observed traversal.")
stable = source.find("RequireKnownCountStableAfterTraversal(edges, knownCount.Value);", mismatch)
sort_index = source.find("snapshot.Sort(CompareEdges);", stable)
if not (mismatch >= 0 and stable > mismatch and sort_index > stable):
    errors.append("post-traversal Count stability must occur after under-yield detection and before canonical sorting")

# The known-count overrun guard must precede the independent streaming ceiling and semantic edge validation.
overrun = source.find("if (knownCount.HasValue && index == knownCount.Value)")
streaming = source.find("if (index == MaximumEdges)", overrun)
null_validation = source.find("if (edge == null)", overrun)
duplicate_validation = source.find("if (!keys.Add(key))", overrun)
if not (overrun >= 0 and streaming > overrun and null_validation > streaming and duplicate_validation > null_validation):
    errors.append("known-count overrun must fail before semantic edge validation while preserving the streaming ceiling")

for token in (
    "OverrunFailsBeforeUnexpectedEdgeValidation();",
    "PostTraversalCountDriftFailsClosed();",
    "PostTraversalNegativeCountFailsClosed();",
    "PostTraversalConflictingCountsFailClosed();",
    "UnderYieldStillFailsClosed();",
    "HonestMultiInterfaceCountRemainsAccepted();",
    "PureStreamingInputRemainsAccepted();",
    "PostTraversalCountCollection",
    "PostTraversalMultiCountCollection",
    "[ModuleInitializer]",
):
    if token not in smoke:
        errors.append("RateReferenceGraph Count-stability smoke missing regression/control: " + token)

for token in (
    "SOURCE_READY",
    "50,000",
    "known Count",
    "early overrun",
    "post-traversal",
    "pure streaming",
    "licensed BricsCAD",
):
    if token not in runbook:
        errors.append("RateReferenceGraph Count-stability runbook missing contract text: " + token)

print("QS3D RateReferenceGraph known-count stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: RateReferenceGraph rejects known-count overrun before semantic processing and rebinds deterministic Count evidence after traversal before canonical publication.")
