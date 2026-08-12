# Work claim — Auto Room active-id canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-auto-room-active-id-canonicality-20260812-0749`
- Registered: `2026-08-12T07:49:00+07:00`
- Baseline main SHA: `7014868bd5ee1da9fda48f3c9ae90b35bc6fce47`
- Priority: evidence-driven Core lifecycle correctness during owner-requested `continue all`

## Reserved scope

Canonicalize `activeRoomIds` inside `AutoRoomLifecycle.MarkStaleForSelection` so active semantic Room identity is independent of the caller collection comparer and surrounding whitespace.

## Expected surfaces

- `src/QS3D.Core/Domain/AutoRoomLifecycle.cs`
- focused existing/new Core smoke regression around Auto Room canonical stale scope
- this claim file for close-out

## Concrete defect

`MarkStaleForSelection` canonicalizes selected source handles and compares Floor/Zone IDs using trimmed case-insensitive identity, but it calls `activeRoomIds.Contains(room.Id)` directly. A caller-provided case-sensitive set, or an active ID with surrounding whitespace, can therefore fail to protect the matching active Room and falsely mark it stale even though the semantic ID is the same.

## Explicit exclusions

- No Room source-signature hashing/tessellation, topology detection, selected-source filtering semantics, stale timestamp/reason contract, family synchronization, quantity exclusion, Room Auto native command/UI lifecycle, BricsCAD runtime, Actions, release, or LOCAL_PASS changes.

## Validation plan

- Snapshot active IDs into a trimmed `StringComparer.OrdinalIgnoreCase` set before stale selection.
- Preserve existing selection/scope filters, deterministic ordering, single project `Touch()`, and stale metadata behavior.
- Add focused smoke coverage where the active Room ID is supplied with different casing/whitespace through a case-sensitive caller set; require that active Room to remain active while an otherwise matching inactive Room becomes stale.
- Re-fetch source/test after claim before editing and never overwrite concurrent edits.
- No GitHub Actions will be dispatched and no local .NET/BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

Active Room identity is canonical inside stale selection, focused regression is committed on current `main`, and this claim is marked `COMPLETED`.
