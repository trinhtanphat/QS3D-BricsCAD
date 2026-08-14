# Work claim — Semantic Selection property-key canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-selection-property-key-canonicality-20260814-0907`
- Registered: `2026-08-14T09:07:00+07:00`
- Baseline main SHA: `53b1b4f03a29d827be38bbbf9eb1bba9bd8df21b`
- Priority: `P1 Core semantic-integrity hardening` — semantic property inspection must not emit malformed property identities created by direct dictionary mutation.

## Confirmed source gap

Canonical `ProjectElement.SetProperty(...)` trims requested property names, and completed Family/element property-map mutation lanes fail closed on malformed existing maps before real mutations. `SemanticSelectionInspector.BuildEffectiveProperties(...)` previously copied raw keys directly from the public Family and element `Properties` dictionaries. A bypassed/direct padded key such as `" Mark "` could therefore appear as a public editable `SemanticSelectionTextValue.Name`, while a whitespace-only key was silently hidden by the ownership filter rather than reported as corrupted state.

## Reserved scope

- `src/QS3D.Core/Selection/SemanticSelectionInspector.cs` — property-key validation inside effective-property projection only.
- `tests/QS3D.Core.SmokeTests/SemanticSelectionInspectorSmoke.cs` — focused regression only.
- this claim file.

## Implemented acceptance

1. Selected element property maps now fail closed on blank/whitespace-only and surrounding-whitespace keys before public property projection.
2. Referenced Family property maps receive the same validation before inherited values are projected.
3. Canonical internal ownership keys remain filtered, Family→instance override precedence and case-insensitive canonical identity remain unchanged, and existing mixed/present ordering semantics are preserved.
4. Inspection remains read-only; malformed Family/element property dictionaries are not repaired or rewritten.

## Explicit non-scope

No changes to property mutation services, editable-key policy, property values, quantity inspection, persistence schema, mapping, measurement, reports, cost, IFC, documentation/layout, update/release, LOCAL/native/UI or BricsCAD adapters. No control-character policy expansion. No GitHub Actions dispatch and no force-push.

## Evidence / history

- `3a8663c94430a35a55cb6d13987b9a0ba4892391` completed Family property-map mutation canonicality.
- `53b1b4f03a29d827be38bbbf9eb1bba9bd8df21b` completed generic element Bulk property-map mutation preflight before this claim was registered.
- Targeted history search found no pre-existing semantic-selection property-map projection canonicality lane.

## Completion record

- Claim-only commit: `a9dd8d0cd205b012a74f2c5ae7db367b4cffb4f0`.
- Production fix: `88a58559cd4d360bbd9b0e8452a3ae1123c1d233` (`fix(core): reject noncanonical selection property keys`). Exact commit inspection showed only bounded validation in `BuildEffectiveProperties(...)` plus the helper.
- Focused regression: `192b9ce2d1af6385f9cc27f97b4fd0d2fef4cfcb` (`test(core): guard selection property key canonicality`). It covers element blank/padded keys, Family blank/padded keys, read-only failure behavior and canonical inherited/instance property success.
- Remote verification: live source/test were re-fetched at `192b9ce2d1af6385f9cc27f97b4fd0d2fef4cfcb`; the key guards and focused smoke remained present.
- Concurrent reconciliation: unrelated commits advanced `main` between production and regression writes without touching the reserved Selection source/test, and all concurrent work remained on lineage.
- Managed .NET/Core smoke execution: `NOT_RUN` — no executable repository .NET toolchain was available through this connected workflow.
- GitHub Actions: `NOT_DISPATCHED`.
- BricsCAD/native runtime qualification: `NOT_RUN` / not claimed.

## Completion

Satisfied: current `main` rejects malformed Family or element property keys at semantic-selection projection, focused regression coverage is pushed and remotely verified, and this claim is closed `COMPLETED` with exact commit references and truthful validation status.
