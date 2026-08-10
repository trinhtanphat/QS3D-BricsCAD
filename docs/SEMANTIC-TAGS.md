# QS3D Native Semantic Tags

Updated: 2026-08-10 (UTC+7)

QS3D now has a guarded P0 native documentation slice that turns the existing Core `SemanticTagRenderer` output into owned BricsCAD `MText`.

## Commands

```text
QS3DTAG
QS3DTAGREFRESH
QS3DTAGREMOVE
QS3DTAGHEALTH
```

### `QS3DTAG`

1. select one **authoritative CAD source** already tracked by exactly one semantic element;
2. pick a tag insertion point;
3. QS3D resolves the tag template from Instance `SemanticTagTemplate`, then Family `SemanticTagTemplate`, defaulting to `{Id}`;
4. QS3D renders through `SemanticTagRenderer` before any destructive CAD mutation;
5. QS3D creates/replaces one owned native `MText` in the same owner space/layer context as the semantic source.

The text height resolves from Instance/Family `SemanticTagTextHeightM`, default `0.18` m, with finite/positive and upper-bound guards.

### `QS3DTAGREFRESH`

Select the same authoritative source again. QS3D re-renders the template against current semantic state and replaces the tag at the stored drawing-local WCS point/rotation. It does not invent a new placement.

If a tag has not been placed yet, refresh fails closed and tells the user to run `QS3DTAG` first.

### `QS3DTAGREMOVE`

Explicitly erases the generated semantic-tag `MText` and clears all `GeneratedSemanticTag*` ownership/render/placement metadata for its semantic owner.

The user may select either the generated tag itself or the authoritative semantic source. If the selected generated object belongs to another generated slot, removal is refused. Every live tag handle must resolve through the canonical `GeneratedSemanticTagHandles` owner slot, must still be `MText`, and must pass native QS3D XData ownership verification before destructive erase.

Removal is transactional: native erase, metadata clear, audit and project revision are committed together; a pre-commit failure restores the project snapshot and aborts the CAD transaction. A missing native tag handle may be treated as already absent after ownership metadata is validated, allowing stale semantic tag metadata to be cleaned without pretending another CAD object was erased.

`QS3DUNTRACK` intentionally remains a different operation: semantic untrack preserves CAD geometry by contract. It does not silently call `QS3DTAGREMOVE`. If the user wants the generated tag physically erased before detaching semantic ownership, run `QS3DTAGREMOVE` first and then `QS3DUNTRACK`.

### `QS3DTAGHEALTH`

Runs the persisted Core tag health plus the V25-side read-only live CAD inspection for generated semantic tags. It reports missing handles, wrong entity type, QS3D XData ownership mismatch, MText content drift, text-height drift, drawing-local WCS position/rotation drift and normal drift. The command never repairs or erases CAD; it only reports and can locate live tag handles for review.

The normal runtime health aggregator also includes this live semantic-tag inspection, so `QS3DHEALTH`/`QS3DHEALTHALL` see the same native integrity problems. `QS3DRELEASECHECK` consumes that runtime aggregator too, making live generated Solid3d/Grid annotation/Semantic Tag problems release blockers without claiming the separate licensed V25 qualification gate has been executed.

## Template contract

The native tag uses the same bounded Core renderer already used by source-side documentation work. Supported tokens are defined by `SemanticTagRenderer`, including:

- `{Id}`
- `{Category}`
- `{Family}`
- `{Floor}`
- `{Zone}`
- `{P:PropertyName}`
- `{Q:QuantityName}`

Generated/native runtime properties are not documentable through `{P:...}`. Invalid, nested, unsupported or over-limit templates fail before the existing tag is erased.

Rendered text is encoded as plain MText content: line breaks become MText paragraph separators and backslash/braces are escaped so semantic values are not silently treated as MText formatting commands.

## Ownership and replacement

The generated tag slot is:

