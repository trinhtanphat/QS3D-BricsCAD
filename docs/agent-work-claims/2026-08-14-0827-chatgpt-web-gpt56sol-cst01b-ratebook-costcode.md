# Work claim — CST-01B canonical matched CostCode identity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-cst01b-ratebook-costcode-20260814-0827`
- Registered: `2026-08-14T08:27:30+07:00`
- Baseline main SHA: `28e19a066f89b67b12ff9bdbdfef3bbceb5568e3`
- Priority: `CST-01 / P1 deterministic identity hardening` — matched RateBook resolutions must expose the selected canonical rate identity rather than a caller-supplied case alias

## Confirmed source gap

`CostCode` identity compares case-insensitively and `RateBook.Resolve(...)` intentionally accepts case aliases such as `conc` for a catalog item stored as `CONC`. The current matched path nevertheless builds `RateBookResolution` with the caller's `CostCode` object. As a result, two semantically identical lookups can resolve the same `RateItem` while exposing different observable `RateBookResolution.CostCode.Value` casing.

The existing deterministic RateBook smoke already exercises a lowercase `conc` lookup against canonical `CONC`, but does not assert the resolution identity. This lane pins that mismatch without changing lookup matching, rate selection, unmatched semantics, persistence, estimate math, or currency/unit policy.

## Reserved scope

- For a matched `RateBook.Resolve(...)`, make `RateBookResolution.CostCode` come from the selected `RateItem.CostCode` so the matched result exposes the selected catalog identity.
- Keep unmatched results tied to the requested `CostCode`, because no catalog item exists to supply a selected identity.
- Add one focused regression assertion proving a lowercase alias lookup returns the canonical selected item CostCode.

## Expected surfaces

- `src/QS3D.Core/Cost/RateBook.cs`
- `tests/QS3D.Core.SmokeTests/RateBookSmoke.cs`
- this claim file

## Excluded scope

- No changes to RateBook matching rules, effective-date selection, duplicate/ambiguity rules, unit/currency canonicality, signed-zero handling, RateItem ordering, persistence, EstimateLine, estimate freshness, revision cost impact, report/BQ, IFC, UI, BricsCAD adapters, release tooling or native qualification.
- No new cost/rate model and no change to unmatched resolution semantics.
- No GitHub Actions dispatch and no native BricsCAD claim.

## Validation plan

- Publish this claim alone to `main`, refresh and verify it remains on current lineage, then recheck concurrent claims/commits for the two exact reserved files before implementation.
- Apply the minimal matched-result identity fix and extend the already registered `RateBookSmoke` deterministic lookup case.
- Re-fetch the pushed implementation and compare the exact diff from its parent.
- Managed smoke execution is `NOT_RUN` unless a real .NET execution path becomes available; do not claim PASS from source inspection.

## Coordination

- CST-01A RateBook core is completed and is consumed as-is except for this narrow matched identity correction.
- The just-completed estimate-input-freshness lane, current Family-category lanes, IFC-02E, MAP-03C, V25 release/update and LOCAL_ONLY qualification work are explicitly outside this claim.
- Targeted RateBook/CostCode history shows no current overlapping RateBook claim after CST-01A completion.
- Concurrent commits through `28e19a066f89b67b12ff9bdbdfef3bbceb5568e3` were compared before registration and did not touch either reserved production/test file.

## Completion condition

Current `main` contains the minimal matched CostCode canonicality correction plus focused regression coverage, the pushed source is re-fetched/verified, validation is reported truthfully, and this claim is updated to `COMPLETED` with the exact implementation SHA.
