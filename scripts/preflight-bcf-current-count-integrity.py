#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/BcfIssueExchange.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/BcfIssueExchangeCurrentCountIntegritySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/bcf-current-count-integrity.md"


def require(text: str, needle: str, label: str) -> int:
    index = text.find(needle)
    if index < 0:
        raise SystemExit(f"BCF Current Count preflight failed: missing {label}: {needle}")
    return index


source = SOURCE.read_text(encoding="utf-8")
method_start = require(source, "internal static List<T> MaterializeBounded<T>(", "bounded materializer")
method_end = require(source[method_start:], "private static void RequireStableKnownCounts<T>(", "stable-count helper") + method_start
method = source[method_start:method_end]

move = require(method, "if (!enumerator.MoveNext())", "MoveNext traversal")
post_move = require(method[move:], "RequireStableKnownCounts(", "post-MoveNext Count rebound") + move
current = require(method, "var value = enumerator.Current;", "Current read")
post_current = require(method[current:], "RequireStableKnownCounts(", "post-Current Count rebound") + current
stage = require(method, "items.Add(value);", "item staging")
advance = require(method, "observedCount++;", "observed-count advance")

if not (move < post_move < current < post_current < stage < advance):
    raise SystemExit(
        "BCF Current Count preflight failed: required ordering is MoveNext -> Count rebound -> Current -> Count rebound -> staging -> observed-count advance"
    )

smoke = SMOKE.read_text(encoding="utf-8")
for needle, label in (
    ("StableCountIsReboundImmediatelyAfterCurrent();", "stable positive control"),
    ("CurrentInducedCountDriftIsRejectedImmediately();", "hostile Current-induced Count regression"),
    ("Equal(7, source.CountReads", "stable Count observation budget"),
    ("Equal(4, source.CountReads", "rejecting post-Current Count observation"),
    ("[ModuleInitializer]", "automatic smoke registration"),
):
    require(smoke, needle, label)

runbook = RUNBOOK.read_text(encoding="utf-8")
for needle in ("topics", "viewpoints", "comments", "components", "Current", "Count"):
    require(runbook, needle, f"runbook token {needle}")

print("PASS BCF Current-induced Count integrity preflight")
