# Work claim — FloorDefinition elevation signed-zero canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-floor-elevation-signed-zero-20260813`
- Registered: `2026-08-13T19:35:00+07:00`
- Baseline main SHA: `ed9db67e6b3253053a9ca1b484be98bf049bc4b1`

## Confirmed defect

`FloorDefinition.ElevationM` is persisted semantic state. Its setter rejects NaN/infinity but stores every finite `double` verbatim, so `-0d` survives as a distinct IEEE-754 representation. The repository already contains a downstream workaround in `FloorGeneratedIdentityPlanner` (`565197840cf397863f36126ca073dbcb3281a1ca`) that canonicalizes floor elevation only while building generated identity, proving the source `FloorDefinition` can retain signed zero while leaving other consumers/persistence exposed to the raw representation.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectState.cs` — only `FloorDefinition.ElevationM` storage normalization
- `tests/QS3D.Core.SmokeTests/FloorDefinitionSignedZeroSmoke.cs`
- this claim file

## Intended change

Canonicalize accepted zero elevation to literal `+0d` in the `FloorDefinition.ElevationM` setter while preserving negative/positive nonzero elevations and existing non-finite refusal. Add bit-level regression for constructor and setter paths plus nonzero/nonfinite cases.

## Excluded scope

No ProjectState scalar/version semantics, ProjectFloorService mutation behavior, generated identity planner, persistence schema, UI/native BricsCAD, ModelHealth, CST/cost, Formula, CI/release or licensed runtime changes.

## Coordination

- `FloorDefinition signed zero` and `FloorDefinition elevation` recent searches returned no competing claim.
- `floor elevation signed-zero` found only the historical downstream generated-identity workaround `565197840cf397863f36126ca073dbcb3281a1ca`, not a storage-boundary fix.
- `ProjectState.cs` recent path history has no commit newer than 2026-08-12 before this claim.
- Concurrent ModelHealth numeric-source-identity work is disjoint.

## Validation

Refresh moving `main` before source mutation, constrain the production diff to the one setter assignment, add focused `[ModuleInitializer]` smoke, exact source/test readback before closeout, and do not claim execution gates not actually run.
