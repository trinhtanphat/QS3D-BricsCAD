# Work claim — Semantic Schedule category snapshot bound

- Status: `ACTIVE`
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
- This claim record.

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
