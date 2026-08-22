# Work claim — Semantic Selection explicit property override parity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-semantic-selection-instance-override-20260812-1041`
- Registered: `2026-08-12T10:41:00+07:00`
- Baseline main SHA: `8583e783369fa87bf76551d3edbc31abe8c82ce1`
- Priority: P1 Core semantic mutation parity

## Confirmed defect

`SemanticSelectionBulkEditService.SetProperty(...)` determines no-op from `EffectivePropertyValue(...)`, which treats a Family fallback value as present. When an element has no instance property and its Family currently exposes the requested value, an explicit semantic-selection SetProperty is skipped instead of materializing the instance override. The canonical `BulkEditService.SetProperty(...)` only no-ops when the actual instance property already equals the requested value.

This makes the two explicit bulk-set paths observably different and leaves a supposedly edited element dependent on future Family property changes.

## Reserved scope

- `src/QS3D.Core/Selection/SemanticSelectionBulkEditService.cs`
- focused regression/preflight for explicit instance-property materialization
- this claim file for close-out

## Contract

- Explicit `SetProperty` no-ops only when the selected element already owns the same instance property value.
- Missing instance property must be materialized even when the current Family fallback equals the requested value.
- Preserve `ProjectElement.SetProperty(...)`, mutation journaling, project touch behavior, generated-output freshness/dirty semantics, selection validation, and deterministic changed IDs.
- Do not change numeric multiplication or Family assignment semantics in this claim.

## Exclusions

- No UI/command styling changes.
- No Family catalog mutation behavior changes.
- No Quantity/Opening/Reporting/Health lane changes.
- No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime PASS claim.

## Validation plan

Add focused regression coverage that fails if `SetProperty(...)` uses Family fallback equality to suppress an instance write, while preserving true instance-value no-op behavior and the canonical `ProjectElement.SetProperty(...)` mutation path.

## Coordination

This scope is separate from active Door/Opening XLSX row snapshot and other concurrent Core health/integrity claims. Existing BulkEdit generated-output freshness work is preserved rather than reimplemented.

## Completion evidence

- Source fix commit: `0ea3e2d1060ab10862a1950a7c0a2e2227ce92d5`
- Focused regression commit: `6fb87e42d20536859752ff9ec83ba1a5d37a2551`
- Pull request: `#927`
- Merge commit on `main`: `8db23a0ff0ecaca56c99b2f64b1de84920db9138`
- Post-merge source read-back blob: `e91c2097b5f8b9303f2401b7844158c32bb766e1`
- Post-merge regression read-back blob: `75dda7b3f070a17c7bdfc580f6590c096d1e083b`
- GitHub Actions/build/release dispatch: not run, per claim exclusions.
- Local build: not run through this connector.
- BricsCAD V25/V26 runtime: not run; no runtime PASS claim.

## Completion condition

Satisfied: source fix and focused regression are integrated on `main`, read back after merge, and this claim records exact commit evidence plus remote validation boundaries.
