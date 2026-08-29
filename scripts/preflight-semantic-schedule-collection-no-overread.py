#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Documentation/SemanticScheduleCatalog.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/SemanticScheduleCollectionNoOverreadSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/semantic-schedule-collection-no-overread.md"


def fail(message: str) -> None:
    print(f"FAIL semantic schedule collection no-overread: {message}", file=sys.stderr)
    raise SystemExit(1)


for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.exists():
        fail(f"missing required artifact: {path.relative_to(ROOT)}")

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

snapshot_marker = "private static IReadOnlyList<T> SnapshotBounded<T>"
save_marker = "public static void Save(ProjectState project, IEnumerable<SemanticScheduleDefinition> definitions)"
if snapshot_marker not in source:
    fail("SemanticScheduleDefinition SnapshotBounded<T> helper is missing")
if save_marker not in source:
    fail("SemanticScheduleCatalog.Save boundary is missing")

snapshot = source[source.index(snapshot_marker):source.index("private static void ValidateKnownCount<T>(ICollection<T>?", source.index(snapshot_marker))]
save_start = source.index(save_marker)
save_end = source.index("public static void Upsert", save_start)
save = source[save_start:save_end]

required_snapshot_tokens = (
    "while (enumerator.MoveNext())",
    "if (knownCount.HasValue && result.Count >= knownCount.Value)",
    "result.Add(enumerator.Current)",
    "ValidateKnownCountEvidence(values, maxCount, capacityError, knownCount, \"after traversal\")",
)
for token in required_snapshot_tokens:
    if token not in snapshot:
        fail(f"SnapshotBounded<T> is missing admission token: {token}")

if snapshot.index("if (knownCount.HasValue && result.Count >= knownCount.Value)") > snapshot.index("result.Add(enumerator.Current)"):
    fail("SnapshotBounded<T> reads Current before known-Count admission")

required_save_tokens = (
    "using (var enumerator = definitions.GetEnumerator())",
    "while (enumerator.MoveNext())",
    "if (list.Count >= MaxSchedules)",
    "list.Add(enumerator.Current)",
)
for token in required_save_tokens:
    if token not in save:
        fail(f"Save is missing explicit admission token: {token}")
if save.index("if (list.Count >= MaxSchedules)") > save.index("list.Add(enumerator.Current)"):
    fail("Save reads Current before the 128-definition admission check")
if "foreach (var definition in definitions)" in save:
    fail("Save regressed to foreach and therefore exposes Current before the body guard")

for token in (
    "KnownCountOverrunStopsBeforeUnexpectedCurrent",
    "PostTraversalCountDriftFailsClosed",
    "CatalogCapacityStopsBeforeUnexpectedCurrent",
    "StableCountedSnapshotStillMaterializesExactly",
    "CurrentReads",
    "MoveNextCalls",
):
    if token not in smoke:
        fail(f"regression smoke is missing token: {token}")

for token in ("Issue: #4486", "Lane-Key: `issue-4486`", "MoveNext", "Current", "128"):
    if token not in runbook:
        fail(f"runbook is missing token: {token}")

print("PASS semantic schedule collection known-Count/cap Current no-overread source guard")
