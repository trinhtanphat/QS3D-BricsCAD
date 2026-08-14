# Agent work claim — Rebar export validation parity

- Agent: `chatgpt-web-gpt56sol-rebar-export-validation-parity`
- Date: 2026-08-14
- Status: `ACTIVE`
- Baseline main SHA: `fd74a3a030f03ad1c3192006c3eb77e2584c1775`
- Implementation branch: `agent/chatgpt-web-gpt56sol/rebar-export-validation-parity-20260814`
- Planned integration branch: `integration/chatgpt-web-gpt56sol-rebar-export-validation-parity-20260814`
- Priority: Core export correctness

## Reserved scope

Close one confirmed BBS export-contract mismatch: `RebarCsvExporter` requires `CuttingLengthM`, `TotalLengthM`, and `UnitWeightKgM` to be finite and strictly greater than zero, while `XlsxRebarScheduleExporter` currently routes those same fields through its non-negative validator and therefore accepts zero.

The CSV exporter is the read-only parity oracle in this lane. The implementation changes only XLSX validation and focused existing BBS smoke coverage.

## Expected surfaces

- `src/QS3D.Core/Export/XlsxRebarScheduleExporter.cs` — route `CuttingLengthM`, `TotalLengthM`, and `UnitWeightKgM` through the existing positive numeric validator while preserving non-negative policy for `NetWeightKg`, `WastePercent`, and `TotalWeightKg`.
- `tests/QS3D.Core.SmokeTests/BbsRegressionSmoke.cs` — add deterministic CSV ↔ XLSX zero-boundary parity regression for the three positive-only physical fields.
- this claim file for coordination/closeout evidence.

## Read-only reference surface

- `src/QS3D.Core/Export/RebarCsvExporter.cs` — existing strict-positive validation is the parity contract; no source edit is planned.

## Explicit non-scope

- No changes to rebar schedule generation, notation parsing, fabrication qualification, CSV formatting/escaping, XLSX package structure, worksheet row/cell limits, quantity/BQ exporters, schedule exporters outside BBS, UI/native adapters, release/CI/signing, or LOCAL_ONLY BricsCAD qualification.
- No manual GitHub Actions dispatch/rerun/cancel.
- No implementation merge to `main` without explicit integration authorization.

## Evidence before registration

At baseline `fd74a3a030f03ad1c3192006c3eb77e2584c1775`, `RebarCsvExporter.ValidateRow` calls its strict-positive helper for `DiameterMm`, `CuttingLengthM`, `TotalLengthM`, and `UnitWeightKgM`; the helper rejects non-finite values and values `<= 0`. `XlsxRebarScheduleExporter.SnapshotRows` already applies the strict-positive helper to diameter and quantity, but applies the non-negative helper to `CuttingLengthM`, `TotalLengthM`, and `UnitWeightKgM`, admitting zero for those fields.

The existing registered `BbsRegressionSmoke` already exercises both Rebar CSV and XLSX exporters, so no smoke registration surface is required.

No matching open PR or recent commit was found for this exact parity lane, and the active QuantityRule XML persistability lane reserves unrelated rule-engine/persistence smoke surfaces.

## Validation plan

- verify this claim is visible on refreshed `main` and capture its post-claim SHA before source work;
- create the implementation branch from that exact post-claim SHA;
- make the smallest XLSX validator-routing change;
- extend `BbsRegressionSmoke` so each affected zero-valued field is rejected by both CSV and XLSX before destination mutation;
- run the Core smoke harness and applicable repository preflight/source guards when executable in the available environment;
- publish the implementation branch and PR/integration handoff without landing implementation on `main` unless separately authorized;
- continue read-only audit of quantity/BQ/schedule export contracts after the rebar lane is ready for integration.

## Completion condition

Claim-first reservation, isolated source + regression, verification evidence, fresh-main reconciliation, authorized integration/readback, and truthful CI/native boundaries are recorded; then status becomes `COMPLETED`.
