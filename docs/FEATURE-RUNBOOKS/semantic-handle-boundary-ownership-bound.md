# Semantic handle Auto Room boundary ownership bound

## Scope

This Core-only contract governs how `SemanticHandleOwnershipResolver.Resolve(...)` consumes persisted Auto Room `BoundarySourceHandles` when a Room has no explicit `SourceHandles`.

## Invariant

Auto Room boundary provenance supports at most 5,000 source handles. Semantic selection must enforce that ceiling before materializing an unbounded token array, using one bounded split with a 5,001st sentinel entry. Metadata above the ceiling fails closed before any boundary alias is published into the selection ownership index.

The persisted boundary string must also remain canonical. After bounded tokenization, the tokens are normalized through `AutoRoomLifecycle.NormalizeSourceHandles(...)`; the resulting canonical string must match the persisted text exactly. Empty tokens, padding, duplicate/noncanonical structure, or other formatting drift must therefore fail closed rather than be silently repaired by semantic selection.

## Preserved behavior

- Explicit `SourceHandles` continue to take precedence. If an Auto Room has an explicit source handle, dormant boundary provenance is not parsed or promoted as an alias.
- Valid canonical boundary lists containing 1–5,000 handles remain selectable.
- Shared canonical boundary handles across different Auto Rooms remain ambiguous and fail closed through the existing ownership index.
- Boundary provenance is not promoted into global generated-handle ownership.
- Selected-handle Count/cap and project-change integrity checks are unchanged.

## Regression coverage

`SemanticHandleBoundaryOwnershipBoundSmoke` proves exact-boundary success, over-limit fail-closed behavior, noncanonical delimiter rejection, and explicit-source precedence even when dormant boundary metadata is oversized.

`preflight-semantic-handle-boundary-ownership-bound.py` pins the production ordering and rejects regression to the historical unbounded `Split(..., RemoveEmptyEntries)` path. `preflight-auto-room-canonical-selection.py` is strengthened to require the same bounded/canonical contract while preserving its existing Workspace/Auto Room ownership guarantees.

## Runtime boundary

No BricsCAD/native runtime is required. This is deterministic `QS3D.Core` semantic ownership/data-integrity behavior and is eligible for remote Shared CI evidence. It must not be represented as licensed BricsCAD `LOCAL_PASS` evidence.
