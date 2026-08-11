# GPT-5.6 Sol repository audit + reporting identity hardening

- Status: `ACTIVE`
- Started: `2026-08-11T19:55:00+07:00`
- Agent: `GPT-5.6 Sol / ChatGPT Web`
- Task: repository-wide source audit, multi-agent planning/governance review, and a narrow proven Reporting/Quantity identity-safety fix

## Multi-agent registration rule

This repository is being edited by multiple concurrent coding agents. Every agent MUST register what it is going to do in a committed Markdown record under `docs/agent-work-claims/` and push that registration to `origin/main` before touching substantive implementation files. The record must name the agent, task, exact reserved files/scope, planned changes, validation, dependencies/risks, and status. Agents must re-read `main` before implementation and before push, must not overwrite another ACTIVE reservation, and must update the record to `COMPLETE`/`BLOCKED` with commit hashes and handoff notes when finished.

Canonical policy remains `docs/AGENT-WORK-REGISTRATION.md` and `AGENTS.md`; this claim follows that policy rather than creating a competing protocol.

## Reserved scope

- `src/QS3D.Core/Reporting/QuantityReportBuilder.cs`
- `tests/QS3D.Core.SmokeTests/ProjectQuantitySmoke.cs`
- `docs/REPOSITORY-AUDIT-PLAN-2026-08-11.md`
- this reservation record

No edits are permitted from this claim in currently reserved atomicity work (`src/QS3D.Core/MEP/**`, `QuickCreateService.cs`, `QuickRemoveService.cs`) or other agents' active lanes.

## Preliminary audit finding selected for implementation

`ProjectQuantityReportBuilder` already rejects duplicate semantic element IDs, but the legacy `QuantityReportBuilder.Group(IEnumerable<ElementInstance>)` path does not. Supplying the same identity twice silently doubles count and all quantities while duplicating provenance. In a BIM/takeoff pipeline this is unsafe because duplicate semantic identity is corruption/ambiguous provenance, not two independent physical elements. The legacy path should fail closed consistently with the project-backed reporting path.

## Detailed implementation plan

1. **Coordination audit**
   - Re-read latest `main`, recent commits, open PR/issues, `AGENTS.md`, `docs/AGENT-WORK-REGISTRATION.md`, and active claims.
   - Avoid all overlapping ACTIVE reservations.
   - Record repository-wide findings and prioritized follow-up lanes in `docs/REPOSITORY-AUDIT-PLAN-2026-08-11.md`.

2. **Reporting identity hardening**
   - Add case-insensitive duplicate `ElementInstance.Id` detection before legacy grouped quantities are accumulated.
   - Preserve first-seen deterministic ordering and all existing grouping behavior.
   - Fail closed with a precise `InvalidOperationException` rather than silently double-counting.

3. **Regression coverage**
   - Extend `ProjectQuantitySmoke` so both duplicate object reuse and separate instances carrying the same case-insensitive identity are rejected by `QuantityReportBuilder.Group`.
   - Keep the existing project-backed quantity/report regression suite unchanged except for the new coverage.

4. **Repository-wide planning document**
   - Summarize architecture/build/test/release/BricsCAD-local boundaries observed during audit.
   - Separate source-safe work from local-only V25/DWG/UI proof.
   - List prioritized workstreams, ownership boundaries, acceptance criteria, and multi-agent handoff rules.
   - Do not mark speculative TODOs as bugs unless source/contracts prove the defect.

5. **Validation**
   - Static source review against current HEAD.
   - Confirm modified file SHAs have not changed since reservation before write.
   - Inspect smoke-test registration so the extended `ProjectQuantitySmoke.Run()` remains executed.
   - Check commit status/workflow metadata after push when available.
   - Any BricsCAD V25/runtime-only validation remains a local-agent handoff and must not be falsely reported as remote proof.

## Dependencies / risks

- `main` is highly concurrent; HEAD may advance during this task. Writes must be fast-forward only and rebased/reconstructed on the newest tree if needed.
- Do not edit another agent's ACTIVE files merely to make a broader cleanup look complete.
- GitHub connector cannot execute the Windows/BricsCAD runtime itself; source-safe validation and runtime-local validation must be reported separately.

## Completion record

Pending implementation and final audit commit(s).
