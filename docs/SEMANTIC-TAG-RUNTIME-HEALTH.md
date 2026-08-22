# QS3D Semantic Tag — Live Runtime Health

Updated: 2026-08-10 (UTC+7)

`QS3DTAGHEALTH` combines two independent checks:

1. **persisted semantic tag health** — template/render/owner/position metadata stored in the QS3D project;
2. **live BricsCAD MText health** — the native object currently referenced by `GeneratedSemanticTagHandles`.

This distinction matters after manual CAD edits, save/reopen, copy/paste, native deletion, XData damage or external DWG processing.

## Live checks

For every semantic element that owns `GeneratedSemanticTagHandles`, the runtime service opens the referenced entity read-only and reports:

- `SEMANTIC_TAG_MTEXT_MISSING` — handle no longer resolves to a live entity;
- `SEMANTIC_TAG_MTEXT_TYPE_MISMATCH` — handle resolves, but not to `MText`;
- `SEMANTIC_TAG_MTEXT_OWNERSHIP_MISMATCH` — QS3D generated XData no longer matches current project/element/category;
- `SEMANTIC_TAG_MTEXT_CONTENT_DRIFT` — live MText content differs from the rendered text recorded at the last create/refresh;
- `SEMANTIC_TAG_MTEXT_HEIGHT_DRIFT` — live `TextHeight` differs from `GeneratedSemanticTagTextHeightM` after drawing-unit conversion;
- `SEMANTIC_TAG_MTEXT_POSITION_DRIFT` — live `Location` differs from stored drawing-local WCS X/Y/Z;
- `SEMANTIC_TAG_MTEXT_ROTATION_DRIFT` — live rotation differs from stored tag rotation;
- `SEMANTIC_TAG_MTEXT_NORMAL_DRIFT` — P0 tag normal is no longer +Z.

The runtime service uses an open/read transaction only. It never erases, moves, refreshes, replaces or rewrites project state.

## Command behavior

```text
QS3DTAGHEALTH
```

- prints combined persisted + live issues;
- caps command-line issue output to avoid flooding the editor;
- selects resolvable affected tag handles so the user can inspect them;
- does not auto-repair.

Repair remains explicit:

```text
QS3DTAGREFRESH
QS3DTAGREMOVE
```

Use `QS3DTAGREFRESH` when semantic/rendered/live content or placement needs to be restored at the saved position. Use `QS3DTAGREMOVE` when the generated annotation should be physically erased and tag ownership metadata cleared.

`QS3DUNTRACK` is intentionally different: untrack preserves CAD geometry and semantic detach does not imply destructive tag deletion.

## Aggregation

`GeneratedSolidRuntimeHealthService` now aggregates:

- generated Solid3d XData runtime ownership health;
- native Grid annotation XData runtime health;
- native Semantic Tag MText runtime health.

`QS3DHEALTHALL` already consumes this aggregate runtime service. A separate latest-source audit found that current `QS3DRELEASECHECK` does not directly consume the aggregate service on the inspected head; that release integration remains a source regression to restore safely on the latest hot release blob. Do not claim live annotation runtime health is a Release Check blocker until that exact source connection is present and statically/runtime verified.

## Local V25 matrix

For exact-SHA licensed BricsCAD V25 qualification, include:

1. create a tag, run health — PASS;
2. save/reopen, run health — PASS;
3. manually edit MText contents — content drift;
4. manually move/rotate/change height — placement/style drift;
5. delete native MText — missing;
6. replace/corrupt the handle target or XData — type/ownership mismatch, no destructive repair;
7. `QS3DTAGREFRESH` after drift — health returns PASS;
8. `QS3DTAGREMOVE` — native MText erased and tag metadata cleared;
9. Undo/redo create/refresh/remove and verify project/native state remain coherent;
10. millimetre and metre drawings, Unicode text and Unicode file paths.

Source/static health coverage is not a substitute for these native runtime scenarios.
