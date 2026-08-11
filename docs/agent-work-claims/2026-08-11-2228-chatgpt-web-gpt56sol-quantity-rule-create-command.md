# Agent work claim — Quantity intersection rule creation command

- Agent: `chatgpt-web-gpt56sol-quantity-rule-create-command-20260811-2228`
- Started: `2026-08-11T22:28:00+07:00`
- Status: `ACTIVE`
- Task ID / title: `QS3DRULECREATE missing directed intersection rule`
- Source / user driver: user requested `continue all`; original priority explicitly included “Tạo rule”. Current Quantity Settings browser can edit existing directed pairs but explicitly refuses to create a missing pair from a partial/imported template.
- Baseline main SHA: `ee620fdc586a48581aaa9613315aa5510bd3845b`

## Objective

Add a narrow BricsCAD-native creation command for a missing directed quantity intersection pair without changing the existing Quantity Settings WPF/store/core settings implementation. The command must load validated settings, restrict source/target to category codes already observed by the settings payload, reject duplicates, require explicit confirmation, append one all-false directed rule, validate the whole clone, and persist through the existing atomic `QuantitySettingsStore.Save(...)` path.

## Expected path surfaces

- `src/QS3D.BricsCAD.V25/QuantityRuleCreateCommands.cs` (new)
- `scripts/preflight-quantity-rule-create.py` (new)
- this claim file for close-out

## Explicit exclusions

- `QuantitySettingsWindow.xaml*` and `QuantitySettingsStore.cs`
- Core quantity settings/rule models and arithmetic
- active Quantity Settings health-export claim surfaces
- Ribbon, Workspace, geometry/builders, persistence schema, updater/release
- every other agent's claim file

## Dependencies / risks / merge constraints

- Existing `QS3DSETUP` remains the editor for rule flags; creation command only creates a missing A -> B row with every subtraction option disabled so behavior is not inferred.
- A -> B and B -> A remain distinct; no reverse rule is auto-created.
- Unknown compatibility codes remain usable only when already present in category/intersection settings; the command does not invent new category codes.
- Main is highly concurrent; refresh before writes and never force-push.
- No BricsCAD V25 runtime proof is claimed remotely.

## Validation gates

- command aliases `QS3DRULECREATE` and `QS3DINTERSECTIONRULECREATE` have one canonical owner;
- command calls `QuantitySettingsStore.Load()`, validates observed category codes, rejects `FindIntersectionRule(source, target) != null`, asks explicit confirmation, appends exactly one `QuantityIntersectionRuleSetting`, calls `NormalizeAndValidate()`, then `Save(settings)`;
- new rule defaults all subtraction booleans to `false` by construction and does not auto-create reverse rule;
- source preflight forbids project/CAD mutation and direct JSON/file writes;
- no GitHub Actions dispatch.

## Exact completion condition

A user can create a missing directed quantity rule from BricsCAD through `QS3DRULECREATE`, duplicate/unknown/cancel cases leave settings untouched, the new row persists through the existing validated atomic store, a focused static gate is merged on `main`, and this claim is marked `COMPLETED` with exact SHAs.