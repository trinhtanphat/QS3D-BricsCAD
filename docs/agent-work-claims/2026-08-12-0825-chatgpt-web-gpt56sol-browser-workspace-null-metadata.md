# Work claim — Browser workspace null metadata fail-closed load

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T08:25:00+07:00`
- Completed: `2026-08-12T08:27:00+07:00`
- Baseline main SHA: `5466eb22a7c501ce9b97fbaebd0587dd5d20667f`
- Priority: P1 — close the remaining malformed metadata boundary without changing valid workspace behavior.

## Confirmed defect

`ProjectBrowserWorkspaceStateStore.Load(ProjectState)` treated a present metadata key as persisted state, but dereferenced `serialized.Length` before guarding a null dictionary value. `ProjectState.Metadata` is a public mutable `IDictionary<string,string>`, so a malformed in-memory/persisted project could contain a present null value and leak `NullReferenceException` rather than the store's fail-closed `InvalidDataException` corruption contract.

The earlier completed empty-metadata lane covered present empty and whitespace values but not a present null dictionary value.

## Completed work

- Removed the redundant pre-deserialization `.Length` check in `Load(...)`, so every present metadata value is routed through canonical `Deserialize(...)`.
- `Deserialize(...)` already rejects null/empty/whitespace via `string.IsNullOrWhiteSpace(...)` with `InvalidDataException` and still enforces the same oversized-state bound immediately afterward.
- Extended `ProjectBrowserWorkspaceEmptyMetadataSmoke` with a present-null case and reused its existing metadata/freshness no-mutation checks.
- Missing-key default behavior, canonical round-trip, Save/Clear dirty tracking, XML canonicality and schema behavior remain unchanged.

## Reserved scope

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceEmptyMetadataSmoke.cs`
- this claim file

## Integration evidence

- Claim registration: `7c30bd81f06d3d654ca368e00337dde7dad156f8`.
- Source fix on `main`: `ac80d38b643b1206ff4ca936d61522ebed8a2daa`.
- Focused regression update on `main`: `e90d4bd983f480b832fdabe47d0d85c47d85b757`.
- Exact source diff removes only the duplicate pre-deserialization length guard; the same maximum-size check remains in `Deserialize(...)`.
- Exact smoke diff adds `NullMetadataFailsWithoutMutation()` and makes the existing corruption helper nullable for the test fixture.

## Validation boundary

Both reserved diffs were read back from GitHub after integration. GitHub Actions were not dispatched; executable smoke/build PASS and BricsCAD runtime PASS are not claimed.
