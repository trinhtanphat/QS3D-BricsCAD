# Work claim — Bulk object-target structural freshness

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:29:00+07:00`
- Completed: `2026-08-12T10:31:00+07:00`
- Baseline main SHA: `e8d3b8d72c18bc6ed1b11345396ebdd8ae8bf6a7`
- Claim commit: `ec9fd6f08a4ec1a71489cf356544bbdebce281d4`
- Source fix: `889f28d2836fc70ab965a7a4bbabbfad5664e0ee`
- Focused regression source: `b447be52de089b4116b845ecd1d36ddc8fd1ccc7`
- Priority: P1 — object-based bulk edits must not mutate stale detached element instances after caller enumeration changes project membership without a version bump.
- Task Key: `CORE-BULK-OBJECT-TARGET-STRUCTURAL-FRESHNESS`

## Confirmed defect

`BulkEditService.SetProperty(ProjectState, IEnumerable<ProjectElement>, ...)` and `MultiplyNumericProperty(...)` call `OwnedDistinct(...)`. That helper snapshots the project's current element-id → instance map before enumerating caller-provided `elements`. A lazy enumerable could yield a canonical element, then remove or replace that element in the public `project.Elements` collection without calling `project.Touch()`. `OwnedDistinct(...)` still accepted the previously snapshotted instance; the existing post-enumeration `ChangeVersion` check could not detect the structural change, so the operation could mutate a detached stale element and then advance the canonical project's version.

This differed from current Family/Zone assignment freshness contracts, which revalidate exact project ownership after caller enumeration.

## Completed scope

- `SetProperty(ProjectState, IEnumerable<ProjectElement>, ...)` preserves its existing target materialization and `ChangeVersion` freshness check, then revalidates every target by current `project.FindElement(element.Id)` + `ReferenceEquals` before update planning.
- `MultiplyNumericProperty(...)` applies the same post-version-check structural ownership guard before numeric parsing/multiplication planning.
- The existing `ChangeVersion` diagnostic retains precedence when caller enumeration explicitly changes semantic revision; the new guard catches structural membership changes that bypass `Touch()`.
- Removal and same-ID replacement now fail before service-owned property, element timestamp/dirty, or project version mutation.
- Stable object-target SetProperty/Multiply behavior, numeric parse/overflow semantics, target bounds/null/id checks and `ProjectSemanticMutationExecutor` behavior are unchanged.
- Bulk Family assignment, LOCAL-003 fixtures, selection/UI/native BricsCAD and persistence schema were not modified by this lane.

## Validation evidence

- Claim registration on `main`: `ec9fd6f08a4ec1a71489cf356544bbdebce281d4`.
- Source fix on `main`: `889f28d2836fc70ab965a7a4bbabbfad5664e0ee`.
- Focused regression source on `main`: `b447be52de089b4116b845ecd1d36ddc8fd1ccc7`.
- Post-integration source readback confirms `RequireTargetEnumerationFreshness(...)` remains first and `RequireCurrentElementOwnership(...)` runs before update planning in both object-target overloads.
- `BulkObjectTargetStructuralFreshnessSmoke` covers SetProperty with caller removal, MultiplyNumericProperty with same-ID replacement, and stable controls for both operations. Failure cases assert caller structural side effects remain while no service-owned stale/canonical mutation occurs.

## Validation boundary

The regression source was committed and read back but was not executed in this connector session. No force-push, GitHub Actions dispatch, executable full-smoke/build PASS or licensed BricsCAD V25/V26 runtime qualification is claimed.

## Completion

Completed. Object-based bulk property edits now fail closed when target element ownership changes structurally during caller enumeration without a semantic version bump. Reservation released.
