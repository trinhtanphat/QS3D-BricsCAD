# Work claim — Wall Junction analysis read-only integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-wall-junction-readonly`
- Registered: `2026-08-11T20:55:00+07:00`
- Baseline main SHA: `8fc958c3b67d7466928d7933451aca7a148b1a82`
- Claim commit: `e11e28bec909c5328c3804f7297c48ddc33591e6`
- Priority: keep `QS3DWALLJUNCTIONS` as a true analysis command so inspecting junction topology cannot mutate persisted project state or advance `ChangeVersion`.

## Reserved scope

Audit and harden `QS3DWALLJUNCTIONS` read-only lifecycle. Remove command-side project mutation that exists only for analysis telemetry while preserving selection-first behavior, optional project metadata settings, non-creating semantics, topology diagnostics and Wall Snap preview/apply ownership.

## Implementation surfaces

- `src/QS3D.BricsCAD.V25/WallJunctionCommands.cs`
- `scripts/preflight-wall-junctions.py`
- this claim file

## Excluded scope

- No `QS3DWALLSNAPPREVIEW` / `QS3DWALLSNAPAPPLY` mutation contract changes.
- No Core `AuditTrail`, ProjectState atomicity or transaction primitive changes.
- No generated-source recognition, Create Similar, quantity/UI, material refresh, Direct Draw or Level Z-chain work.
- No BricsCAD V25 runtime PASS, installer/signing, private-DWG qualification or release work.

## Proven defect fixed

`QS3DWALLJUNCTIONS` is presented and structured as analysis, but previously resolved `ExistingProjectMutationContext` after selection and recorded `wall.junction.analyze` through `AuditTrail.ForProject(project).Record(...)`. `AuditTrail.Record` calls `ProjectState.Touch()`, so merely inspecting wall junction topology advanced persisted project state and `ChangeVersion`.

The command now consumes the `ProjectState` returned by `ProjectContextCoordinator.TryGetReadOnly(...)` directly for optional tolerance/sagitta/planarity metadata. It no longer binds a mutation context and no longer writes an audit event. Selection-first behavior, non-project fallback defaults, geometry reads, topology planning and diagnostics remain unchanged.

## Regression contract

`scripts/preflight-wall-junctions.py` now isolates `AnalyzeWallJunctions` and requires the selection → read-only project lookup → geometry read → planner lifecycle. It rejects mutation-only surfaces inside that method, including `ExistingProjectMutationContext`, `AuditTrail.ForProject`, `ProjectContextCoordinator.GetOrCreate`, project save/pending-save paths, `Touch()` and `Record()`.

Wall Snap preview/apply guards remain intentionally unchanged and still require their persisted audit/mutation behavior.

## Product commits

- `bd381161c04393ffc0de30b4b26056b317e37057` — `fix(wall): keep junction analysis read-only`
- `5c4b1ee389fca59489e7d669aaa78119d0617a69` — `test(wall): guard junction analysis read-only integrity`

## Validation

- Re-fetched `WallJunctionCommands.cs` from remote `main` after both product commits and confirmed the analysis method uses the read-only snapshot directly and contains no audit/mutation-context call.
- GitHub combined status for `5c4b1ee389fca59489e7d669aaa78119d0617a69` returned no status checks.
- GitHub workflow lookup for the same commit returned no workflow runs.
- No full C# build, BricsCAD V25 `NETLOAD`, private-DWG runtime, installer/signing or release qualification is claimed in this lane.
