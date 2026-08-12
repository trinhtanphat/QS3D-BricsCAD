# Work claim — Locate boundary-handle resource bound

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-locate-boundary-handle-bound-20260812-0902`
- Registered: `2026-08-12T09:02:00+07:00`
- Baseline main SHA: `7b16210497167156599a3e4f9080817511054182`
- Priority: evidence-driven persisted-input resource bound during owner-requested review/fix continuation

## Confirmed defect

`SourceHandleResolver.AddBoundaryHandles(...)` reads persisted `AutoRoomLifecycle.BoundarySourceHandlesKey` through an unbounded `Split(';')`. Canonical Room boundary discovery already rejects more than 5,000 source segments, but malformed delimiter-dense persisted metadata can allocate a token array far beyond that supported topology before Locate fails or completes.

## Reserved scope

- `src/QS3D.Core/Services/SourceHandleResolver.cs` — boundary-handle tokenization/count guard only.
- `tests/QS3D.Core.SmokeTests/SourceHandleBoundaryResourceBoundSmoke.cs` — focused CAD-independent regression.
- this claim file.

## Contract

- Materialize at most 5,001 non-empty boundary-handle tokens using the existing Room boundary 5,000-source capacity as the persisted Locate bound.
- Reject more than 5,000 boundary source handles before adding any of them to the published Locate result.
- Preserve root input bound/freshness, direct source-handle canonicality, dependency validation, generated-owner fallback, ordering/deduplication and Auto Room provenance semantics.
- Do not edit `AutoRoomLifecycle.cs`, Room boundary discovery, command/native Locate code or existing Locate smoke files.

## Validation plan

Prove 5,000 persisted boundary handles are accepted and returned in order, 5,001 fail closed with `InvalidOperationException`, and a small ordinary boundary-handle property preserves existing Locate behavior. Re-fetch exact source before write; never force-push. No GitHub Actions dispatch, executable full test PASS or licensed BricsCAD runtime qualification claim.