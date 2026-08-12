# Work claim — Quantity Rule provenance generic-edit guard

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:42:00+07:00`
- Baseline main SHA: `661cc8400397aeb74a2695ffec69bb49bab33f93`
- Priority: P1 Core quantity-state integrity during owner-requested `continue all`
- Task Key: `CORE-RULE-PROVENANCE-GENERIC-EDIT-GUARD`

## Confirmed defect

`QuantityRuleEngine` reserves `ProjectElement.Properties` keys under the `Rule:` prefix as managed quantity-rule provenance. `GetStaleManagedOutputs(...)` later interprets those keys as ownership markers and may remove the corresponding quantity/provenance when the output is no longer active.

The shared `SemanticPropertyEditPolicy` used by both `BulkEditService` and `SemanticSelectionBulkEditService` already blocks semantic identity/reference, CAD-derived and native/generated ownership namespaces, but it does not reserve `Rule:`. Generic property editing can therefore create or overwrite `Rule:<output>` metadata that the quantity-rule lifecycle later trusts as internal provenance, allowing user/bulk edits to spoof managed-output ownership and trigger stale cleanup of an unrelated quantity.

## Reserved scope

- `src/QS3D.Core/Services/SemanticPropertyEditPolicy.cs`
- focused Core smoke coverage for the shared generic property-edit boundary
- this claim file for close-out

## Contract

- reject any generic semantic property key whose canonical trimmed spelling starts with `Rule:` case-insensitively;
- fail before element dirty/timestamp/project change-version mutation;
- protect both low-level `BulkEditService` and selection bulk editing through the existing shared policy;
- preserve ordinary user-defined keys that merely begin with `Rule` but are outside the reserved `Rule:` namespace, such as `RuleFactor`;
- do not change Quantity Rule evaluation, provenance read/cleanup behavior, inspector-only relation work, UI/native BricsCAD behavior, or other property namespaces.

## Validation plan

Add deterministic auto-registered Core smoke coverage proving reserved `Rule:` edits fail closed without mutation and a nearby non-reserved user key remains editable. Re-fetch source/claim before each write and inspect exact pushed diffs. No GitHub Actions dispatch, executable .NET smoke/build PASS, or licensed BricsCAD V25/V26 runtime qualification will be claimed unless actually executed.
