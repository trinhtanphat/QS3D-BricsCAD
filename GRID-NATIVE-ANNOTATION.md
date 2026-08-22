# QS3D native Grid annotation — BricsCAD V25

Updated: 2026-08-10 (UTC+7)

## Commands

- `QS3DGRIDANNOTATE`: select already-captured Grid source LINE/ARC entities and replace their native annotation.
- `QS3DGRIDANNOTATEALL`: replace annotation for every semantic Grid that already has a non-empty `GridLabel`.

`QS3DGRID` remains the source-capture command and `QS3DGRIDNUMBER` remains the explicit-order semantic naming command. Native annotation does not invent labels and does not change Grid ordering.

## Native output

For each source endpoint the builder creates:

1. a short extension line when the bubble center is offset from the source endpoint;
2. a native `Circle` bubble;
3. native `DBText` containing the semantic `GridLabel`.

LINE sources place bubble centers beyond each line endpoint. ARC sources offset each endpoint radially away from the arc center. Annotation is appended to the same owner space and attempts to inherit the source layer.

The annotation plane is explicit rather than view-dependent:

- a planar LINE whose endpoints share one WCS Z elevation uses WCS Z as the annotation normal;
- **ARC uses its native plane normal** for both the bubble `Circle` and `DBText` normal;
- a **3D-sloped LINE** is rejected fail-closed because one line alone does not define a stable annotation plane. The builder does not silently project it onto WCS-XY.

This avoids the previous source-level failure mode where every bubble/text was forced onto `Vector3d.ZAxis` even when an ARC lived on another plane. Exact text orientation/readability on tilted ARC planes still requires licensed V25 visual qualification.

Default semantic parameters are:

- `GridBubbleRadiusM = 0.25`;
- `GridTextHeightM = 0.18`.

Element/family values may override those defaults. Values must remain finite and positive, and text height is bounded relative to bubble radius.

## Generated ownership

All extension lines, circles and text entities are marked through QS3D generated `XData` using the current project id, Grid element id and `ElementCategory.Grid`.

The semantic Grid stores the generated handles in:

`GeneratedGridAnnotationHandles`

Additional persisted metadata records the label, project owner, element owner and ownership version. `GeneratedHandleOwnershipPolicy` already recognizes `Generated*Handles` slots, so the annotation family participates in common generated-handle ownership discovery instead of using an untracked side channel.

Replacement is fail-closed: a previously tracked live annotation entity is erased only after `GeneratedGeometryService.RequireMatchingOwnership` confirms that its QS3D XData still belongs to the same project and semantic Grid. A user/native entity or an entity owned by another QS3D element is never silently erased.

## Cross-layer atomicity

`GridAnnotationBuilder` captures a `ProjectStateSnapshot`, opens one native CAD transaction for the whole batch, erases matching old annotation, appends owned replacement entities, writes the new semantic handle metadata and audit events, calls `project.Touch()`, then commits CAD.

Any exception before native commit causes the CAD transaction to roll back and restores the semantic snapshot. A failed semantic restore is surfaced as an aggregate rollback failure. `Editor.Regen()` and palette refresh happen after commit as best-effort UI work.

This prevents the failure window where DWG annotation is committed while `.qsdb` ownership still points at the previous entities.

## Persisted + live health integration

`GeneratedGridAnnotationHealthService` is the Core/persisted checker. It validates annotation metadata without pretending Core can inspect a live BricsCAD database. It reports malformed/duplicate handles, generated handles leaking into `SourceHandles`, stale built labels, owner project/element/version mismatches, and invalid bubble/text sizing.

`GeneratedGridAnnotationRuntimeHealthService` is the V25 **read-only live** checker. For every persisted annotation handle it verifies:

- the handle still resolves to a live CAD entity;
- the deterministic six-slot layout remains `Line / Circle / DBText` for each endpoint;
- the entity still carries matching QS3D XData ownership for the current project/Grid;
- each live `DBText` still equals the current semantic `GridLabel`.

It surfaces `GRID_ANNOTATION_CAD_*` issues for missing entities, slot type mismatch, ownership mismatch and stale text. The checker only opens CAD objects for read and never repairs, erases or silently reclaims a mismatched entity.

`GeneratedSolidRuntimeHealthService` aggregates the live Grid checker, so the existing `QS3DHEALTH` runtime path receives native Grid integrity without another command. `ComprehensiveModelHealthService` continues to supply the persisted Core checks and classify `GRID_ANNOTATION` issues as generated-output issues.

Persisted and live checks are deliberately separate: healthy `.qsdb` metadata is not proof that the CAD object exists, and a live CAD object is not trusted merely because its handle resolves. Matching QS3D ownership remains mandatory. The existing generated-output locate routing can prefer a tracked generated handle while retaining semantic/source fallback when output is missing.

## Explicit exclusions

This source batch does not claim completion of:

- automatic rectangular/radial Grid discovery;
- intersection constraints or dimensions;
- associative annotation reactors;
- DrawJig-based new Grid authoring;
- automatic paper-space viewport annotation;
- native Level heads/elevation symbols.

Those require separate ownership and runtime contracts rather than hidden side effects in the naming command.

## Runtime qualification

Source implementation is `REMOTE_DONE`; native validation remains `LOCAL_ONLY` until the exact source SHA is exercised on licensed BricsCAD V25.

The V25 runtime matrix must include at least:

1. WCS-planar LINE plus ARC sources in a real DWG;
2. tilted/non-WCS ARC and confirmation that Circle/DBText remain on the ARC native plane;
3. 3D-sloped LINE refusal with zero CAD/semantic residue;
4. first annotation and repeated replacement without duplicate bubbles/text;
5. label change followed by replacement;
6. bubble/text size overrides and drawing-unit conversion;
7. ownership mismatch refusal after intentionally replacing one generated handle with a non-QS3D entity;
8. live health after erase, entity-type replacement, XData ownership corruption and manual DBText editing;
9. Undo/Redo around a completed batch;
10. cancel/exception before commit with no partial semantic or CAD mutation;
11. save/reopen and rebuild from persisted `GeneratedGridAnnotationHandles`;
12. multi-DWG isolation and source owner-space behavior;
13. Unicode labels and HiDPI visual review.

Do not describe native Grid annotation as BricsCAD V25 runtime-certified until that matrix is actually executed.
