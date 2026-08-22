# Work claim — Bulk family property canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-bulk-family-property-canonicality`
- Registered: `2026-08-12T07:46:00+07:00`
- Last Updated: `2026-08-12T07:54:00+07:00`
- Baseline main SHA: `0696f3cbcf602e140c3cad23282160641f2e659d`
- Priority: deterministic Core assignment-integrity mismatch found during owner-requested evidence-driven audit
- Task Key: `CORE-BULK-FAMILY-PROPERTY-CANONICALITY`

## Confirmed defect

`ProjectFamilyService.Assign(...)` already snapshots and validates target Family defaults plus every previous Family's properties before project or instance mutation. `BulkEditService.AssignFamily(...)` bypassed that boundary by consuming raw Family property dictionaries and writing their entries directly into semantic element properties.

That allowed malformed legacy Family defaults—such as padded/non-canonical keys or over-bound values—to be rejected by the canonical assignment API but propagated by the supported BulkEdit assignment path while changing FamilyId, dirty state and project revision.

## Implemented scope

`BulkEditService.AssignFamily(...)` now reuses `ProjectFamilyService.SnapshotProperties(...)` for the target Family and every distinct previous Family before entering `ProjectSemanticMutationExecutor`.

The mutation phase consumes only those canonical snapshots. Target keys are materialized once for inherited-default removal, previous snapshots are cached by Family id for inherited-value detection, and the existing FamilyId/no-op/override/dirty behavior remains intact.

Validation therefore happens before `ProjectState.Touch()` or any element mutation, matching the canonical `ProjectFamilyService.Assign(...)` contract without duplicating Family property policy.

## Committed evidence

- Claim registration: `0151f4b9ff18e9956c4b3d25530cdc0d1bd4c06a` — `chore(agent): claim bulk family property canonicality`
- Core fix: `aaa2f3e0b75473a6ab79a74bb0d5651b9c1be8d0` — `fix(core): validate bulk family defaults canonically`
- Focused smoke: `c28ac88bf3af038c2cd2f94dd46d3862d65fc770` — `test(core): guard bulk family property canonicality`
- Isolated smoke registration: `1d71cc43b768b88cef7c157495d523e4f1b71d54` — `test(core): register bulk family property canonicality smoke`
- Moving-main read-back on `3a766aeb9192ae12d42fc4f9bd2d27b05baaae37` confirmed source, smoke and isolated registration were all still present after concurrent commits.

The focused smoke locks:

- malformed target Family key rejection before FamilyId/property/dirty/version/timestamp mutation;
- malformed previous Family key rejection before inherited-default processing;
- over-bound target Family value rejection before mutation;
- valid reassignment semantics for inherited default replacement, inherited default removal, explicit override preservation, new target defaults, relation/property/quantity/geometry dirty flags and one project revision touch.

## Preserved behavior / exclusions

- The completed Bulk Family relation-dirty behavior from `847ee0f25c530d0a61bc0fdb813a7d6786def6eb` remains intact.
- `ProjectFamilyService.cs` was not modified; its existing canonical validator is the single policy source.
- WPF/native selection UI, persistence schema, Family create/rename/property editing and unrelated BulkEdit property/numeric operations were not modified.
- Canonical padded/case-varied same-Family no-op behavior remains intact for valid Family state.
- No unrelated ACTIVE claim was overwritten; no force-push or GitHub Actions/build/release dispatch was used.
- No local smoke/.NET execution or BricsCAD runtime qualification is claimed by this connector-only batch.

## Completion condition

Satisfied: every supported Core Family reassignment path, including BulkEdit, now fails before mutation on malformed Family property defaults and shares the same canonical Family property validation contract.