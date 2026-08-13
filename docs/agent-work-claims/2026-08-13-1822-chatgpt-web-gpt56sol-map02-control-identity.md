# Work claim — MAP-02 coverage control-character identity integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-map02-control-identity-20260813-1822`
- Registered UTC: `2026-08-13T11:22:00Z`
- Baseline main SHA: `f914469f95706bec4561bf93271017c94653a558`
- Priority: `MAP-02 / P0-P1 hardening` — fail closed on non-canonical element identity before quantity/work-item coverage projection

## Confirmed defect

`MeasurementWorkItemCoverageEvaluator` snapshots project element identities through `RequireCanonicalIdentity()`, but that helper currently rejects only blank text and leading/trailing whitespace. `ProjectElement` can exist in memory with an ID containing a control character, and the coverage evaluator can therefore return a seemingly valid finding for a non-canonical identity. This is inconsistent with MAP-01 mapping identifiers and MeasurementTrace canonical text, both of which reject control characters, and with QSDB XML publication which cannot safely represent arbitrary control characters.

The existing MAP-02A claim promised fail-closed handling for non-canonical element identity and is now `COMPLETED`; its smoke covers duplicate/null/non-finite/padded/undefined-category corruption but does not cover control-character element IDs.

## Reserved files

- `src/QS3D.Core/Mapping/MeasurementWorkItemCoverage.cs`
- `tests/QS3D.Core.SmokeTests/MeasurementWorkItemCoverageSmoke.cs`
- this claim file

## Scope

- Extend the existing canonical-identity helper to reject any control character with `InvalidOperationException`.
- Add one focused regression proving an element ID containing a control character fails closed before coverage is returned.
- Preserve all mapped/unmapped/stale/missing semantics, ordering, mapping resolution and quantity values for valid identities.
- No changes to `ProjectElement`, MAP-01 mapping catalog, ProjectState, QSDB/schema/migration, MeasurementTrace/Snapshot/Delta/REV-03, reports/UI, rates/cost, geometry or BricsCAD/native surfaces.

## Initial overlap check

- MAP-02A is `COMPLETED` and no longer reserves its evaluator/smoke files.
- REV-03A remains `ACTIVE` but reserves new Measurement Snapshot delta-reason files only; this lane does not touch Revision/Measurement surfaces.
- Recent Quantity Summary/Recognition/NETLOAD/runtime-diagnostics claims are UI/native/preflight bounded and do not reserve either MAP file here.
- Targeted current history search found no coverage/control-character fix or competing MAP-02 hardening claim.

## Validation plan

- Re-fetch current `main` after this claim-only commit and compare any intervening changes for overlap before source write.
- Keep production diff to the existing identity helper only and regression diff to `CorruptProjectStateFailsClosed()`.
- Re-fetch exact implementation files and inspect commit diffs after push.
- Do not dispatch GitHub Actions and do not claim `.NET`/native PASS without execution.

## Completion condition

Current `main` rejects control-character element IDs before MAP coverage projection, the focused smoke regression is committed, concurrent work is reconciled without force-push/overwrite, remote readback confirms the landed change, and this claim is closed `COMPLETED` with exact pushed SHAs and truthful validation status.
