# Work claim — Project quantity report collision-free grouping identity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:56:00+07:00`
- Baseline main SHA: `6432233e718643757befcec600286332e16a373e`
- Priority: evidence-driven remote-safe reporting integrity

## Confirmed defect

Grouped `ProjectQuantityReportBuilder` rows use an unescaped U+001F-delimited key over floor/zone/category/family/material/density tokens. Accepted floor/zone/material identifiers can contain U+001F internally, so distinct grouping tuples can serialize to the same dictionary key. For example `(floor=A<US>B, zone=C)` and `(floor=A, zone=B<US>C)` collide when the remaining tokens are equal, causing incorrect count/quantity/provenance merging.

## Reserved scope

Replace only the grouped-report composite identity with deterministic collision-free token encoding. Preserve detail-mode identity, case-insensitive grouping, first-seen ordering, material/density semantics, quantities, notes and provenance.

## Expected surfaces

- `src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs`
- `tests/QS3D.Core.SmokeTests/ProjectQuantityReportGroupKeyCollisionSmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectQuantityReportGroupKeyCollisionRegistration.cs`
- this claim file

## Excluded scope

- No legacy `QuantityReportBuilder` changes; its prior material-grouping claim is completed.
- No quantity formula/settings/business-rule or material catalog changes.
- No detail-mode grouping behavior changes.
- No XLSX/UI/native BricsCAD changes.
- No new character restrictions.
- No GitHub Actions dispatch.

## Validation plan

- Preserve grouping for identical floor/zone/category/family/material/density tuples.
- Prove `(A<US>B,C)` and `(A,B<US>C)` remain separate grouped BQ rows.
- Verify Count and representative LengthM totals remain independent.
- Use length-prefixed tokens, dedicated module initializer, target re-fetch before product write, exact diff review and ancestry verification.
- No .NET/V25/V26 runtime PASS will be claimed unless actually executed.

## Coordination

The completed legacy reporting material claim explicitly excluded `ProjectQuantityReportBuilder`. Recent claim search found no current reservation for this exact file/group-key boundary.

## Completion condition

Distinct accepted project quantity grouping tuples cannot alias through delimiter injection, focused regression source is on current `main`, concurrent work is preserved, and this claim is closed with exact commit SHAs.