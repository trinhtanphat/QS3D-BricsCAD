#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Reporting/QuantityReportBuilder.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QuantityReportBuilderKnownCountStabilitySmoke.cs"


def fail(message: str) -> None:
    print(f"ERROR: quantity report builder known-Count stability preflight failed: {message}")
    sys.exit(1)


def require(text: str, token: str, label: str) -> int:
    index = text.find(token)
    if index < 0:
        fail(f"missing {label}: {token}")
    return index


source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

method_start = require(
    source,
    "public static IReadOnlyList<QuantityReportRow> Group(IEnumerable<ElementInstance> elements)",
    "QuantityReportBuilder.Group",
)
method_end = require(source[method_start:], "private static int? SnapshotKnownElementCount", "known-count helper") + method_start
region = source[method_start:method_end]

loop = require(region, "while (true)", "explicit enumerator traversal")
pre_rebound = require(region[loop:], "RequireStableKnownElementCount(elements, knownCount);", "pre-MoveNext Count rebound") + loop
move_next = require(region[pre_rebound:], "var moved = enumerator.MoveNext();", "MoveNext") + pre_rebound
post_rebound = require(region[move_next:], "RequireStableKnownElementCount(elements, knownCount);", "post-MoveNext Count rebound") + move_next
overrun = require(region[post_rebound:], "if (knownCount.HasValue && observedCount >= knownCount.Value)", "known-count overrun admission") + post_rebound
current = require(region[overrun:], "var element = enumerator.Current;", "Current read") + overrun

if not (loop < pre_rebound < move_next < post_rebound < overrun < current):
    fail("Group traversal must order while(true) -> Count rebound -> MoveNext -> Count rebound -> overrun admission -> Current")

if region.count("var element = enumerator.Current;") != 1:
    fail("Group traversal must read semantic Current exactly once")

for token, label in [
    ("ICollection<ElementInstance>", "generic ICollection Count surface"),
    ("IReadOnlyCollection<ElementInstance>", "IReadOnlyCollection Count surface"),
    ("ICollection nonGenericCollection", "non-generic ICollection Count surface"),
    ("count < 0", "negative Count rejection"),
    ("knownCount.Value != count", "conflicting Count rejection"),
    ("observedCount != knownCount.Value", "under-yield rejection"),
]:
    require(source, token, label)

for test_name in [
    "KnownCountOverrunRejectsBeforeUnexpectedCurrentRead",
    "TransientGrowthRejectsBeforeCurrentRead",
    "TransientShrinkRejectsBeforeCurrentRead",
    "TransientNegativeCountRejectsBeforeCurrentRead",
    "TransientCrossInterfaceConflictRejectsBeforeCurrentRead",
    "StableCountedSourcePreservesGrouping",
    "PureStreamingSourceRemainsSupported",
]:
    require(smoke, test_name, f"smoke case {test_name}")

if smoke.count("Equal(0, source.CurrentReads);") < 4:
    fail("hostile transient Count cases must prove rejection before Current")
require(smoke, "Equal(1, source.CurrentReads);", "N+1 no-overread assertion")

print("PASS quantity report builder known-Count stability source guard")
