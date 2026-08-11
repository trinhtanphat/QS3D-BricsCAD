# QS3D Revision Semantic Review UI — source handoff

Updated: 2026-08-11

This batch exposes the WS-27 grouped semantic change model in the existing Revision window while preserving the current quantity report and modeless document-freshness boundaries.

## UI behavior

`RevisionWindow` now has two review tabs:

- **Khối lượng** — the existing `QuantityRevisionReport` rows, unchanged as the dedicated quantity-diff view;
- **Ngữ nghĩa** — one row per stable semantic element from `SemanticChangeReviewBuilder`, showing Added/Removed/Changed plus Identity, Property, Quantity and omitted source-reference change counts.

The header/footer summarizes both semantic and quantity change counts. When there are semantic changes but no quantity rows, the window opens directly on the Semantic tab so non-quantity changes are not hidden behind an empty quantity view.

## Locate behavior

The semantic tab does not capture native handles. It forwards only the stable `ElementId` through the existing Revision locate callback. `ReviewCommands.LocateCurrentElement` then re-resolves:

1. the active DWG;
2. the current `ProjectState`;
3. the current semantic element by stable ID;
4. current source handles through `SourceHandleResolver`;
5. current native entities through `CadHandleService.Select`.

Removed elements therefore fail closed when they no longer exist in the current project; stale modeless rows do not keep a captured native handle alive.

## Portable-authority boundary

The Semantic tab displays only `OmittedSourceReferenceChangeCount` for source-reference changes. It does not expose raw before/after `SourceHandles` and does not treat handles as portable revision authority.

The UI has no native write transaction, no Apply path and no semantic mutation. It remains a review surface over existing revision snapshots.

## Regression gate

`scripts/preflight-revision-semantic-review-ui.py` checks:

- both Quantity and Semantic tabs exist;
- grouped semantic counts are bound to `SemanticChangeReviewElement`;
- raw source handles/native write APIs are absent from the window;
- the window stays document-bound;
- semantic Locate forwards stable `ElementId` only;
- `ReviewCommands` still re-resolves the current project/element/source handles at click time;
- Core semantic review remains backed by `RevisionService.Compare`.

The aggregate `preflight-all.py` discovers this gate automatically.

## Qualification status

This is source integration only. It does not claim licensed BricsCAD V25 WPF rendering, DPI/theme fidelity, NETLOAD, private-DWG behavior, multi-document interaction or exact-SHA runtime qualification. Those remain `LOCAL_ONLY`.
