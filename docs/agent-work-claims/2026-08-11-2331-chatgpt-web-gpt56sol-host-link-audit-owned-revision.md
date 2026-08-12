# Work claim — HostLinkService audit-owned project revision

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-host-link-audit-owned-revision`
- Registered: `2026-08-11T23:31:00+07:00`
- Completed: `2026-08-11T23:33:00+07:00`
- Baseline main SHA: `d77510f2c65074916c0c8c4b041a2124ee8e7353`
- Reservation commit: `07ae74b40ead94a93066d9cab0a7f9956a9b0f4a`
- Priority: P1 — one logical audited host-link mutation should advance project revision once.

## Defect fixed

`HostLinkService` performed semantic changes inside `ProjectSemanticMutationExecutor`, called `project.Touch()`, and then called `AuditTrail.ForProject(project).Record(...)`. A project-bound `AuditTrail.Record(...)` already advances `ProjectState.ChangeVersion`, so link, unlink, and stale AutoHost-provenance cleanup advanced project revision twice for one audited mutation.

The redundant explicit touches were removed. The audit record remains inside the rollback-protected semantic mutation scope and is now the single project revision owner for these operations.

## Reserved scope

- `src/QS3D.Core/Services/HostLinkService.cs`
- `tests/QS3D.Core.SmokeTests/HostLinkCanonicalizationSmoke.cs`
- this claim file

## Published commits

- `c4f894ff142702609e0a8e54e24ebf34c35c6d17` — remove redundant project touches from audited host link/unlink/provenance-clear mutations.
- `98061aae1eaa884d1b9a8784cef3de85a42fcc0d` — assert one revision + one audit for link/unlink and stale AutoHost cleanup.

## Delivered contract

- Real link and unlink operations each advance project `ChangeVersion` exactly once and append exactly one audit event.
- Clearing stale AutoHost provenance without `HostWallId` advances once and records one audit event.
- Canonical no-op link/unlink behavior remains side-effect free.
- Element dirty-state updates and `ProjectSemanticMutationExecutor` rollback protection are unchanged.

## Validation notes

- Exact post-publication source diff removes only the three redundant `project.Touch()` calls immediately before project-bound audit records.
- Exact regression diff adds focused revision/audit count checks to the already-registered host-link smoke.
- No force-push was used; writes were SHA-guarded.
- GitHub Actions were not dispatched.
- This remote environment does not provide the exact BricsCAD V25/.NET qualification toolchain, so executable/native runtime PASS is not claimed.

## Excluded scope

- No Direct Draw/AutoHost command changes.
- No native DWG/regeneration/UI changes.
- No audit contract redesign.

## Completion condition

Satisfied for the source/static Core contract. Exact runtime/native qualification remains separate.
