# Work claim — QSC-02 host/opening integrity rule family

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-qsc02-host-opening-integrity-20260813-2126`
- Registered: `2026-08-13T21:26:00+07:00`
- Baseline main SHA: `6aaa1d208376df1b067cff15b4f763061a7116b9`
- Priority: `QSC-02 / P2`

## Confirmed gap

QSC-01A provides the immutable deterministic `QsRuleProfile` contract, and current `ModelHealthService` already emits stable host/opening integrity findings (`MISSING_HOST`, `AMBIGUOUS_HOST`, `INVALID_HOST`, `HOST_REFERENCE_NON_CANONICAL`, `INVALID_HOST_CATEGORY`). The repository has no configured QSC rule family for those existing findings. This lane adds declarative profile metadata only; it does not duplicate or alter host validation predicates.

A parallel QSC-02 semantic-readiness claim currently owns family/floor/zone/material/dimension rule metadata. This claim intentionally excludes those codes and reserves only host/opening integrity metadata.

## Reserved scope

- new `src/QS3D.Core/Diagnostics/QsHostOpeningIntegrityRuleFamily.cs`
- new `tests/QS3D.Core.SmokeTests/QsHostOpeningIntegrityRuleFamilySmoke.cs`
- this claim file

## Intended bounded change

- define one deterministic host/opening profile mapping the five existing host health codes to stable QSC rule ids, matching current health severities and human explanations;
- keep affected element identity and runtime evidence on the original `ModelHealthIssue`; resolution remains code-only through QSC-01A;
- focused smoke runs existing `ModelHealthService` against malformed Door/WallOpening host states and proves emitted findings resolve to the expected declarative rules;
- unrelated health findings remain explicitly unmapped.

## Excluded scope

- no edits to `ModelHealthService`, `ModelHealthIssue`, `QsRuleProfile`, host/opening business logic, ProjectState, persistence/MAP, family/floor/zone/material/dimension QSC metadata, QSC-03 autofix, UI, reports, native BricsCAD, release qualification, or cross-repo platform work;
- no GitHub Actions, force-push, or unexecuted managed/native PASS claim.

## Completion condition

Claim-only first; refresh/recheck overlap; publish profile + focused smoke; reconcile current `main`; read back exact remote files; close `COMPLETED` with only validation actually performed recorded.
