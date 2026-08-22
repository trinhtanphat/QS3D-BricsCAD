# Work claim — Quantity XLSX non-negative publication preflight

- Status: `COMPLETED`
- Agent: `codex-gpt5-audit-blt-notes-latest` (`/root/audit_blt_notes_latest`)
- Registered: `2026-08-12T09:32:51+07:00`
- Completed: `2026-08-12T09:51:28+07:00`
- Baseline main SHA: `340c88459f312710a6a794ffa8362d19f879c8af`
- Priority: `P1` — prevent public BQ/ED2 XLSX APIs from publishing finite negative counts or physical quantity magnitudes that the canonical reporting contract rejects.

## Reserved scope

Harden the standard BQ and ED2 XLSX publication boundary so negative row counts and finite negative physical quantities fail closed before destination-directory creation, temporary-package creation or destination replacement. ED2 must reject a matched negative `CHI_TIET` / `TONG_HOP` pair instead of accepting it merely because its aggregate arithmetic agrees. Preserve zero/positive values, exact ED2 aggregation, optional density/mass semantics and atomic publication.

## Expected surfaces

- `src/QS3D.Core/Export/XlsxQuantityExporter.cs`
- `tests/QS3D.Core.SmokeTests/XlsxQuantityStandardNumericPreflightSmoke.cs`
- `tests/QS3D.Core.SmokeTests/Ed2NumericParitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs` for additive, exactly-once standard-smoke registration
- one focused static preflight only if existing deterministic smoke coverage cannot guard the source ordering sufficiently
- this claim file for close-out

## Excluded scope

- No changes to quantity calculation, grouping, deduction formulas, density derivation, mass derivation, report-row mutability or XLSX schema/formatting.
- No changes to Excel Handle reverse lookup, B4D recognition, CAD Locate, BricsCAD adapters/UI, persistence, revisions or native geometry.
- No private/customer fixture access, licensed BricsCAD runtime qualification, package/release work or GitHub Actions dispatch.

## Validation plan

- Add standard-export smoke coverage for negative Count and every emitted physical-quantity field, with refusal before filesystem mutation; keep zero and positive export coverage.
- Add ED2 smoke coverage for matching negative detail/summary physical values, preserving an existing destination and leaving no temporary package; keep current density/mass/null and positive parity behavior.
- Run focused Core smoke coverage, Core Release build/full smoke, relevant static preflight(s), and aggregate preflight within the source-only boundary.

## Coordination

All current `ACTIVE` / `BLOCKED` claims were inspected at the baseline. None reserves `XlsxQuantityExporter`, `XlsxQuantityStandardNumericPreflightSmoke` or `Ed2NumericParitySmoke`. The completed Core reporting non-negative claim explicitly excluded XLSX exporters; active quantity-revision and local BQ preflight claims reserve disjoint revision/test-only and adapter/static surfaces. After review exposed the standard smoke's legacy self-registration, the central registration file was added as an additive shared surface and the smoke's module initializer removed so the new coverage runs exactly once; concurrent registration entries remain untouched.

## Completion condition

Satisfied by implementation PR [#720](https://github.com/trinhtanphat/QS3D-BricsCAD/pull/720), source commit `780ad1e45634dac63fa1b4a3b3a1a7b0d432a3ed`, integrated on `main` as `e9454e2566dfaabf00a6389c3f219ef46fe3f683`.

## Validation evidence

- PASS: focused isolated execution of `Ed2NumericParitySmoke.Run()` and `XlsxQuantityStandardNumericPreflightSmoke.Run()`.
- PASS: `py -3 scripts/preflight-ed2-numeric-parity.py`.
- PASS: `py -3 scripts/preflight-ed2-excel-roundtrip.py` after the separately owned gate update landed on current main.
- PASS: `py -3 scripts/preflight-smoke-registration.py`.
- PASS: `py -3 scripts/preflight-command-xlsx-export-freshness.py`.
- PASS: `py -3 scripts/preflight.py` and `git diff --check`.
- BLOCKED by current-main, out-of-scope compile debt: strict Core Release build/full smoke report `CS8602` at `ReportingProjectIdentityGuard.cs:58` and `SemanticSelectionInspector.cs:177`; a warnings-relaxed full smoke additionally reports pre-existing stale `ProjectElement` constructor call sites and `ElementCategory.Wall` test references. No reserved file in those lanes was changed.
- Aggregate preflight was executed and remained red on multiple unrelated current-main static gates; focused XLSX/ED2 gates above passed.
- No GitHub Actions, private/customer fixture, licensed BricsCAD runtime, package or release operation was run.
