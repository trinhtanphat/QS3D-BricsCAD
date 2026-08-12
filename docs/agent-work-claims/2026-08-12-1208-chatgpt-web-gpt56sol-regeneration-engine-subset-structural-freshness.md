# Work claim — Regeneration Engine subset structural freshness

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-regeneration-engine-subset-structural-freshness-20260812-1208`
- Registered: `2026-08-12T12:08:00+07:00`
- Completed: `2026-08-12T12:15:00+07:00`
- Baseline main SHA: `9e6a5cc371b93a8df6a683775d7b9b59359421f0`
- Claim commit: `6d93e196d07b02afc59d71aa42f83ac283a7a706`
- Source fix commit: `a586f6a16356c281491e688e393707202534ce7a`
- Focused smoke commit: `47c77158dbc2fdc7d70f2e004dc22bff9a047e93`
- Integration PR: `#869`
- Main integration SHA: `6d3dd8a4e7562fc9d0bedc93207b55e88d98a675`
- Priority: P1 — targeted regeneration must not mutate a structurally replaced semantic element under an unchanged ChangeVersion.
- Task Key: `CORE-REGENERATION-ENGINE-SUBSET-STRUCTURAL-FRESHNESS`

## Confirmed defect

`RegenerationEngine.RegenerateDirtySubset(...)` captured project element count and `ChangeVersion` before caller target-ID enumeration, but after enumeration it scanned live `project.Elements`. Direct public-list edits that do not call `Touch()` could therefore replace/reorder/remove semantic element instances while retaining the same revision, and targeted regeneration could resolve and mutate replacement same-ID instances.

## Implemented contract

- Targeted regeneration snapshots exact ordered project element references before lazy caller target enumeration.
- The existing target cardinality bound uses the captured element count.
- Existing `ChangeVersion` freshness rejection remains first for ordinary semantic mutations.
- Count/order/reference structural drift is rejected before target resolution and again immediately before transactional regeneration.
- Targets are resolved in captured project order from the original structural snapshot rather than a changed live list.
- Existing null/duplicate/missing target validation, dependency validation, rollback behavior, regeneration rules and full-project regeneration behavior remain unchanged.

## Regression evidence

`tests/QS3D.Core.SmokeTests/RegenerationEngineSubsetStructuralFreshnessSmoke.cs` uses the established Beam fixture. A lazy target-ID source replaces target `B1` with a new same-ID Beam through `project.Elements` without calling `Touch()`. The smoke requires structural freshness rejection with unchanged project revision and no regenerated quantity on either replacement or detached original. A stable `B1` subset control still regenerates and yields `NetVolumeM3 ~= 0.9` while leaving `B2` untouched.

## Integration / concurrency evidence

The branch diff from claim commit contained exactly the reserved `RegenerationEngine.cs` change plus the focused smoke. Eight commits between claim `6d93e196...` and reviewed moving `main@c805b9ab...` did not touch the reserved source or smoke. Current-main readback immediately before merge still had the exact pre-fix source blob `151b5ce99e35655f389008fa809bfa7f63f0c159`. PR #869 was squash-merged with expected head `47c77158dbc2fdc7d70f2e004dc22bff9a047e93` as `6d3dd8a4e7562fc9d0bedc93207b55e88d98a675`.

## Validation boundary

No GitHub Actions were dispatched. No force-push was used. No local .NET/full executable smoke or licensed BricsCAD V25/V26 runtime PASS is claimed from this connector-only lane.
