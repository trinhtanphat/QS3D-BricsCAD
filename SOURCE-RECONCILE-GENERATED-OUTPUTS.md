# Source reconcile generated-output lifecycle

`QS3DSYNCSOURCE` treats an explicitly selected authoritative CAD source as a geometry change even when measured length/area is numerically unchanged. This is intentional: translation, rotation, endpoint movement, and other location/orientation edits can make generated CAD output spatially stale without changing scalar quantities.

## Transaction boundary

Source reconcile expands the semantic dependency closure, captures a `ProjectStateSnapshot`, starts one CAD transaction, prepares generated-output invalidation, refreshes source-derived semantic state, regenerates the affected semantic subset to a stable state, commits invalidation metadata, touches the project, and only then commits the CAD transaction. A failure before CAD commit restores the semantic snapshot.

## Spatial generated outputs

The invalidator removes source-dependent generated CAD that must not remain at its previous location:

- host `GeneratedSolid*` output;
- generated rebar/mesh owner slots defined by `GeneratedHandleOwnershipPolicy.RebarHandleKeys`;
- generated curtain-wall frame output;
- physical opening-cut state/host solid replacement handled by the existing opening/solid ownership path;
- `GeneratedGridAnnotation*` extension lines, bubbles and labels.

Grid annotation invalidation is destructive only after two independent ownership checks succeed:

1. the handle must have a unique semantic owner and the logical owner slot must be `GeneratedGridAnnotationHandles` on the Grid being reconciled;
2. the live CAD entity must carry matching QS3D XData for the same project, element and `Grid` category.

A live Grid annotation handle that resolves to an unexpected CAD type also fails closed. Missing live handles are tolerated so stale metadata can be removed and the annotation can be rebuilt explicitly with `QS3DGRIDANNOTATE` / `QS3DGRIDANNOTATEALL`.

## Semantic tags are intentionally retained

`GeneratedSemanticTag*` is not part of generic source-geometry invalidation. Tag placement is drawing-local/user-controlled and should not disappear just because an authoritative source moves. Rendered tag content is checked by semantic-tag health and can be refreshed with the existing tag refresh command. This lifecycle is deliberately separate from spatial Grid annotation replacement.

## Runtime boundary

The ownership/invalidation source contract and static preflight are remote-safe. Exact BricsCAD V25 behavior for transaction rollback, Undo/Redo, save/reopen, multi-DWG, locked layers, unusual owner spaces and real private DWGs remains `LOCAL_ONLY` until exercised against installed BricsCAD V25 runtime references. No release claim should infer those runtime gates from static source checks alone.
