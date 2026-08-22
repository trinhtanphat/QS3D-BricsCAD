# QS3D Native Semantic Tags

Updated: 2026-08-10 (UTC+7)

QS3D now has a guarded P0 native documentation slice that turns the existing Core `SemanticTagRenderer` output into owned BricsCAD `MText`.

## Commands

```text
QS3DTAG
QS3DTAGREFRESH
QS3DTAGREMOVE
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

`GeneratedSemanticTagHealthService` is part of `ComprehensiveModelHealthService` / normal Health and Release paths. It checks persisted tag metadata without mutating the model:

- generated handle syntax/duplicates/source-handle leakage;
- owner project/element/version;
- template validity;
- re-rendered current text versus stored built text (`SEMANTIC_TAG_TEXT_STALE`);
- generated/native runtime property exposure through templates;
- text height;
- drawing-local position scope and finite X/Y/Z.

A semantic element is **not required** to have a tag. Health starts only when generated tag ownership exists. After a successful `QS3DTAGREMOVE`, tag ownership metadata no longer exists and the element is again in the optional/no-tag state.

P0 persisted health does not yet prove the live CAD handle is an MText after save/reopen. Exact live-entity/XData/content verification belongs to the licensed V25 runtime matrix or a future dedicated native tag health command.

## Product boundary

Source is implemented and statically guarded, but the following remain open:

- exact-SHA licensed BricsCAD V25 placement/refresh/remove/save-reopen/Undo/runtime qualification;
- native MLeader / leader geometry;
- automatic associative reposition when source geometry moves;
- batch auto-placement / collision avoidance;
- dimensions, title blocks and sheet/layout generation;
- paper-space/view-specific annotation scale behavior;
- standards-specific documentation templates;
- reusable tag-style/template UI beyond normal Family/Instance properties.

Do not claim full documentation/sheet parity from `QS3DTAG`. It is a guarded semantic-to-native MText lifecycle slice built on the existing Core renderer.
