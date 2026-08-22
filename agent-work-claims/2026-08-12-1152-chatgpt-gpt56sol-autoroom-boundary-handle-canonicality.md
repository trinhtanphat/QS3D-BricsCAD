# Work claim — Auto Room boundary source handle canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-autoroom-boundary-handle-canonicality-20260812`
- Registered: `2026-08-12T11:52:00+07:00`
- Completed: `2026-08-12T12:01:00+07:00`
- Baseline main SHA: `0c93d6b24da2978a2ec76ca61b8ad17eb0c0a864`
- Integration SHA: `e782ab6760b0f6cde9a09ecc04ca973095df86ca` (PR #854)
- Priority: P1 — Locate must not silently normalize malformed persisted Auto Room boundary ownership metadata
- Task Key: `CORE-AUTOROOM-BOUNDARY-SOURCE-HANDLES-CANONICALITY`

## Confirmed defect

`RoomBoundaryCommands` persists `BoundarySourceHandles` from the `sourceSignature` returned by `AutoRoomLifecycle.NormalizeSourceHandles(...)`. The exact writer contract is trim + invariant uppercase + case-insensitive distinct + deterministic ordinal-ignore-case sort + semicolon join.

`SourceHandleResolver.AddBoundaryHandles(...)` previously parsed the persisted value with `StringSplitOptions.RemoveEmptyEntries`, trimmed every token, and silently de-duplicated through the global handle set. Malformed persisted metadata such as leading/trailing whitespace, lowercase/noncanonical case, empty `;;` entries, duplicate/case-alias handles, or noncanonical ordering was therefore silently accepted by Locate even though the Room Auto writer cannot produce those representations.

## Completed repair

- Non-empty persisted `BoundarySourceHandles` is split without dropping empty tokens and validated against `AutoRoomLifecycle.NormalizeSourceHandles(...)` before any boundary handle is added.
- Null/noncanonical representations now fail closed with a repair-oriented Locate error.
- Empty-string boundary snapshots remain valid and continue to fall back to generated ownership.
- The existing 5000-handle bound is preserved.
- Direct → boundary → generated precedence and deterministic canonical boundary ordering are preserved.
- Focused Core smoke covers canonical resolution, whitespace/case/order/empty-token/duplicate rejection, empty generated fallback, and direct-source precedence.

## Readback / validation boundary

PR #854 was inspected as exactly two changed files before guarded squash merge. Integration SHA: `e782ab6760b0f6cde9a09ecc04ca973095df86ca`. Source and focused smoke were read back from `main` after integration.

No GitHub Actions/full .NET build/executable smoke/BricsCAD V25/V26 runtime PASS was claimed or executed.
