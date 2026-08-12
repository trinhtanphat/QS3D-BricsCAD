# Semantic Tag Remove PICKFIRST — 2026-08-11

## Goal

Remove the redundant second entity pick when a user has already selected the generated Semantic Tag MText or the authoritative semantic source before running `QS3DTAGREMOVE`.

## Source behavior

`QS3DTAGREMOVE` now declares `CommandFlags.Modal | CommandFlags.UsePickSet`.

Selection is deterministic:

1. Exactly one implied/PICKFIRST entity: use its handle directly.
2. No implied selection: preserve the existing `Editor.GetEntity(...)` fallback.
3. More than one implied entity: fail closed before canonical project binding/destructive removal.

Existing ownership rules are unchanged. A selected generated object is accepted only when its canonical owner slot is `GeneratedSemanticTagHandles`; unrelated QS3D-generated geometry remains rejected. Authoritative sources retain the existing unique-owner and generated-tag metadata checks.

## Destructive safety boundary

Selection is still complete before `ExistingProjectMutationContext.Require(...)`. `ResolveTagOwner(...)` still runs before `SemanticTagRemovalService.Remove(...)`, and the removal service remains the only destructive implementation. No project bootstrap path was added.

## Static contract

```text
python scripts/preflight-semantic-tag-remove-pickfirst.py
```

The gate locks PICKFIRST flags, implied-selection-before-picker ordering, multiple-selection fail-closed behavior, selection-before-bind, owner-resolution-before-remove and no-bootstrap behavior.

## BricsCAD V25 local qualification

Source/static completion is not a live runtime PASS. Qualify on V25:

- preselect one generated Semantic Tag MText → `QS3DTAGREMOVE` removes it without a second entity pick;
- preselect its authoritative source → same remove path without a second pick;
- no preselection → explicit picker still appears;
- multiple preselection → fail closed before project bind/remove;
- unrelated generated QS3D output → still rejected by generated owner-slot validation;
- ESC from explicit picker → no removal/no semantic mutation;
- stale/missing/ambiguous owner and incomplete generated-handle cases retain existing fail-closed behavior;
- active-DWG switching during interactive selection remains subject to existing V25 document lifecycle qualification.

No GitHub Actions or BricsCAD runtime qualification is claimed by this source batch.
