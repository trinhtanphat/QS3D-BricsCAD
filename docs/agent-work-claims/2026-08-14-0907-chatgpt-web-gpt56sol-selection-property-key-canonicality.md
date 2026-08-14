# Work claim — Semantic Selection property-key canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-selection-property-key-canonicality-20260814-0907`
- Registered: `2026-08-14T09:07:00+07:00`
- Baseline main SHA: `53b1b4f03a29d827be38bbbf9eb1bba9bd8df21b`
- Priority: `P1 Core semantic-integrity hardening` — semantic property inspection must not emit malformed property identities created by direct dictionary mutation.

## Confirmed source gap

Canonical `ProjectElement.SetProperty(...)` trims requested property names, and completed Family/element property-map mutation lanes now fail closed on malformed existing maps before real mutations. `SemanticSelectionInspector.BuildEffectiveProperties(...)`, however, still copies raw keys directly from the public Family and element `Properties` dictionaries. A bypassed/direct padded key such as `" Mark "` can therefore appear as a public editable `SemanticSelectionTextValue.Name`, while a whitespace-only key is silently hidden by the ownership filter rather than reported as corrupted state. This can split one semantic property into multiple observable identities at a read/projection boundary.

## Reserved scope

- `src/QS3D.Core/Selection/SemanticSelectionInspector.cs` — property-key validation inside effective-property projection only.
- `tests/QS3D.Core.SmokeTests/SemanticSelectionInspectorSmoke.cs` — focused regression only.
- this claim file.

## Acceptance

1. Fail closed when selected-element direct property state contains a blank/whitespace-only key or a nonblank key with surrounding whitespace.
2. Fail closed when the selected element's referenced Family contains the same malformed property-key states before inherited values are projected.
3. Preserve internal ownership-property filtering for canonical internal keys, Family→instance override precedence, case-insensitive canonical identity, mixed/present semantics and deterministic ordering.
4. Inspection stays read-only and does not repair/rewrite corrupted maps.

## Explicit non-scope

No changes to property mutation services, editable-key policy, property values, quantity inspection, persistence schema, mapping, measurement, reports, cost, IFC, documentation/layout, update/release, LOCAL/native/UI or BricsCAD adapters. No control-character policy expansion. No GitHub Actions dispatch and no force-push.

## Evidence / history

- `3a8663c94430a35a55cb6d13987b9a0ba4892391` completed Family property-map mutation canonicality.
- `53b1b4f03a29d827be38bbbf9eb1bba9bd8df21b` completed generic element Bulk property-map mutation preflight; that lane is now closed before this claim.
- Current `SemanticSelectionInspector.BuildEffectiveProperties(...)` at the baseline assigns raw Family and element dictionary keys directly into the effective projection after only ownership classification.
- Targeted history search found no existing semantic-selection property-map projection canonicality lane.

## Validation plan

Publish this claim alone, refresh `main` and recheck exact Selection source/test overlap, then add bounded read-side key validation and focused smoke cases for element and Family corruption plus canonical happy paths. Re-fetch exact diffs/live source, close `COMPLETED`, and report managed/native execution truthfully as `NOT_RUN` unless actually run.

## Completion condition

Current `main` rejects blank/padded Family or element property keys at semantic-selection projection, focused regression is pushed and remotely verified, and this claim is closed `COMPLETED` with exact commit references.
