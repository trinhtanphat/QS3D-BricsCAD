# Work claim — Quantity settings matrix diagnostics

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-matrix-diagnostics`
- Registered: `2026-08-11T22:11:00+07:00`
- Completed: `2026-08-11T22:16:00+07:00`
- Baseline main SHA observed: `e2253598e044b845f61cc88bf75cca4524426551`
- Claim commit: `88fed066dd68b06dec3b5eaea583696aae4cdaa9`
- Priority: P1 — continue Setup & Rules hardening with deterministic diagnostics for imported/edited category + directed-intersection matrices without repairing or guessing missing rules.

## Delivered scope

- `src/QS3D.Core/Reporting/QuantityCalculationMatrixDiagnostics.cs`
- `tests/QS3D.Core.SmokeTests/QuantityCalculationMatrixDiagnosticsSmoke.cs`
- `tests/QS3D.Core.SmokeTests/QuantityCalculationMatrixDiagnosticsSmokeRegistration.cs`
- `scripts/preflight-quantity-calculation-matrix-diagnostics.py`
- this claim file

## Implemented contract

- `QuantityCalculationMatrixDiagnostics.Analyze(...)` clones and validates caller settings before analysis, leaving caller collection order/state unchanged.
- Builds the deterministic ascending union of every `CategoryRules.Category` plus every intersection `Source`/`Target`, including unknown imported integer codes.
- Reports intersection-only category codes not represented by a category rule.
- Reports category-rule codes never referenced by any intersection rule.
- Reports every missing directed pair across the observed code universe in deterministic source-major / target-major order; A -> B and B -> A remain independent.
- Exposes existing/expected directed rule counts and `IsCompleteDirectedMatrix` without creating or modifying any rule.

## Product commits

- `2617eb4d66bc4db73be605dbcc35879ac341b8c8` — `feat(quantity): diagnose calculation rule matrix integrity`
- `9697783b47e89aaede1cccf02c48b6cfef4a7a29` — `test(quantity): cover rule matrix diagnostics`
- `4f2cf78d597d34402f208f9cd6207c6b9188babc` — `test(quantity): register matrix diagnostics smoke`
- `b1b22130e2715dd3639e2e18073144f17dfe8dc9` — `test(quantity): guard matrix diagnostics integrity`

## Validation evidence

- Re-fetched final Core diagnostic, smoke, registration and focused preflight from current `main` after concurrent repository movement; registered files remained intact.
- Smoke source covers complete matrices, one-way missing reverse pair isolation, intersection-only codes, unreferenced category rules, unknown imported codes, deterministic sorting and caller non-mutation.
- Focused preflight requires defensive clone/validation and deterministic ordering, while rejecting settings mutation, synthesized rules, enum/category inference, reverse fallback and report/CAD/ProjectState/AuditTrail coupling.
- One registration create initially received a GitHub 409 because `main` advanced concurrently; after re-reading current `main`, the target path was still absent and the exact registered file was committed successfully without overwriting concurrent work.
- No GitHub Actions were dispatched. This remote session source-reviewed the final files but does not claim execution of the smoke/preflight in a repository checkout or licensed BricsCAD runtime.

## Remaining boundary

- Diagnostics now tells callers exactly where an imported/edited matrix is incomplete or references category codes without category definitions. It deliberately does not auto-repair those gaps because doing so would create engineering semantics the user did not provide.
- Native CAD intersection measurement, face/contact classification, engulf behavior and multi-overlap precedence remain separate work.

## Completion

Reservation released. Settings/import/UI callers now have a deterministic read-only matrix integrity surface suitable for warnings, import review and future runtime diagnostics.
