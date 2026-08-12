# Work claim — Template empty BQ-column layout apply fidelity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:11:00+07:00`
- Completed: `2026-08-12T10:16:00+07:00`
- Baseline main SHA: `1da219ca2233df338429f914678c65aa1db133ea`
- Claim commit: `9932c14491168d42655e95f024a7bc410af017e5`
- Source fix commit: `80805f5178ce981f1ba5185cc5d68157c2b07f58`
- Regression smoke commit: `388b42a82d7854fce83eee78fbb83d28cf4aaf0c`
- Priority: P1 template/BQ settings fidelity during owner-requested `continue all`
- Task Key: `CORE-TEMPLATE-EMPTY-BQ-LAYOUT-APPLY`

## Confirmed defect

`.qstemplate` is the company-standard import/export surface for BQ column layout. `TemplateProfileStore.Serialize(...)` always emits the canonical `bqColumns` section, including an empty section when `VisibleBqColumns` is empty, and `Load(...)` reconstructs an empty list. However, `Apply(...)` only wrote `QS3D.BqVisibleColumns` when the incoming list contained at least one column and otherwise left any existing project metadata untouched.

Applying a template that explicitly carried an empty BQ column layout therefore preserved stale project column preferences instead of applying the template state, breaking template round-trip/apply fidelity.

## Implemented contract

- an empty normalized `VisibleBqColumns` list now removes the existing `QS3D.BqVisibleColumns` metadata key;
- a nonempty normalized list continues to replace the metadata with the template's pipe-delimited layout;
- an empty layout applied to a project without the metadata remains absent;
- template validation, BQ-column load/save canonicalization, family/rule/layer-mapping apply behavior and the existing `ProjectStateSnapshot` rollback boundary are unchanged;
- no UI, QSDB schema, template XML schema, release-preflight or native BricsCAD behavior changed.

## Regression coverage

`TemplateEmptyBqLayoutApplySmoke` is auto-registered with a module initializer and covers:

- clearing a pre-existing project BQ-column preference with an empty template layout;
- replacing a pre-existing preference with a nonempty two-column template layout;
- applying an empty layout when no project preference exists, ensuring the metadata key remains absent.

The source change is a single metadata set/remove branch inside the existing transactional `Apply(...)` body. The pre-existing snapshot/rollback catch boundary remains intact; no new fault injection was added because this change introduces no operation outside that boundary.

## Validation performed

- Current-main readback confirmed `TemplateProfileStore.Apply(...)` contains the symmetric set/remove logic after normalizing `VisibleBqColumns`.
- Current-main readback confirmed `tests/QS3D.Core.SmokeTests/TemplateEmptyBqLayoutApplySmoke.cs` is present with blob SHA `16a7a0a41d39c78d2f32b1a4ccb0e2934780a571`.
- The release #30 Template Profile schema claim explicitly excludes production `TemplateProfileStore.cs`; prior family-property/template canonicality claims were already completed, so this source scope did not overlap them.
- No GitHub Actions were dispatched. No executable .NET smoke/full build PASS and no licensed BricsCAD runtime qualification are claimed from this connector-only session.

## Completion

`COMPLETED`: applying an empty `.qstemplate` BQ-column layout now clears stale project column preferences instead of silently retaining them, while nonempty layout replacement remains unchanged.
