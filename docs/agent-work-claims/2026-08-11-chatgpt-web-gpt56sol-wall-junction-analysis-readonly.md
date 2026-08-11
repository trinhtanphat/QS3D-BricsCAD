# Work claim — Wall Junction analysis read-only integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-wall-junction-readonly`
- Registered: `2026-08-11T20:55:00+07:00`
- Baseline main SHA: `8fc958c3b67d7466928d7933451aca7a148b1a82`
- Priority: keep `QS3DWALLJUNCTIONS` as a true analysis command so inspecting junction topology cannot mutate persisted project state or advance `ChangeVersion`.

## Reserved scope

Audit and harden `QS3DWALLJUNCTIONS` read-only lifecycle. Remove command-side project mutation that exists only for analysis telemetry while preserving selection-first behavior, optional project metadata settings, non-creating semantics, topology diagnostics and Wall Snap preview/apply ownership.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/WallJunctionCommands.cs`
- `scripts/preflight-wall-junctions.py`
- this claim file for close-out

## Excluded scope

- No `QS3DWALLSNAPPREVIEW` / `QS3DWALLSNAPAPPLY` mutation contract changes.
- No Core `AuditTrail`, ProjectState atomicity or transaction primitive changes.
- No generated-source recognition, Create Similar, quantity/UI, material refresh, Direct Draw or Level Z-chain work.
- No BricsCAD V25 runtime PASS, installer/signing, private-DWG qualification or release work.

## Proven defect before implementation

`QS3DWALLJUNCTIONS` is presented and structured as analysis, but after selection it resolves `ExistingProjectMutationContext` and records `wall.junction.analyze` through `AuditTrail.ForProject(project).Record(...)`. Current `AuditTrail.Record` calls `ProjectState.Touch()` before appending the event, so merely inspecting wall junction topology mutates persisted project state and advances `ChangeVersion`.

## Intended contract

- Selection remains acquired before any project lookup/creation.
- If a project exists, analysis may read current Wall Junction tolerance/sagitta/planarity settings from it.
- The command must never create a project, require a mutation context, append audit events, call `Touch()`, or otherwise advance persisted state merely to display analysis results.
- Wall Snap preview/apply remains unchanged because those commands intentionally persist preview/apply state.

## Validation plan

- Re-fetch latest `main`, claims, source and focused preflight immediately before source write.
- Update static preflight to require read-only project lookup and reject mutation-context/audit/touch calls inside `AnalyzeWallJunctions`.
- Inspect exact source/preflight diffs and main ancestry.
- Do not claim GitHub Actions/full C# build/V25 `NETLOAD`/private-DWG runtime unless actually executed.
