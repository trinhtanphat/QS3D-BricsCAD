# Work claim — Auto Room boundary source handle canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-autoroom-boundary-handle-canonicality-20260812`
- Registered: `2026-08-12T11:52:00+07:00`
- Baseline main SHA: `0c93d6b24da2978a2ec76ca61b8ad17eb0c0a864`
- Priority: P1 — Locate must not silently normalize malformed persisted Auto Room boundary ownership metadata
- Task Key: `CORE-AUTOROOM-BOUNDARY-SOURCE-HANDLES-CANONICALITY`

## Confirmed defect

`AutoRoomLifecycle` writes `BoundarySourceHandles` as a canonical semicolon-separated snapshot: input handles are nonblank, trimmed, case-insensitively distinct, deterministically ordered, then joined with `;`.

`SourceHandleResolver.AddBoundaryHandles(...)` currently parses the persisted value with `StringSplitOptions.RemoveEmptyEntries`, trims every token, and silently de-duplicates through the global handle set. This lets malformed persisted metadata such as leading/trailing token whitespace, empty `;;` entries, duplicate/case-alias handles, or noncanonical ordering be silently accepted by Locate even though the lifecycle writer cannot produce those representations. Neighboring persisted `DependsOn` and `SourceHandles` are already fail-closed on noncanonical data.

## Reserved scope

- `src/QS3D.Core/Services/SourceHandleResolver.cs`
- one focused Core smoke and registration if required
- this claim file

## Intended repair

- Validate persisted `BoundarySourceHandles` against the lifecycle writer's canonical serialization before adding any boundary handle.
- Fail closed on empty tokens, token whitespace, case-insensitive duplicates, and noncanonical ordering/serialization.
- Preserve the 5000-handle bound, direct → boundary → generated precedence, deterministic returned handle order, and valid Auto Room Locate behavior.

## Validation boundary

Deterministic source/smoke diff and GitHub readback only. No GitHub Actions/full .NET build/executable smoke/BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.
