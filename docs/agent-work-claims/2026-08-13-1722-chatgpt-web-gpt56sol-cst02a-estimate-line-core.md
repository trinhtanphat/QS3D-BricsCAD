# Work claim — CST-02A frozen EstimateLine core

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-cst02a-estimate-line-core-20260813-1722`
- Registered: `2026-08-13T17:22:00+07:00`
- Baseline main SHA: `5b3a048206bf07d43bb81e53da0e419a7f67e233`
- Priority: `CST-02 / P1` — establish the smallest deterministic estimate-line contract from the completed REV-01 measurement snapshot and CST-01A rate domain

## Confirmed gap

The workstream requires `EstimateLine` to distinguish measured quantity, estimating quantity, waste/commercial adjustment, unit rate and final amount while remaining traceable to frozen measurement/rate inputs. At claim time `src/QS3D.Core/Cost/` contained only `RateBook.cs`; repository history/search contained no `EstimateLine` implementation or CST-02 commit. REV-01 `MeasurementSnapshot` and CST-01A `RateBook` were completed prerequisites.

## Implemented scope

Added a pure-Core immutable `EstimateLine` factory/contract consuming:

- a frozen `MeasurementSnapshot` plus exact canonical trace identity `(semantic identity, source identity, quantity key)`;
- a frozen/detached `RateBook`, `CostCode`, currency and UTC as-of timestamp;
- one explicit additive `commercialAdjustmentQuantity` in the measurement unit, with an explicit reason required for every non-zero adjustment.

The implemented contract now:

- selects the exact trace from the supplied snapshot and uses trace `NetValue` as measured quantity; no geometry/live quantity lookup occurs;
- resolves rate through `RateBook.Resolve(...)` using the trace unit, requested cost code/currency and UTC as-of time; unmatched rates fail visibly;
- converts measured quantity once to `decimal` for commercial arithmetic and fails on decimal overflow or non-zero-to-zero conversion underflow;
- computes `EstimatingQuantity = MeasuredQuantity + CommercialAdjustmentQuantity` with checked decimal arithmetic and rejects a result below zero;
- computes `FinalAmount = EstimatingQuantity * UnitRate` with checked decimal arithmetic;
- retains references to the immutable `MeasurementSnapshot`, exact `MeasurementTrace`, immutable `RateBook`, selected `RateItem` and rate as-of timestamp, so `RateBook.RateBookId` and all selected measurement/rate evidence remain traceable without duplicating them;
- preserves exact measurement unit matching; no unit conversion or FX conversion is introduced;
- allows zero adjustment with no reason, while validating any supplied reason for blank/padded/control-character state.

## Pushed commits

- Claim-only: `dc27e5878308b2705169cac3370c00f4cc236837` — `chore(agent): claim CST-02A EstimateLine core`.
- Core implementation: `8171d325e693f09ca500026148347169927c4680` — `feat(cost): add frozen EstimateLine core`.
- Focused smoke: `6a70f1f1334edacfbb8a819c35da535304307451` — `test(cost): cover frozen EstimateLine core`.
- Smoke registration: `3a3ff5abe7322858a0ba3fbf6ae2d0bc18ab174c` — `test(cost): register EstimateLine smoke`.

## Exact remote surfaces verified

- `src/QS3D.Core/Cost/EstimateLine.cs` — remote blob `5ebf321450951dac2d883700989855631a63e20e`.
- `tests/QS3D.Core.SmokeTests/EstimateLineSmoke.cs` — remote blob `5aa4c45b9995764ef2519d655345f6af54ce1124`.
- `tests/QS3D.Core.SmokeTests/EstimateLineRegistration.cs` — remote blob `d9548dd1ab08b7d7ba17a939d78d12a4ceb677d8`.

## Focused regression coverage committed

- exact snapshot trace binding retains the original immutable snapshot and selected trace;
- rate selection chooses the latest eligible effective rate and retains the original immutable RateBook / selected RateItem / as-of timestamp;
- measured quantity, positive adjustment, estimating quantity, unit rate and final amount remain distinct and reconcile (`10 + 1.5`, rate `120`, amount `1380`);
- zero adjustment preserves measured quantity and does not invent a reason;
- explicit negative commercial deduction is supported while leaving measured quantity unchanged;
- missing trace, unknown cost-code rate and pre-effective-date rate fail visibly;
- non-zero adjustment without reason, padded reason, over-deduction, padded line identity, noncanonical currency and non-UTC as-of fail visibly;
- a non-zero measurement smaller than decimal commercial precision is rejected rather than silently becoming zero;
- final amount overflow is rejected through checked decimal arithmetic.

## Excluded scope preserved

- No percentage/waste policy engine, markup/tax/discount/rounding policy, FX, unit conversion or remote rates.
- No persistence/schema/migration, Estimate/BQ collection, CST-03 revision cost impact or CST-04 renderer/export.
- No geometry, ProjectState, semantic entity or measurement-rule mutation.
- `MeasurementSnapshot`, `MeasurementTrace` and `RateBook` were not edited.
- No WPF/BricsCAD/native/local qualification and no GitHub Actions dispatch.

## Validation actually executed

- Re-read the CST-02 workstream acceptance on current main and confirmed prerequisites from current source/history before claim.
- Published claim alone; post-claim HEAD was a direct descendant and the immediate concurrent change was UI-only.
- Re-fetched the exact implementation, smoke and registration from remote current-main lineage after push.
- Compared claim commit to later current `main`; only this lane's three Cost/test files touched CST-02, while concurrent Basic Drawing/UI/native work remained non-overlapping.
- Performed static C# audit of exact trace selection, rate resolution, canonical input handling, checked decimal conversion/addition/multiplication, read-only prerequisite objects and failure semantics.
- Environment toolchain probe found no `dotnet`, `csc`, `mcs`, `msbuild` or `xbuild`; managed build and smoke execution are therefore `NOT_RUN`.
- GitHub Actions were not dispatched. BricsCAD V25/V26 runtime/native qualification was not run. No PASS is claimed for unexecuted gates.

## Coordination

- CST-01A and REV-01A remain completed prerequisites and were unmodified.
- CST-03/CST-04 remain future independent lanes and are not reserved by this completed claim.
- Concurrent MTR, diagnostic/UI, Basic Drawing, Curtain and LOCAL/native work remained outside this scope.

## Completion condition

Satisfied for CST-02A: a claim-first immutable EstimateLine core plus focused registered smoke is present on current `main`; commercial arithmetic is deterministic/checked and traceable to frozen measurement/rate inputs without geometry/rate assumptions leaking into semantic entities; exact remote blobs were verified; and all executable/native gates not actually run are explicitly `NOT_RUN` rather than PASS.
