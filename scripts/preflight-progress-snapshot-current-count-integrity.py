#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Progress/ProgressSnapshot.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProgressSnapshotCurrentCountIntegritySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/progress-snapshot-current-count-integrity.md"


def require(text: str, needle: str, label: str) -> int:
    index = text.find(needle)
    if index < 0:
        raise SystemExit(f"Progress snapshot Current Count preflight failed: missing {label}: {needle}")
    return index


source = SOURCE.read_text(encoding="utf-8")
method_start = require(source, "internal static List<T> Snapshot<T>(", "snapshot materializer")
method_end = require(source[method_start:], "private static void RequireKnownCountStable<T>(", "known-count helper") + method_start
method = source[method_start:method_end]

move = require(method, "if (!enumerator.MoveNext())", "MoveNext traversal")
post_move = require(method[move:], "RequireKnownCountStable(source, knownCount, parameterName, label);", "post-MoveNext Count rebound") + move
current = require(method, "var item = enumerator.Current;", "Current read")
post_current = require(method[current:], "RequireKnownCountStable(source, knownCount, parameterName, label);", "post-Current Count rebound") + current
null_validation = require(method, "if (item == null)", "null validation")
retention = require(method, "result.Add(item);", "item retention")

if not (move < post_move < current < post_current < null_validation < retention):
    raise SystemExit(
        "Progress snapshot Current Count preflight failed: required ordering is MoveNext -> Count rebound -> Current -> Count rebound -> null validation -> retention"
    )

smoke = SMOKE.read_text(encoding="utf-8")
for needle, label in (
    ("CurrentCountDriftPreemptsNullValidation();", "hostile Current-induced Count regression"),
    ("StableCountedAndStreamingSourcesRemainSupported();", "stable counted/streaming controls"),
    ("return null!;", "malformed Current payload"),
    ("known count changed during traversal", "Count-integrity diagnostic assertion"),
    ("[ModuleInitializer]", "automatic smoke registration"),
):
    require(smoke, needle, label)

runbook = RUNBOOK.read_text(encoding="utf-8")
for needle in ("ProgressDomainContract.Snapshot", "MoveNext", "Current", "Count", "null validation", "retention"):
    require(runbook, needle, f"runbook token {needle}")

print("PASS progress snapshot Current-induced Count integrity preflight")
