# Work claim — CST-01A deterministic RateBook core contract

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-cst01a-ratebook-core-20260813-1446`
- Registered: `2026-08-13T14:46:00+07:00`
- Baseline main SHA: `30fb30a7d91ec95f3a188ddd5cbc6cd792daeb5d`
- Priority: `CST-01 / P1` — establish the smallest deterministic commercial-rate identity/lookup foundation on top of the completed measurement/snapshot work without mixing cost assumptions into geometry or quantity engines

## Confirmed gap

The current workstream calls for a minimal `RateBook` / `RateItem` / `CostCode` foundation before `EstimateLine`. Current `main` contains no Cost/Estimate Core directory and repository source/history searches find no `RateBook`, `RateItem`, `CostCode`, `CST-01` or estimate-domain implementation. Existing Mapping/Measurement/Revision foundations are present, so this commercial contract can be added without inventing temporary quantity infrastructure.

## Reserved scope

Add one pure-Core in-memory rate catalog contract with deterministic, fail-closed identity and as-of lookup semantics:

- canonical `CostCode` value identity;
- immutable `RateItem` carrying item id, cost code, unit, currency, non-negative unit rate, effective UTC timestamp and explicit version token;
- immutable/detached `RateBook` snapshot with deterministic ordering;
- case-insensitive duplicate rate-item ids fail visibly;
- ambiguous natural keys `(cost code, unit, currency, effective timestamp)` fail visibly instead of depending on input order;
- lookup by `(cost code, unit, currency, as-of UTC)` returns the latest eligible effective rate or an explicit unmatched result;
- lookup does not perform unit conversion, currency conversion, quantity math, waste or estimating arithmetic.

Canonicality policy for this sub-lane:

- identifiers/tokens reject blank, surrounding whitespace and control characters;
- unit tokens are canonical lower-case;
- currency tokens are exactly three ASCII upper-case letters;
- UTC-effective timestamps and UTC lookup timestamps are required;
- `decimal` unit rate must be non-negative; zero is allowed explicitly.

## Expected surfaces

- new `src/QS3D.Core/Cost/RateBook.cs` — `CostCode`, `RateItem`, resolution and `RateBook` contract only;
- new `tests/QS3D.Core.SmokeTests/RateBookSmoke.cs` — deterministic ordering/lookup/duplicate/canonicality/snapshot regression;
- new `tests/QS3D.Core.SmokeTests/RateBookRegistration.cs` — ModuleInitializer registration;
- this claim file.

## Excluded scope

- No persistence/schema/serialization in CST-01A; compatibility persistence remains a separate follow-on lane.
- No `EstimateLine`, waste/commercial adjustment, quantity snapshot binding, cost delta, BOQ/BQ renderer or XLSX/DWG output.
- No unit conversion or FX logic and no remote rate service.
- No changes to geometry, semantic quantities, MeasurementTrace/Snapshot/Delta, Mapping coverage, REV-03A, Workspace UI, Curtain or LOCAL/native qualification.
- No GitHub Actions and no BricsCAD native PASS claim.

## Validation plan

- Re-fetch current `main` after this claim-only commit and recheck new Cost/Rate claims before source changes.
- Smoke covers canonical construction, deterministic ordering independent of input order/culture, latest-effective as-of selection, explicit unmatched state, duplicate id, ambiguous natural key, invalid UTC/currency/unit/token/rate inputs and source-list mutation isolation.
- Re-fetch exact source/test blobs from remote before closeout.
- Executable managed smoke/build remains `NOT_RUN` unless a real .NET execution path becomes available; source inspection is not reported as PASS.

## Coordination

- REV-01/02 snapshot foundation and MTR foundations are completed prerequisites; this lane does not edit them.
- REV-03A remains treated as reserved and excluded.
- Current Workspace, Curtain and LOCAL lanes are unrelated and excluded.
- MAP-03 is intentionally not claimed here because recent UI activity makes a pure-Core CST lane safer for parallel work.

## Completion condition

A claim-first deterministic in-memory rate identity/catalog contract plus focused auto-registered smoke is present on current `main`, no quantity/estimate/FX arithmetic is duplicated, remote source/test are re-fetched, and this claim is closed with exact pushed SHAs plus validation actually executed.
