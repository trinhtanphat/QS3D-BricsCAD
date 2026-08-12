# Work claim — Template empty BQ-column layout apply fidelity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:11:00+07:00`
- Baseline main SHA: `1da219ca2233df338429f914678c65aa1db133ea`
- Priority: P1 template/BQ settings fidelity during owner-requested `continue all`
- Task Key: `CORE-TEMPLATE-EMPTY-BQ-LAYOUT-APPLY`

## Confirmed defect

`.qstemplate` is the company-standard import/export surface for BQ column layout. `TemplateProfileStore.Serialize(...)` always emits the canonical `bqColumns` section, including an empty section when `VisibleBqColumns` is empty, and `Load(...)` faithfully reconstructs an empty list. However, `Apply(...)` only writes `QS3D.BqVisibleColumns` when the incoming list contains at least one column and otherwise leaves any existing project metadata untouched.

Applying a template that explicitly carries an empty BQ column layout therefore preserves stale project column preferences instead of applying the template state. This breaks template round-trip/apply fidelity.

## Reserved scope

- `src/QS3D.Core/Templates/TemplateProfileStore.cs`
- one focused auto-registered Core smoke for empty/nonempty BQ column layout apply behavior
- this claim file for close-out

## Contract

- an empty normalized `VisibleBqColumns` list removes the existing `QS3D.BqVisibleColumns` project metadata key;
- a nonempty normalized list continues to replace the metadata value with the template's canonical pipe-delimited layout;
- applying an empty layout when the project already has no layout remains an idempotent metadata no-op apart from the existing template-apply audit semantics;
- preserve template validation, BQ-column canonicalization/load/save behavior, family/rule/layer-mapping apply semantics and rollback protection;
- do not modify UI, QSDB persistence format, template XML schema, release preflight or native BricsCAD behavior.

## Validation plan

Add deterministic ModuleInitializer smoke coverage for clearing a pre-existing BQ layout, replacing it with a nonempty template layout, and rollback preservation if a later apply step throws. Re-fetch `TemplateProfileStore.cs` before source write and inspect exact pushed diffs. No GitHub Actions dispatch, executable .NET smoke/build PASS, or licensed BricsCAD runtime qualification will be claimed unless actually executed.
