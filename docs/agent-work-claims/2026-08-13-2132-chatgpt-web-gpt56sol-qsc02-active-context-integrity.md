# Work claim — QSC-02 active Zone/Floor context integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-qsc02-active-context-integrity-20260813-2132`
- Registered: `2026-08-13T21:32:00+07:00`
- Baseline main SHA: `b505ebe447b1bb5955be5003e3a914a2d1749a62`
- Priority: `QSC-02 / P2`

## Confirmed gap

Current `ModelHealthService` already emits six project working-context findings for active Zone/Floor selection: `INVALID_ACTIVE_ZONE`, `AMBIGUOUS_ACTIVE_ZONE`, `ACTIVE_ZONE_NON_CANONICAL`, `INVALID_ACTIVE_FLOOR`, `AMBIGUOUS_ACTIVE_FLOOR`, and `ACTIVE_FLOOR_NON_CANONICAL`. QSC-01A supplies the declarative profile contract, while the completed semantic-readiness QSC-02 family covers element family/floor/zone/material/dimension findings and the completed host/opening family covers host findings. No QSC profile currently owns these six active-context codes.

## Reserved scope

- new `src/QS3D.Core/Diagnostics/QsActiveContextIntegrityRuleFamily.cs`
- new `tests/QS3D.Core.SmokeTests/QsActiveContextIntegrityRuleFamilySmoke.cs`
- this claim file

## Intended bounded change

- define a deterministic active-context profile over the six existing project-level Zone/Floor health codes with matching severities and human explanations;
- focused smoke creates real missing, ambiguous, and non-canonical active Zone/Floor states and resolves the actual `ModelHealthService` findings through the QSC profile;
- keep element semantic-readiness, host/opening, and unrelated health codes unmapped.

## Excluded scope

- no edits to `ModelHealthService`, `ModelHealthIssue`, `QsRuleProfile`, Zone/Floor mutation semantics, ProjectState, persistence/MAP, existing QSC families, QSC-03 autofix, UI, reports, native BricsCAD, release qualification, or cross-repo work;
- no GitHub Actions, force-push, or unexecuted managed/native PASS claim.

## Completion condition

Claim-only first; refresh/recheck overlap; publish profile + focused smoke; reconcile current `main`; read back exact remote files; close `COMPLETED` with actual validation boundaries recorded.
