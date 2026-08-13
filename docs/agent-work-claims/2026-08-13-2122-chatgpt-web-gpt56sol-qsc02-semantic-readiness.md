# Work claim — QSC-02 semantic readiness rule family

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-qsc02-semantic-readiness-20260813-2122`
- Registered: `2026-08-13T21:22:00+07:00`
- Baseline main SHA: `b88c423fe00b6beea1a67bcf25df11faa7c582fe`
- Priority: `QSC-02 / P2`

## Confirmed gap

QSC-01A now provides a deterministic declarative `QsRuleProfile` over existing `ModelHealthIssue` codes. Current Semantic Health already emits stable family/floor/zone/material/dimension readiness findings, but the repository has no configured high-value QSC rule family for those existing findings. This lane adds profile metadata only and proves resolution against real health findings; it does not duplicate health predicates.

## Reserved scope

- new `src/QS3D.Core/Diagnostics/QsSemanticReadinessRuleFamily.cs`
- new `tests/QS3D.Core.SmokeTests/QsSemanticReadinessRuleFamilySmoke.cs`
- this claim file

## Intended bounded change

- define one deterministic semantic-readiness profile mapping existing family/floor/zone/material/dimension health codes to stable QSC rule ids, matching severities, and human explanations;
- keep affected semantic identity/evidence on the original `ModelHealthIssue`; profile resolution remains code-only through the QSC-01A contract;
- focused smoke creates real malformed semantic state, runs existing `ModelHealthService`, and proves emitted findings resolve to the expected declarative rules without re-running validation logic;
- unmapped health codes remain unmapped.

## Excluded scope

- no edits to `ModelHealthService`, `ModelHealthIssue`, `QsRuleProfile`, health predicates, project state, persistence, mapping, autofix, UI, native BricsCAD, or cross-repo platform work;
- no QSC-03 autofix;
- no GitHub Actions, force-push, or unexecuted managed/native PASS claim.

## Completion condition

Claim-only first; refresh/recheck QSC overlap; publish profile + focused smoke; reconcile current `main`; remote-readback exact files; close `COMPLETED` with only validation actually performed recorded.