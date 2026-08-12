# Semantic Tag PICKFIRST — 2026-08-11

## Goal

Remove one redundant CAD pick from the common Semantic Tag workflow without changing semantic ownership, tag content, UCS rules, native MText lifecycle or project mutation boundaries.

## Source behavior

`QS3DTAG` and `QS3DTAGREFRESH` now declare `CommandFlags.UsePickSet` in addition to `Modal`.

Selection acquisition is deterministic:

1. If exactly one implied/PICKFIRST entity exists, QS3D uses that CAD handle directly.
2. If the implied selection is empty, QS3D falls back to the existing `Editor.GetEntity(...)` prompt.
3. If more than one entity is implied, QS3D fails closed before canonical project binding instead of choosing one arbitrarily.

The existing authoritative-source policy is unchanged. A QS3D-generated object remains invalid for Semantic Tag source selection; `ResolveSourceElement(...)` continues to reject generated ownership and requires exactly one authoritative source owner.

## Lifecycle preserved

For `QS3DTAG`, source selection and tag placement are still completed before `ExistingProjectMutationContext.Require(...)`. The read-only preview identity/owner check remains before placement, and canonical owner identity is revalidated before `SemanticTagBuilder.Build(...)`.

For `QS3DTAGREFRESH`, source selection is still completed before canonical bind and native rebuild.

No `GetOrCreate`/project bootstrap path was added. No semantic tag builder, content encoder, handle ownership, remove, health or native cleanup logic changed.

## Static contract

Run:

```text
python scripts/preflight-semantic-tag-pickfirst.py
```

The gate locks command flags, implied-selection-before-explicit-picker ordering, multiple-selection fail-closed behavior, input-before-bind ordering, generated-owner validation and the no-bootstrap boundary.

## BricsCAD V25 local qualification

Source/static completion is not a BricsCAD runtime PASS. On a real V25 machine, qualify at least:

- preselect exactly one valid authoritative semantic source, run `QS3DTAG`, confirm there is no second entity pick and placement proceeds normally;
- no preselection, run `QS3DTAG`, confirm the existing explicit source picker still appears;
- preselect multiple entities, confirm fail-closed before placement/project mutation;
- preselect a QS3D-generated tag/solid, confirm generated ownership is still rejected;
- cancel the explicit source picker and cancel placement independently, confirming no native tag residue and no unintended semantic mutation;
- repeat the same selection cases for `QS3DTAGREFRESH`, including missing-tag and stale source-owner cases;
- switch active DWG during interactive input where practical and confirm existing document/project freshness behavior remains fail-closed.

Exact editor/PICKFIRST/ESC/document-switch evidence remains local-only; this source batch does not claim V25 runtime qualification.
