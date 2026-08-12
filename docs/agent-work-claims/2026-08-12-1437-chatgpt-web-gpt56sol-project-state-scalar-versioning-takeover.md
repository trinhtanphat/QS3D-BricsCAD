# Work claim — ProjectState persisted scalar versioning owner takeover

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-project-state-scalar-versioning-owner-takeover-20260812`
- Registered: `2026-08-12T14:37:00+07:00`
- Baseline main SHA: `f2bff143d6dd199b10d38044574d6a296018d314`
- Priority: repository-owner coordinated continuation of the persisted-scalar freshness lane
- Predecessor: `docs/agent-work-claims/2026-08-12-1436-chatgpt-web-gpt56sol-project-state-persisted-scalar-versioning.md`

## Coordination

Repository owner `trinhtanphat` explicitly requested that the remaining work be finished. This successor reservation is published first; the predecessor reservation will be marked `RELEASED` immediately after this commit. The brief overlap is an explicit owner-coordinated handoff, and no source/test work begins until the predecessor release is visible on `main`.

## Reserved scope

Fix `ProjectState` persisted scalar mutation freshness so real changes to `DrawingPath`, `DrawingFingerprint`, `ActiveZoneId`, and `ActiveFloorId` advance persistence state.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectState.cs`
- one focused deterministic Core smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

- Each of the four persisted scalars uses exact ordinal equality; assigning the current value is a no-op.
- A real value change advances `ChangeVersion` exactly once and refreshes `UpdatedUtc` exactly once.
- Preserve exact string storage semantics; no trimming, casing normalization, Floor/Zone validation, schema, XML, or serialization changes.
- Keep `ProjectState.Touch()` unchanged.
- Preserve snapshot hydration cleanliness: snapshot assignment may invoke the setters, after which `ProjectStateSnapshot.CopyInto()` restores the persisted timestamp/version through the existing restore path.

## Validation plan

Focused CAD-independent smoke covers all four changed-scalar paths, same-value no-op behavior, and snapshot restore version/timestamp preservation. Read back source/test from current `main` after landing and verify ancestry. No GitHub Actions and no licensed BricsCAD runtime PASS.

## Completion condition

Implementation + focused smoke land on `main`, source/test are read back, and this successor claim is marked `COMPLETED` with exact landed SHA(s) and validation limits.