```text
GeneratedSemanticTagHandles
```

Current P0 writes one MText handle but uses a plural owner slot so later reviewed leader/secondary entities can extend the same lifecycle without inventing a competing ownership store.

Replacement/removal is fail-closed:

- `GeneratedHandleOwnershipIndex` must resolve the old handle to the same semantic element and canonical `GeneratedSemanticTagHandles` slot;
- the live old entity must be `MText`;
- the native QS3D XData owner marker must match project + element + category;
- wrong/missing ownership never authorizes destructive erase;
- tag template/render/text-height validation occurs before destructive replacement;
- semantic generated-handle/template/text/owner/position metadata, audit and project revision advance while the CAD transaction is still rollback-capable;
- explicit remove clears all `GeneratedSemanticTag*` metadata only inside the same rollback-capable native transaction;
- if an operation fails before CAD commit, the CAD transaction aborts and `ProjectStateSnapshot` restores semantic state;
- viewport/Palette refresh is command-level best-effort after the operation boundary.

Persisted metadata includes:

- `GeneratedSemanticTagHandles`
- `GeneratedSemanticTagTemplate`
- `GeneratedSemanticTagText`
- `GeneratedSemanticTagOwnerProjectId`
- `GeneratedSemanticTagOwnerElementId`
- `GeneratedSemanticTagOwnershipVersion`
- `GeneratedSemanticTagTextHeightM`
- `GeneratedSemanticTagPositionScope=DrawingLocalWcs`
- `GeneratedSemanticTagPositionX/Y/Z` in drawing-local WCS drawing units
- `GeneratedSemanticTagRotationRad`

The position is deliberately marked drawing-local. It is not a portable interchange coordinate and must not be treated as one.

## UCS boundary

P0 accepts a current UCS whose XY plane is parallel to WCS XY. The point returned by the editor prompt is transformed by `Editor.CurrentUserCoordinateSystem` into WCS before persistence/native creation. The stored rotation follows the current UCS X axis.

Tilted/3D UCS is rejected rather than creating an ambiguously oriented tag.

## Health

`GeneratedSemanticTagHealthService` is part of `ComprehensiveModelHealthService` / normal semantic health paths. It checks persisted tag metadata without mutating the model:

- generated handle syntax/duplicates/source-handle leakage;
- owner project/element/version;
- template validity;
- re-rendered current text versus stored built text (`SEMANTIC_TAG_TEXT_STALE`);
- generated/native runtime property exposure through templates;
- text height;
- drawing-local position scope and finite X/Y/Z.

`GeneratedSemanticTagRuntimeHealthService` adds V25-side read-only validation of the live CAD entity referenced by that metadata. It checks live existence/type, XData ownership, encoded MText content, text height, drawing-local WCS placement/rotation and +Z normal. It never repairs or erases mismatched CAD.

A semantic element is **not required** to have a tag. Health starts only when generated tag ownership exists. After a successful `QS3DTAGREMOVE`, tag ownership metadata no longer exists and the element is again in the optional/no-tag state.

These source/static health paths improve runtime integrity detection, but they do not replace exact-SHA licensed BricsCAD V25 placement/refresh/remove/save-reopen/Undo qualification.

## Product boundary

Source is implemented and statically guarded, but the following remain open:

- exact-SHA licensed BricsCAD V25 placement/refresh/remove/save-reopen/Undo/runtime qualification;
- native MLeader / leader geometry;
- automatic associative reposition when source geometry moves;
- batch auto-placement / collision avoidance;
- native DWG Table generation/refresh;
- dimensions, title blocks and sheet/layout generation;
- paper-space/view-specific annotation scale behavior;
- standards-specific documentation templates;
- reusable tag-style/template UI beyond normal Family/Instance properties.

Do not claim full documentation/sheet parity from `QS3DTAG`. It is a guarded semantic-to-native MText lifecycle slice built on the existing Core renderer.
