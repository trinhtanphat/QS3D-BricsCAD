# Work claim — CST-01A deterministic RateBook core contract

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-cst01a-ratebook-core-20260813-1446`
- Registered: `2026-08-13T14:46:00+07:00`
- Baseline main SHA: `30fb30a7d91ec95f3a188ddd5cbc6cd792daeb5d`
- Priority: `CST-01 / P1` — establish the smallest deterministic commercial-rate identity/lookup foundation without mixing cost assumptions into geometry or quantity engines

## Confirmed gap

The workstream called for a minimal `RateBook` / `RateItem` / `CostCode` foundation before `EstimateLine`. Baseline `main` had no Cost/Estimate Core directory and source/history searches found no corresponding rate-domain implementation.

## Completed scope

Added a pure-Core in-memory rate catalog contract with deterministic, fail-closed identity and as-of lookup semantics:

- canonical case-insensitive `CostCode` value identity;
- immutable `RateItem` with item id, cost code, canonical lower-case unit, three-letter upper-case currency, non-negative decimal unit rate, UTC effective timestamp and explicit version token;
- detached/read-only `RateBook` snapshot with deterministic ordering;
- case-insensitive duplicate rate-item ids fail visibly;
- ambiguous natural keys `(cost code, unit, currency, effective timestamp)` fail visibly;
- lookup by `(cost code, unit, currency, as-of UTC)` returns the latest eligible rate or explicit `Unmatched` resolution;
- no unit conversion, currency conversion, quantity math, waste or estimate arithmetic was added.

## Surfaces

- `src/QS3D.Core/Cost/RateBook.cs`
- `tests/QS3D.Core.SmokeTests/RateBookSmoke.cs`
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`
- this claim file

The initially planned standalone `RateBookRegistration.cs` creation was rejected by the connector before any write/commit occurred. Claim scope was refined first, then the existing aggregate registration surface was used.

## Implementation

- Claim-only registration: `3e8732f20097194a52f7f781db0d99f473c11a20`.
- Core contract: `3e6f9e0482130b6e5caa525cc2e12afb17088d0f`.
- Focused smoke: `7b1e992dd823d67dfff83368c59b75792c92dffa`.
- Registration-surface claim refinement: `c2ff49d1595afb44ec35dd9b206c96d5ea47f914`.
- Aggregate smoke registration: `c3768490ba9ab057daa618f328881b6370cdadf6`.

## Validation actually executed

- Re-fetched current `main` after claim publication and after implementation work.
- Re-fetched remote `RateBook.cs`; verified rate identity canonicality, duplicate/natural-key rejection, deterministic ordering, UTC-only as-of selection and explicit unmatched result are present.
- Re-fetched remote `RateBookSmoke.cs`; verified coverage for culture/input-order independence, latest-effective selection, unmatched state, source-list isolation/read-only view, duplicate id, ambiguous natural key and malformed token/unit/currency/rate/time inputs.
- Re-fetched `SmokeTestRegistration.cs`; verified `RateBookSmoke.Run()` is registered in the aggregate runner.
- Repository commit search for `cost` showed no competing Cost/Rate implementation during this lane beyond these commits.
- Executable managed smoke/build: `NOT_RUN`; this environment has no `dotnet`, `csc`, `msbuild` or `mcs`, and source inspection is not reported as PASS.
- GitHub Actions: not dispatched by this lane.
- BricsCAD native qualification: `NOT_APPLICABLE` to this pure Core contract; no native PASS claimed.

## Excluded / remaining follow-ons

- Persistence/schema/serialization compatibility for rates remains a separate CST-01 follow-on.
- `EstimateLine`, waste/commercial adjustment, measurement-snapshot binding, revision cost delta, BOQ/BQ/XLSX/DWG projections and FX remain unimplemented by this lane.
- REV-03A, Workspace, Curtain and LOCAL/native work remained excluded.

## Completion condition

Satisfied: deterministic in-memory rate identity/catalog semantics and focused registered regression are present on current `main`, no quantity/estimate/FX arithmetic is duplicated, and validation is recorded only at the level actually executed.
