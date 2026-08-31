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

snapshot_start = source.index(snapshot_marker)
snapshot_end = source.index("private static void ValidateKnownCountEvidence<T>", snapshot_start)
snapshot = source[snapshot_start:snapshot_end]
save_start = source.index(save_marker)
save_end = source.index("public static void Upsert", save_start)
save = source[save_start:save_end]

required_snapshot_tokens = (
    'ValidateKnownCountEvidence(values, maxCount, capacityError, knownCount, "before MoveNext")',
    "var moved = enumerator.MoveNext();",
    'ValidateKnownCountEvidence(values, maxCount, capacityError, knownCount, "after MoveNext")',
    "if (!moved) break;",
    "if (knownCount.HasValue && result.Count >= knownCount.Value)",
    "var current = enumerator.Current;",
    'ValidateKnownCountEvidence(values, maxCount, capacityError, knownCount, "after Current")',
    "result.Add(current)",
    'ValidateKnownCountEvidence(values, maxCount, capacityError, knownCount, "after traversal")',
)
for token in required_snapshot_tokens:
    if token not in snapshot:
        fail(f"SnapshotBounded<T> is missing admission token: {token}")

pre_move = snapshot.index('ValidateKnownCountEvidence(values, maxCount, capacityError, knownCount, "before MoveNext")')
move = snapshot.index("var moved = enumerator.MoveNext();", pre_move)
post_move = snapshot.index('ValidateKnownCountEvidence(values, maxCount, capacityError, knownCount, "after MoveNext")', move)
break_guard = snapshot.index("if (!moved) break;", post_move)
known_guard = snapshot.index("if (knownCount.HasValue && result.Count >= knownCount.Value)", break_guard)
current = snapshot.index("var current = enumerator.Current;", known_guard)
post_current = snapshot.index('ValidateKnownCountEvidence(values, maxCount, capacityError, knownCount, "after Current")', current)
add = snapshot.index("result.Add(current)", post_current)
if not (pre_move < move < post_move < break_guard < known_guard < current < post_current < add):
    fail("SnapshotBounded<T> Count/no-overread ordering is not fail-closed")
if "result.Add(enumerator.Current)" in snapshot:
    fail("SnapshotBounded<T> must rebind Count after Current before retaining the value")

required_save_tokens = (
    "var knownCount = ResolveSaveKnownCount(definitions);",
    "using (var enumerator = definitions.GetEnumerator())",
    'RequireStableSaveKnownCount(definitions, knownCount, "before MoveNext")',
    "var moved = enumerator.MoveNext();",
    'RequireStableSaveKnownCount(definitions, knownCount, "after MoveNext")',
    "if (!moved) break;",
    "if (knownCount.HasValue && list.Count >= knownCount.Value)",
    "if (list.Count >= MaxSchedules)",
    "var current = enumerator.Current;",
    'RequireStableSaveKnownCount(definitions, knownCount, "after Current")',
    "list.Add(current)",
)
for token in required_save_tokens:
    if token not in save:
        fail(f"Save is missing explicit admission token: {token}")

save_pre_move = save.index('RequireStableSaveKnownCount(definitions, knownCount, "before MoveNext")')
save_move = save.index("var moved = enumerator.MoveNext();", save_pre_move)
save_post_move = save.index('RequireStableSaveKnownCount(definitions, knownCount, "after MoveNext")', save_move)
save_break = save.index("if (!moved) break;", save_post_move)
save_known_guard = save.index("if (knownCount.HasValue && list.Count >= knownCount.Value)", save_break)
save_cap = save.index("if (list.Count >= MaxSchedules)", save_known_guard)
save_current = save.index("var current = enumerator.Current;", save_cap)
save_post_current = save.index('RequireStableSaveKnownCount(definitions, knownCount, "after Current")', save_current)
save_add = save.index("list.Add(current)", save_post_current)
if not (save_pre_move < save_move < save_post_move < save_break < save_known_guard < save_cap < save_current < save_post_current < save_add):
    fail("Save Count/capacity no-overread ordering is not fail-closed")
if "list.Add(enumerator.Current)" in save:
    fail("Save must rebind Count after Current before retaining a definition")
if "foreach (var definition in definitions)" in save:
    fail("Save regressed to foreach and therefore exposes Current before the body guard")

for token in (
    "KnownCountOverrunStopsBeforeUnexpectedCurrent",
    "TerminalMoveNextCountDriftFailsClosed",
    "CatalogCapacityStopsBeforeUnexpectedCurrent",
    "StableCountedSnapshotStillMaterializesExactly",
    "CurrentReads",
    "MoveNextCalls",
    "known Count changed or conflicted after MoveNext",
):
    if token not in smoke:
        fail(f"regression smoke is missing token: {token}")

for token in ("Issue: #4486", "Lane-Key: `issue-4486`", "MoveNext", "Current", "128", "post-Current Count"):
    if token not in runbook:
        fail(f"runbook is missing token: {token}")

print("PASS semantic schedule collection known-Count/cap Current no-overread source guard")
