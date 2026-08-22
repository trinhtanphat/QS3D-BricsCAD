# Work claim — ProjectElement null persisted-scalar persistability

- Status: `RELEASED`
- Agent: `gpt56sol-project-element-null-scalar-persistability-20260814-1334`
- Registered: `2026-08-14T13:34:00+07:00`
- Released: `2026-08-14T13:35:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline observed main SHA: `f9466f3400e0c85b4702646ecf62b4d11d8f86fe`

## Confirmed defect

`ProjectElement` constructor canonicalizes nullable `familyId`, `floorId`, and `zoneId` to non-null strings, and `DrawingFingerprint` starts as `string.Empty`. After construction, however, all four are public auto-properties. Runtime callers can assign null and leave the semantic element in a representation that `QsdbProjectStore.Serialize(...)` later rewrites to `string.Empty` (`?? string.Empty`), while load also returns non-null strings.

## Release reason

After this claim was created, the guarded source write returned HTTP 409 because `src/QS3D.Core/Domain/ProjectElement.cs` had changed. Refresh showed an earlier concurrent claim, `ProjectElement id persistability`, registered at 13:31 and actively modifying the same production file. The earlier claim was not returned by the initial indexed search in time.

This lane therefore stops without any `ProjectElement.cs` mutation, test creation, rebase, force-push or overwrite. The null-scalar observation remains a future candidate after the active ProjectElement owner finishes and current source is re-audited.

## Reserved scope released

- `src/QS3D.Core/Domain/ProjectElement.cs`
- `tests/QS3D.Core.SmokeTests/ProjectElementNullScalarPersistabilitySmoke.cs` (never created)
- this claim file

## Validation boundary

No source/test change from this lane. GitHub Actions and BricsCAD runtime were not run.
