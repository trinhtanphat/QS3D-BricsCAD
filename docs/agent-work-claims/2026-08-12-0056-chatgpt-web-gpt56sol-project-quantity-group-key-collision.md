# Work claim — Project quantity report collision-free grouping identity

- Status: `COMPLETED — SUPERSEDED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:56:00+07:00`
- Closed: `2026-08-12T07:18:00+07:00`
- Baseline main SHA: `6432233e718643757befcec600286332e16a373e`
- Priority: evidence-driven remote-safe reporting integrity

## Confirmed defect

Grouped `ProjectQuantityReportBuilder` rows used an unescaped U+001F-delimited key over floor/zone/category/family/material/density tokens. Accepted identity/material text could contain U+001F internally, so distinct grouping tuples could serialize to the same dictionary key and merge count/quantity/provenance incorrectly.

## Reserved scope

Replace only the grouped-report composite identity with deterministic collision-free token encoding while preserving detail-mode identity, case-insensitive grouping, first-seen ordering, material/density semantics, quantities, notes and provenance.

## Expected surfaces

- `src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs`
- focused Core smoke coverage
- this claim file

## Excluded scope

- No legacy `QuantityReportBuilder` changes.
- No quantity formula/settings/business-rule or material catalog changes.
- No detail-mode grouping behavior changes.
- No XLSX/UI/native BricsCAD changes.
- No new character restrictions.
- No GitHub Actions dispatch.

## Close-out / supersession

Before any product edit under this claim, current `main` was re-fetched and already contained the intended collision-free implementation from another completed lane:

- concurrent completed claim: `docs/agent-work-claims/2026-08-12-0058-chatgpt-web-gpt56sol-project-quantity-report-group-key.md`;
- implementation commit: `64fb83263c56560191e25738c8fef20a77f58700`;
- focused regression commit: `a3c794205790ed43cc5a2e1dc9144bdf667ff345`;
- current source uses `CanonicalGroupKey(...)` with length-prefixed grouping tokens and keeps detail mode unchanged.

No product or test commit was created from this claim. This avoids duplicate work and preserves the concurrent agent's already-integrated fix.

## Validation actually performed

- Re-fetched current `src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs` and verified grouped mode now calls `CanonicalGroupKey(floorId, zoneId, category, familyId, material, DensityKey(...))`.
- Re-fetched the concurrent completed claim and verified it records the exact implementation/regression commits and validation scope.
- No repository .NET tests were executed in this hosted session for this superseded lane.
- No GitHub Actions were dispatched or rerun.
- No BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

Satisfied by safe supersession: the defect is already fixed and regression-covered on current `main`, this agent produced no duplicate product change, and the claim is released.