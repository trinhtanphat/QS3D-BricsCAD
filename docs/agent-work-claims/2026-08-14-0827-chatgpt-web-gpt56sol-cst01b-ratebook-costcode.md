# Work claim — CST-01B canonical matched CostCode identity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-cst01b-ratebook-costcode-20260814-0827`
- Registered: `2026-08-14T08:27:30+07:00`
- Last updated: `2026-08-14T08:32:18+07:00`
- Baseline main SHA: `28e19a066f89b67b12ff9bdbdfef3bbceb5568e3`
- Priority: `CST-01 / P1 deterministic identity hardening` — matched RateBook resolutions must expose the selected canonical rate identity rather than a caller-supplied case alias

## Confirmed source gap

`CostCode` identity compares case-insensitively and `RateBook.Resolve(...)` intentionally accepts case aliases such as `conc` for a catalog item stored as `CONC`. The matched path nevertheless built `RateBookResolution` with the caller's `CostCode` object. As a result, two semantically identical lookups could resolve the same `RateItem` while exposing different observable `RateBookResolution.CostCode.Value` casing.

The existing deterministic RateBook smoke already exercised a lowercase `conc` lookup against canonical `CONC`, but did not assert the resolution identity.

## Completed implementation

- Source commit: `06df9150d99dcf1a888a74812ba6a5de27cfb3c8` (`fix(cost): canonicalize matched RateBook CostCode`).
- Regression commit: `a4058f12400a2aaa9a43a2902b4c30a5715ab5ee` (`test(cost): guard matched RateBook CostCode identity`).
- Matched `RateBook.Resolve(...)` now constructs the resolution with `match.CostCode`, preserving the identity of the selected catalog item.
- Unmatched results still retain the requested `CostCode`, because there is no selected catalog item in that state.
- Existing matching, effective-date selection, duplicate/ambiguity handling, unit/currency policy, signed-zero behavior and ordering are unchanged.

## Files changed

- `src/QS3D.Core/Cost/RateBook.cs`
- `tests/QS3D.Core.SmokeTests/RateBookSmoke.cs`
- this claim file

## Regression coverage

The already registered `RateBookSmoke.DeterministicOrderingAndLatestLookup()` case resolves lowercase alias `conc` against the catalog item stored as `CONC` and now asserts `RateBookResolution.CostCode.Value == "CONC"`. The assertion fails against the pre-fix implementation and pins the canonical matched identity behavior.

## Validation actually performed

- Claim-only commit published to `main`: `21730dfb89a7c943d0b95ca0609458979781f82e`.
- Refreshed `main` and checked concurrent commits before source/test writes; no concurrent commit touched either reserved file.
- Re-fetched both implementation commits. Source diff is exactly the matched `CostCode` argument substitution; regression diff is exactly one assertion in the existing deterministic lookup case.
- Live `main` was re-fetched after source/test publication; `a4058f12400a2aaa9a43a2902b4c30a5715ab5ee` is on current lineage and was immediately followed only by unrelated semantic-selection hardening at the verification point `8e888ddf371aa7bbd8c7d34e1e1ea84dcb7fef66`.
- Local managed smoke execution: **NOT_RUN** — this container has no `dotnet`, `csc`, or `mcs` executable.
- GitHub Actions: **not dispatched**.
- BricsCAD/native qualification: **not executed** and no native PASS is claimed.
- Force-push: **not used**.

## Excluded scope preserved

No changes were made to persistence, EstimateLine, estimate freshness, revision cost impact, report/BQ, IFC, UI, BricsCAD adapters, release tooling or native qualification. No new rate/cost engine was introduced.

## Completion

CST-01B is complete and no longer reserves `RateBook.cs` or `RateBookSmoke.cs`. Later cost work should treat the selected `RateItem` identity as authoritative for matched resolution output while preserving explicit unmatched request identity.
