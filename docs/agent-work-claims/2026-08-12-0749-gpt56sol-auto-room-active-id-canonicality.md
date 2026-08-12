# Work claim — Auto Room active-id canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-auto-room-active-id-canonicality-20260812-0749`
- Registered: `2026-08-12T07:49:00+07:00`
- Baseline main SHA: `7014868bd5ee1da9fda48f3c9ae90b35bc6fce47`
- Priority: evidence-driven Core lifecycle correctness during owner-requested `continue all`

## Reserved scope

Canonicalize `activeRoomIds` inside `AutoRoomLifecycle.MarkStaleForSelection` so active semantic Room identity is independent of the caller collection comparer and surrounding whitespace.

## Concrete defect

`MarkStaleForSelection` canonicalized selected source handles and compared Floor/Zone IDs using trimmed case-insensitive identity, but called `activeRoomIds.Contains(room.Id)` directly. A caller-provided case-sensitive set, or an active ID with surrounding whitespace, could therefore fail to protect the matching active Room and falsely mark it stale even though the semantic ID was the same.

## Implementation

- `094f0ea79dad5b295f879a64046eb7dd3131fb64` — snapshots active Room IDs through trim + `StringComparer.OrdinalIgnoreCase` before stale filtering; all existing selected-source, Floor/Zone, ordering, `Touch()`, and stale metadata behavior remains unchanged.
- `f9cf0a6e97bc1bf156a661493863b494198aea6d` — extends canonical Auto Room smoke coverage with a case-sensitive caller set containing a padded/lowercase active ID; verifies that active Room stays active, the otherwise matching inactive Room becomes stale, and `ChangeVersion` increments once.

## Validation

- Re-read `AutoRoomLifecycle.cs` from current `main`; source blob `cd70a7ee2a59f5be8ac27fc05f161be68e8adb42` contains canonical active-ID snapshotting.
- Re-read `AutoRoomCanonicalScopeSmoke.cs` from current `main`; test blob `16ce6d59ca2c01ca2f2c56fb6c43efd3775d4588` contains the focused regression.
- No GitHub Actions were dispatched.
- No local .NET compile/test runner or BricsCAD V25/V26 runtime PASS is claimed from this web session.

## Explicit exclusions

- No Room source-signature hashing/tessellation, topology detection, selected-source filtering semantics, stale timestamp/reason contract, family synchronization, quantity exclusion, Room Auto native command/UI lifecycle, BricsCAD runtime, Actions, release, or LOCAL_PASS changes.

## Completion

Active Room identity is canonical inside stale selection, focused regression is committed on `main`, and this source-only claim is complete.
