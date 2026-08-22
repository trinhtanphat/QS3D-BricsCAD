# Work claim — Semantic Schedule category snapshot bound

- Status: `COMPLETED`
- Agent: `Codex /root/issue81_performance_next_gap`
- Registered: `2026-08-15T09:51:01+07:00`
- Baseline main SHA: `017f96803b373955adda72239ad0b6b86cb9ca1b`
- Claim branch: `codex/issue81-schedule-category-bound-claim-20260815`
- Planned implementation branch: `codex/issue81-schedule-category-bound-impl-20260815`
- Issue: `#81`

## Confirmed defect

`SemanticScheduleDefinition` snapshots caller-controlled include/exclude element IDs and columns through `SnapshotBounded(...)`, but snapshots `categories` with an unbounded `List<ElementCategory>(IEnumerable<ElementCategory>)` construction. A very large or non-terminating category producer can therefore consume unbounded time or memory inside the public definition constructor before `SemanticScheduleCatalog.Normalize(...)` reaches duplicate and undefined-category validation.

The deterministic counterexample yields 5,001 category entries and then throws a sentinel exception if enumeration continues. Current source reaches the sentinel because it enumerates the entire source; the category snapshot must instead reject the first over-cap entry without asking for another item.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticScheduleCatalog.cs`: category snapshot materialization in the `SemanticScheduleDefinition` constructor only.
- `tests/QS3D.Core.SmokeTests/SemanticScheduleDefinitionBoundedSnapshotSmoke.cs`: extend the existing registered smoke for exact-cap acceptance, first-over-cap rejection without sentinel over-read, and category defensive-snapshot preservation.
- `scripts/preflight-semantic-schedule-definition-bounds.py`: extend the existing focused gate to pin the category snapshot bound.
- `scripts/preflight-semantic-schedule-catalog.py`: reconcile its stale requirement for the removed unbounded `new List<ElementCategory>` materialization with the bounded `Categories = SnapshotBounded(...)` contract. No other catalog token changes.
- This claim record.

The catalog-gate scope was added after the first aggregate run on implementation merge `ec69723cb1e20fce8effeff31e394527d791e09c` exposed exactly one directly related stale literal. The production implementation and focused bounded-definition gate were already passing; this amendment is merged before editing the adjacent catalog gate.

## Intended contract

- Reuse the existing Semantic Schedule filter envelope `SemanticScheduleCatalog.MaxIds = 5000`; do not introduce a second numeric capacity.
- Accept exactly 5,000 raw category entries and preserve their order in the defensive snapshot.
- Reject the first 5,001st entry with a stable capacity error and do not advance the source to a following sentinel.
- Preserve downstream distinct-category normalization and undefined-category rejection.
- Preserve the existing bounded include/exclude ID and column behavior.

## Exclusions

`Load`, `Save`, `Upsert`, `Remove`, `Build`, XML parsing/schema/serialization, schedule catalog count/payload limits, include/exclude IDs, columns, `SemanticDocumentationTableBuilder` and its active structural-freshness claim, Semantic Views/Tags/Sheets, BricsCAD/native/UI/runtime, LOCAL-only automation, private data, release/CI/workflows, GitHub Actions, and every other ACTIVE/BLOCKED claim or open-PR surface are excluded. Broad issue `#81` remains open after this bounded correction.

## Validation plan

- Run `scripts/preflight-semantic-schedule-definition-bounds.py` plus the adjacent Semantic Schedule catalog/schema/save-bound gates.
- Build Core and Core SmokeTests in Release with zero warnings/errors.
- Run the complete Core smoke harness and require `ALL PASS`.
- Run aggregate remote-safe preflight and require every discovered gate to pass.
- Run `git diff --check` and review the exact branch diff.
- Do not operate GitHub Actions or BricsCAD/native runtime.

## Completion evidence

- Claim PR `#1519` merged as `21cd2f26ed2e8b9b817cd8bd22bf4d44958d10af` before implementation.
- Source commit `635e46c4c4069a5a4b88ac31c3d721c6d6d7429c` merged through PR `#1533` as `ec69723cb1e20fce8effeff31e394527d791e09c`.
- The directly related stale catalog-gate scope was claimed first through PR `#1536` (`c1c117a57a1ce9c41aa7d17bd4180f2ddc525af7`), then gate commit `ee71c08a846ae72a8eed3af7016d7c1bcbb35005` merged through PR `#1537` as `309dff81ed3d7f18b9b32ac60cbd44167a40fa8f`.
- Exact `309dff81...` validation: definition-bounds and catalog gates PASS; Core/Smoke Release build 0 warnings/0 errors; full Core smoke `ALL PASS`.
- Aggregate discovered 813 gates: all Semantic Schedule and other #76/#81/#84 gates passed; only the unrelated release-sync gate failed on an old release-helper token. Release, workflows, Actions, native runtime, and LOCAL surfaces remained untouched.
- Broad issue `#81` remains open.
