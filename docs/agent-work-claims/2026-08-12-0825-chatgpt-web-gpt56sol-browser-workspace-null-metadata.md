# Work claim — Browser workspace null metadata fail-closed load

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T08:25:00+07:00`
- Baseline main SHA: `5466eb22a7c501ce9b97fbaebd0587dd5d20667f`
- Priority: P1 — close the remaining malformed metadata boundary without changing valid workspace behavior.

## Confirmed defect

`ProjectBrowserWorkspaceStateStore.Load(ProjectState)` treats a present metadata key as persisted state, but dereferences `serialized.Length` before guarding a null dictionary value. `ProjectState.Metadata` is a public mutable `IDictionary<string,string>`, so a malformed in-memory/persisted project can contain a present null value and currently leaks `NullReferenceException` rather than the store's fail-closed `InvalidDataException` corruption contract.

The completed empty-metadata lane explicitly covered present empty and whitespace values but not a present null dictionary value.

## Reserved scope

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs` — null persisted metadata guard in `Load(...)` only
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceEmptyMetadataSmoke.cs` — extend the existing focused malformed-metadata regression
- this claim file

## Contract

- Missing metadata key continues to return canonical default state.
- Present null, empty, whitespace, malformed or oversized metadata fails closed without mutation.
- Present null specifically throws `InvalidDataException`, not `NullReferenceException`.
- Canonical metadata round-trip, Save/Clear dirty tracking, XML canonicality and schema behavior remain unchanged.
- No shared registration change is needed because the existing empty-metadata smoke is already module-initializer registered.

## Validation boundary

Re-fetch both reserved files immediately before writing, apply the smallest source/test delta, read back exact results and close with commit SHAs. No GitHub Actions dispatch; no executable smoke/build or BricsCAD runtime PASS claim unless actually run.
