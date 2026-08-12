# Work claim — Floor mutation target input freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-floor-mutation-input-freshness`
- Registered: `2026-08-12T09:57:00+07:00`
- Completed: `2026-08-12`
- Baseline main SHA: `57c21d477cd8e5b47b30a95cfbc07566a9b2ce9c`
- Priority: P1 — fail-closed Core Floor mutation freshness at caller-controlled enumeration boundaries.

## Confirmed defect

`ProjectFloorService.Assign(...)`, `AssignBottomLevel(...)`, `AssignTopLevel(...)`, and `ClearVerticalLevels(...)` all passed caller-controlled `IEnumerable<ProjectElement>` targets into the shared `ResolveOwnedElements(...)` helper. That helper snapshotted project ownership, then enumerated caller code without checking whether the same `ProjectState` changed during enumeration. A lazy target sequence could call `project.Touch()` while yielding otherwise-owned targets; the calling mutation API then continued validation/no-op calculation and could mutate Floor/Level metadata against a newer project state.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectFloorService.cs`, limited to target-enumeration freshness in `ResolveOwnedElements(...)`
- focused Core smoke regression and registration under `tests/QS3D.Core.SmokeTests/`
- focused static preflight under `scripts/`
- `docs/plans/2026-08-12-floor-mutation-input-freshness.md`
- this claim file

## Implemented contract

- Capture `project.ChangeVersion` immediately before enumerating caller-supplied `elements` in `ResolveOwnedElements(...)`.
- Preserve project-element snapshotting, null/ownership validation, duplicate-target collapse, and deterministic ordering.
- Immediately after enumeration, fail closed with `InvalidOperationException` if the project version changed.
- Because all four mutation APIs share the helper, freshness drift is rejected before each caller's validation/no-op/mutation stage.
- Preserve stable-input behavior and existing Floor/Level semantics.

## Evidence

- Claim: `232b4faea453b3f72ad9179f88c5317802c621f4`
- Plan: `13a15ef19a0347b773f6ec7c4ba3ce79eedc2814`
- Source fix: `e5bd158b4eb745cc5a43e8415b588be27e932537`
- Deterministic smoke regression: `578505f2d869d4996b535b8a0f9ff0c07f5657d8`
- Smoke registration: `d98fa91a1582a484710ac7dad6d19f52e3c9ff69`
- Static preflight: `6c060a2b66a8c2ecc6b7d76224f4ed477d9ecd38`

## Validation evidence

- Current `main` readback confirmed helper ordering as project ownership snapshot → version capture → caller enumeration → freshness rejection → deterministic ordered return.
- Deterministic smoke source covers stable Floor assignment, mutating target assignment, mutating empty input, and a mutating Bottom Level call to prove the shared helper protects vertical-level mutation too.
- Static preflight is committed and requires the helper to remain shared by Assign, AssignBottomLevel, AssignTopLevel, and ClearVerticalLevels, plus smoke/registration presence.
- This connector-only session did not execute the full Core smoke executable, the Python preflight, GitHub Actions, or licensed BricsCAD V25/V26 runtime; no PASS claim is made for those environments.

## Coordination

Recent vertical-level canonicality and floor-integrity claims were verified completed before registration. This lane did not change reference canonicality, Floor create/update/delete, active-floor behavior, or UI audit wrappers.

## Excluded scope

- Floor/Level reference canonicality and vertical placement calculations.
- Floor create/update/delete/activate semantics.
- Floor/Zone UI audit behavior.
- Persistence, Actions, build/release dispatch, or licensed BricsCAD runtime qualification.

## Completion condition

`COMPLETED`: all four shared Floor mutation paths now fail closed when caller-controlled target enumeration changes the project, focused regression/preflight coverage is committed, exact integration SHAs are recorded, and remote validation limitations are explicit.
