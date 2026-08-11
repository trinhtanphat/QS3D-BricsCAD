# Work claim — Quantity settings matrix diagnostics

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-matrix-diagnostics`
- Registered: `2026-08-11T22:11:00+07:00`
- Baseline main SHA observed: `e2253598e044b845f61cc88bf75cca4524426551`
- Priority: P1 — continue Setup & Rules hardening with deterministic diagnostics for imported/edited category + directed-intersection matrices without repairing or guessing missing rules.

## Reserved scope

- `src/QS3D.Core/Reporting/QuantityCalculationMatrixDiagnostics.cs` (new)
- `tests/QS3D.Core.SmokeTests/QuantityCalculationMatrixDiagnosticsSmoke.cs` (new)
- `tests/QS3D.Core.SmokeTests/QuantityCalculationMatrixDiagnosticsSmokeRegistration.cs` (new)
- `scripts/preflight-quantity-calculation-matrix-diagnostics.py` (new)
- this claim file for close-out

## Contract

- Analyze a defensive validated clone of `QuantityCalculationSettings`; never mutate caller state.
- Build the deterministic sorted union of every category-rule code plus every intersection source/target code, including unknown imported integer codes.
- Report category codes referenced by intersection rules but missing from `CategoryRules`.
- Report category-rule codes that are never referenced by any intersection rule.
- Report every missing directed pair across the observed code universe as exact `SourceCode -> TargetCode` diagnostics. A -> B and B -> A remain distinct.
- Expose matrix completeness/count summaries only; do not synthesize missing pairs, infer category aliases, alter settings, or assign engineering meaning to unknown codes.

## Excluded scope

- No edits to `QuantityCalculationSettings.cs` while a concurrent clone-validation claim is ACTIVE.
- No Quantity Settings WPF/store, report builder, StructuralRegenerator, CAD/BREP, Ribbon/updater/release/GitHub Actions changes.

## Validation plan

- Core smoke covers a complete matrix, missing directed reverse pair, dangling intersection-only code, unused category code, unknown imported codes, stable sorted output and caller non-mutation.
- Focused static preflight forbids rule synthesis, settings mutation, enum casts, report/CAD coupling and nondeterministic set enumeration.
- Re-fetch final files from latest `main` and preserve concurrent winners.

## Completion condition

- Settings/import/UI callers can diagnose matrix integrity precisely without silently changing the user's rules or requiring native category mappings.
