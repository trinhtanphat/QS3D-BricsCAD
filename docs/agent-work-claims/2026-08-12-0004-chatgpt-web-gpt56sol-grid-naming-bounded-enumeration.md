# Work claim — Grid naming bounded enumeration

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:04:00+07:00`
- Completed: `2026-08-12T00:09:14+07:00`
- Baseline main SHA observed: `a269ba2e35530daa7b7c03dc472227948b3c626c`
- Claim commit: `c324c7e8447cbb4d66ef823db5c27a624cc8c9b3`
- PR: `#556`
- Squash merge on `main`: `af34a621133360f3ca19982f3fbef1ff034f0721`
- Priority: P1 — deterministic Core resource-bound correctness.

## Defect closed

`GridNamingService.Renumber()` declared `MaxGridBatch = 2000` but previously executed `orderedGridElementIds.Select(...).ToList()` before checking `ids.Count > MaxGridBatch`. The capacity therefore limited only accepted cardinality, not enumeration/allocation: a huge or non-terminating lazy source could be consumed without bound before the guard executed.

## Implemented

- Replaced unrestricted LINQ materialization with one-pass bounded buffering.
- Accepted inputs of up to 2,000 continue to use the existing indexed `Required(...)` normalization.
- The 2,001st yielded item now triggers the existing capacity `InvalidOperationException` immediately.
- `Renumber()` never requests item 2,002 after oversize input is known.
- Existing empty-input, duplicate-id, naming-format, sequence, target/category, label-collision, ordering, no-op and real-mutation semantics remain unchanged.
- Added `GridNamingBoundedEnumerationSmoke` with an adversarial unbounded source that throws if item 2,002 is requested.
- Added isolated module registration, `scripts/preflight-grid-naming-bounded-enumeration.py`, and detailed plan `docs/plans/2026-08-12-grid-naming-bounded-enumeration.md`.

## Regression contract

For an unbounded valid-id source:

- yield count reaches exactly 2,001;
- `GridNamingService.Renumber()` throws `A Grid renumber batch supports at most 2000 elements.`;
- source item 2,002 is not requested;
- `ProjectState.ChangeVersion` remains unchanged.

The focused preflight rejects the legacy `.Select(...).ToList()` / post-materialization capacity path and requires the capacity check to precede normalization/add and project resolution.

## Moving-main safety

- Post-claim source was re-fetched from `main` at `c324c7e8447cbb4d66ef823db5c27a624cc8c9b3` before writes and still contained the defect.
- Before PR creation, moving `main` was 10 commits ahead with no overlap in `GridNamingService.cs` or this lane's new files.
- Before merge, moving `main` was 22 commits ahead; compare again showed no overlap.
- Five additional commits landed immediately before merge; those also had no overlap with this lane.
- PR #556 was squash-merged through GitHub's merge endpoint with expected head `43d64a7b99184cea6150a8ee351eaa09a4c012a0`; GitHub returned merge success. No force update was used.

## Validation

- PR #556 changed exactly five expected files: one Core source file plus new smoke, module registration, focused preflight and detailed plan.
- Source/diff review confirms the Core change is limited to bounded materialization in `Renumber()`.
- The adversarial smoke distinguishes the fix from the old implementation because the legacy `.ToList()` path would request item 2,002 and hit the sentinel exception instead of the public capacity error.
- The current container's earlier direct GitHub checkout attempt failed DNS resolution, so no executable smoke/preflight PASS is claimed.
- No GitHub Actions workflow was dispatched, in accordance with repository manual-only policy.
- No BricsCAD V25 runtime PASS is claimed; this lane is pure Core logic and changes no native CAD surface.

## Completion evidence

PR #556 is merged on `main` as `af34a621133360f3ca19982f3fbef1ff034f0721`. Grid naming capacity now bounds lazy-source enumeration itself, not only post-materialization accepted cardinality.