#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ADVANCED = ROOT / "src/QS3D.Core/Cost/AdvancedCostManagement.cs"
DEEP = ROOT / "src/QS3D.Core/Cost/DeepCostWorkflows.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/AdvancedCostKnownCountTraversalSmoke.cs"
errors = []

for path in (ADVANCED, DEEP, SMOKE):
    if not path.is_file():
        errors.append("missing advanced-cost known-count file: " + str(path.relative_to(ROOT)))

sources = ""
replay_guard_count = 0
if ADVANCED.is_file():
    advanced = ADVANCED.read_text(encoding="utf-8")
    sources += advanced
    for token in (
        "internal static void RequireCanProcessNext(",
        "if (hasKnownCount && observedCount >= knownCount)",
        'collectionLabel + " traversal produced more entries than its known count reported " + knownCount + "."',
        "if (observedCount == MaximumEntries)",
        "ThrowTooManyEntries(collectionLabel);",
        "RequireKnownCountMatchesTraversal(",
    ):
        if token not in advanced:
            errors.append("AdvancedCostCollectionContract missing early/final Count-integrity contract: " + token)

    replay_start_marker = "private static void RequireStableComponentGeneration("
    replay_end_marker = "private static bool SameComponentState("
    replay_start = advanced.find(replay_start_marker)
    replay_end = advanced.find(replay_end_marker, replay_start + len(replay_start_marker)) if replay_start >= 0 else -1
    if replay_start < 0 or replay_end < 0:
        errors.append("Rate build-up generation replay method boundary is missing from Advanced Cost source")
    else:
        replay_block = advanced[replay_start:replay_end]
        replay_guard_count = replay_block.count("AdvancedCostCollectionContract.RequireCanProcessNext(")
        if replay_guard_count != 1:
            errors.append(
                "expected exactly one known-count pre-item guard inside rate build-up generation replay, found "
                + str(replay_guard_count))

if DEEP.is_file():
    sources += DEEP.read_text(encoding="utf-8")

expected_labels = (
    "Rate build-up component collection",
    "Historical cost catalog",
    "Build-up analysis rate collection",
    "Trade analysis item collection",
    "BQ library entry collection",
    "BQ project import collection",
    "Tender quote line collection",
    "Tender requirement collection",
    "Tender bid collection",
    "Progress contract item collection",
    "Progress claim line collection",
)

if sources:
    guard_count = sources.count("AdvancedCostCollectionContract.RequireCanProcessNext(")
    original_consumer_guard_count = guard_count - replay_guard_count
    if original_consumer_guard_count != len(expected_labels):
        errors.append(
            "expected exactly " + str(len(expected_labels))+
            " original Advanced Cost pre-item known-count guards outside generation replay, found "
            + str(original_consumer_guard_count))
    for label in expected_labels:
        marker = '"' + label + '"'
        if sources.count(marker) < 3:
            errors.append("collection label is not bound to known-count preflight/traversal checks: " + label)

if SMOKE.is_file():
    smoke = SMOKE.read_text(encoding="utf-8")
    for token in (
        "KnownCountOverrunPrecedesUnexpectedItemSemanticValidation();",
        "private static void KnownCountOverrunPrecedesUnexpectedItemSemanticValidation()",
        "null!",
        "ExactKnownCountAndPureStreamingRemainAccepted();",
        "SemanticValidationStillPrecedesPostTraversalMismatch();",
        'Contains("known count reported", error.Message, message);',
    ):
        if token not in smoke:
            errors.append("advanced-cost smoke missing early Count-drift assertion/control: " + token)
    for phrase in (
        "unexpected component",
        "unexpected record",
        "unexpected rate",
        "unexpected item",
        "unexpected entry",
        "unexpected quote line",
        "unexpected requirement",
        "unexpected bid",
        "unexpected contract item",
        "unexpected claim line",
    ):
        if phrase not in smoke:
            errors.append("advanced-cost smoke missing pre-semantic overrun assertion: " + phrase)

print("QS3D Advanced Cost known-count early-drift preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: all original Advanced Cost known-count consumers plus rate build-up generation replay reject the first unexpected item before semantic processing, retain post-traversal under-yield validation, and keep unknown-stream bounds.")