# Native BBS Table P0

Status: **source-implemented in this slice; licensed BricsCAD V25 runtime qualification is still required**.

This slice advances the native documentation layer by projecting the existing authoritative QS3D Bar Bending Schedule into a project-owned BricsCAD `Table`. It deliberately reuses `ProjectRebarScheduleBuilder`; it does not invent a second rebar schedule model.

## Commands

- `QS3DBBSTABLE` — regenerate dirty semantic state, render the authoritative BBS and create/replace the project-owned native Table at a picked ModelSpace point.
- `QS3DBBSTABLEREFRESH` — regenerate dirty semantic state and rebuild at the persisted drawing-local WCS position.
- `QS3DBBSTABLEREMOVE` — erase only a positively owned BBS Table and clear its persisted table metadata.
- `QS3DBBSTABLEHEALTH` — read-only ownership/staleness/live-CAD/dirty-state diagnostics.

The Schedule Hub exposes the same lifecycle next to the existing `QS3DBBS` and `QS3DBBSCSV` actions.

## Authoritative data source

`BbsNativeTableBuilder.BuildSnapshot(project)` calls `ProjectRebarScheduleBuilder.Build(project)`. Therefore the DWG Table and existing BBS CSV/UI flows share the same schedule calculations and validation boundary.

The P0 Table contains 15 columns:

1. semantic element ID;
2. Bar Mark;
3. Shape code;
4. rebar notation;
5. diameter (mm);
6. quantity;
7. cutting length (m);
8. total length (m);
9. unit weight (kg/m);
10. net weight (kg);
11. waste percent;
12. total weight (kg);
13. fabrication status;
14. fabrication standard code;
15. detailing revision.

Invalid/overflowing rebar inputs continue to fail in the existing Core BBS builder before CAD replacement. The adapter additionally refuses an empty BBS and non-finite/negative numeric cells.

## Project-level ownership and rollback

BBS documentation is project-level generated documentation, not geometry owned by an arbitrary rebar semantic element. It uses the shared `ProjectOwnedNativeTableArtifactService` and its `QS3DDOC` XData contract.

The stable artifact identity is:

```text
documentId     = RebarBbsSchedule
documentKind   = RebarBbsTable
metadataPrefix = GeneratedBbsTable
```

The shared service persists Handle, project owner, ownership version, semantic snapshot fingerprint, WCS position and row/column counts. Replacement/removal erases a live entity only after handle, native `Table` type, project/document ownership and fingerprint agree. Foreign/wrong-type/ambiguous state fails closed.

Project metadata/audit changes occur while the CAD transaction is still rollback-capable. A pre-commit CAD failure restores `ProjectStateSnapshot`; rollback failure surfaces both errors.

## Freshness and health

Build/refresh run the normal Core `RegenerationEngine.RegenerateDirty(project)` before rendering BBS rows. Health remains read-only and does not regenerate or create project state.

`BbsNativeTableBuilder.Inspect` delegates to the shared native Table health contract and adds a BBS-specific dirty warning when a scheduled `RebarNotation` element remains dirty while a generated BBS Table exists.

The shared inspection verifies persisted metadata, current authoritative fingerprint, live handle/type, `QS3DDOC` ownership, table shape, cell text and stored WCS position. `GeneratedSolidRuntimeHealthService` consumes the BBS provider through the existing fail-isolated runtime health aggregation, so `QS3DHEALTHALL` / `QS3DRELEASECHECK` can surface its native documentation drift through their existing runtime path.

Fabrication qualification remains a separate authoritative Health/Release concern. Displaying a fabrication status/standard/revision in the Table is not engineering approval and does not upgrade unqualified rebar to fabrication-grade output.

## Space and UCS boundary

P0 creation is limited to ModelSpace. A newly picked point requires a UCS whose XY plane is parallel to WCS XY; the selected point is transformed and persisted in drawing-local WCS.

Refresh uses the already stored WCS position and therefore does not require the current UCS to match the UCS used during initial placement. PaperSpace/Layout, title-block/sheet placement and viewport-aware scale remain later native documentation work.

## Static source guard

```text
python scripts/preflight-bbs-native-table.py
```

`preflight-all.py` discovers this guard automatically. It verifies authoritative BBS reuse, all 15 schedule fields, lifecycle commands, semantic regeneration before build/refresh, shared `QS3DDOC` ownership/rollback/live-drift health, runtime aggregation, Release Check consumption and Schedule Hub discoverability.

This is static source validation only.

## Required local BricsCAD V25 qualification

On a clean exact-source working copy with licensed BricsCAD V25 x64:

1. build `Release|x64` against the installed V25 managed assemblies;
2. `NETLOAD` and confirm the four BBS Table commands register exactly once;
3. create BBS input covering count notation, spacing notation, waste, shape code and fabrication provenance;
4. run `QS3DBBSTABLE` and verify one real selectable BricsCAD `Table` with all 15 columns;
5. compare representative rows against `QS3DBBS` / `QS3DBBSCSV` from the same project state;
6. test Vietnamese/Unicode text, long marks/codes and realistic row counts;
7. test millimeter and meter `INSUNITS`; physical table sizing should remain coherent;
8. test World UCS and a planar rotated UCS for new placement, then refresh from a different UCS and verify the stored WCS position is preserved;
9. edit a scheduled rebar semantic input and verify Health reports stale/dirty until refresh;
10. manually edit a live Table cell/position/shape and verify read-only health reports CAD drift;
11. corrupt/replace the persisted handle target or `QS3DDOC` ownership on a disposable copy; refresh/remove must not erase foreign CAD;
12. delete the owned Table manually; Health must report missing and refresh must recreate it without touching unrelated objects;
13. save/close/reopen and repeat Health/Refresh/Remove;
14. alternate two DWGs and verify ownership/project state never crosses documents;
15. enter PaperSpace/Layout and verify P0 creation refuses unsupported placement;
16. run `QS3DHEALTHALL`, `QS3DRELEASECHECK`, `QS3DRUNTIMECHECK` and normal exact-SHA V25 qualification; archive only sanitized evidence.

Do not commit private DWGs, licensed BricsCAD DLLs or customer data.

## Remaining documentation work

This slice does not complete issue #77. Still separate are Table style/standards presets, column-width policies beyond the shared P0 default, PaperSpace/Layout/Sheet/Viewport/title-block lifecycle, broader annotation/tag placement, and exact V25 save/reopen/Undo/Unicode/HiDPI/multi-DWG evidence.
