# Native Semantic Element Table P0

Status: **source-implemented; BricsCAD V25 runtime qualification is still required**.

This slice advances issue #77 without inventing a fake `ProjectElement` owner for project-level documentation.

## Scope

Commands:

- `QS3DELEMENTTABLE` — create/replace the project-owned native `Table` at a picked ModelSpace point.
- `QS3DELEMENTTABLEREFRESH` — rebuild at the persisted WCS position from current semantic state.
- `QS3DELEMENTTABLEREMOVE` — erase only a positively owned native Table and clear its project metadata.
- `QS3DELEMENTTABLEHEALTH` — read-only persisted/native ownership, semantic-staleness and live Table shape/text/position checks.

The P0 table is the generic **QS3D Semantic Element Schedule** with columns `Id`, `Category`, `Family`, `Floor`, `Zone`. Rows are ordered by semantic element ID and are rendered through `SemanticDocumentationTableBuilder` / `SemanticTagRenderer`.

It is intentionally **not** a replacement for BQ, BBS, Door/Opening, Room Finish, Material Usage or other specialized authoritative schedule calculations.

## Ownership contract

A documentation Table is project-level state, not generated geometry belonging to an arbitrary semantic element. Therefore this slice does not use `GeneratedSolidHandle` and does not create a dummy semantic element.

The generated Table uses a dedicated `QS3DDOC` RegApp/XData marker containing:

1. ownership version;
2. exact project ID;
3. stable documentation ID `SemanticElementSchedule`;
4. document kind `SemanticElementTable`;
5. semantic snapshot fingerprint.

`ProjectState.Metadata` stores the generated handle, project owner, ownership version, fingerprint, WCS position, row count and column count. Partial metadata fails closed before destructive replacement/removal.

A live previous object is erased only when:

- the persisted handle resolves;
- it is a native `Table`;
- its `QS3DDOC` XData matches the project owner/document kind;
- its XData fingerprint matches the persisted fingerprint.

A foreign object, wrong live type or ownership mismatch is never erased.

## Refresh and rollback

Before CAD mutation, all semantic table rows are rendered and fingerprinted. Broken/ambiguous semantic references therefore fail before replacement.

Build/remove capture `ProjectStateSnapshot`. Semantic metadata/audit mutation occurs while the CAD transaction is still rollback-capable. `AuditTrail.ForProject(...).Record(...)` advances project revision through `ProjectState.Touch()`. If the CAD operation fails before commit, the semantic snapshot is restored.

Changing semantic values makes the persisted fingerprint stale. `QS3DELEMENTTABLEREFRESH` replaces the positively owned old Table using its old persisted fingerprint and writes a new fingerprint from the newly rendered semantic snapshot.

## Live native health

`GeneratedSemanticElementTableRuntimeHealthService` extends the persisted ownership/fingerprint checks with read-only live native validation. It verifies:

- native Table still resolves and remains positively `QS3DDOC`-owned;
- live row/column count matches the current semantic snapshot;
- title, headers and semantic cells match the bounded Core-rendered snapshot;
- live `Table.Position` still matches the persisted drawing-local WCS position.

Cell-detail output is bounded so a manually corrupted large Table cannot flood Health/Release output. The service opens native data read-only and never repairs, erases or rewrites Table content.

`QS3DELEMENTTABLEHEALTH` consumes this full runtime service. The shared native runtime health aggregator also consumes it, and `QS3DRELEASECHECK` consumes that aggregator. Therefore live semantic Table ownership/content/shape/position drift is a release blocker while exact licensed V25 runtime qualification remains a separate gate.

## P0 space/unit boundary

P0 is deliberately restricted to:

- ModelSpace (`Database.TileMode == true`);
- UCS whose XY plane is parallel to WCS XY;
- drawing-local WCS insertion coordinates;
- unit-aware text/row/column sizes through `CadUnitService.MetersToDrawingUnits`.

PaperSpace/Layout placement, viewport-aware scales, title blocks and sheet lifecycle remain separate work. The command fails instead of silently placing a ModelSpace table from a PaperSpace point.

## Source gate

```text
python scripts/preflight-native-semantic-element-table.py
```

`preflight-all.py` discovers this gate automatically.

The gate checks the bounded Core renderer dependency, dedicated project-level ownership, rollback ordering, native Table API use, unit conversion, ModelSpace scope, read-only live drift diagnostics, runtime aggregation and Release Check wiring. It is static source validation only.

## Required local BricsCAD V25 qualification

Run on a clean working copy and record the exact 40-character source SHA.

1. Build `Release|x64` against the installed BricsCAD V25 managed assemblies.
2. `NETLOAD` the built plugin and confirm all four commands register exactly once.
3. In a ModelSpace DWG with valid semantic elements, run `QS3DELEMENTTABLE`; verify a real selectable/editable BricsCAD `Table`, not exploded text/lines.
4. Verify Vietnamese/Unicode values, long Family/Floor/Zone values and row counts near realistic project sizes.
5. Test `INSUNITS` millimeter and meter drawings; visual table size should remain physically consistent.
6. Test World UCS and a rotated planar UCS. Confirm the picked point is stored/used in WCS. Confirm tilted/3D UCS fails closed.
7. Change semantic Family/Floor/Zone data. `QS3DELEMENTTABLEHEALTH` should report semantic stale before refresh; `QS3DELEMENTTABLEREFRESH` should update the native Table at the stored position.
8. Edit the live native Table directly: change title/header/data text, insert/remove a row/column and move the Table. Health and Release Check should report the corresponding live shape/text/position drift without repairing the Table.
9. Run refresh repeatedly without semantic changes and verify one owned live Table remains.
10. On a disposable copy, replace/corrupt the persisted handle target or `QS3DDOC` XData; refresh/remove must fail without erasing the foreign object.
11. Delete the owned Table manually; health must report missing. Refresh should recreate it from persisted project state without deleting unrelated CAD.
12. Run `QS3DELEMENTTABLEREMOVE`; verify only the owned Table is erased and metadata is cleared.
13. Save, close, reopen, then repeat health/refresh/remove. Confirm the persistent handle and metadata survive correctly.
14. Open two DWGs and alternate commands; verify project ownership never crosses documents.
15. Enter a Layout/PaperSpace and confirm P0 build/refresh refuses the unsupported context rather than creating misplaced content.
16. Run `QS3DRUNTIMECHECK`, `QS3DSUPPORTBUNDLE` and the normal local V25 qualification suite; archive only sanitized logs/screenshots.

Record results in the canonical local handoff (`docs/LOCAL-V25-QUALIFICATION.md` / remaining-local issue log). Do not commit private DWGs, proprietary BricsCAD DLLs or customer data.

## Still open for issue #77

- specialized native Table adapters for authoritative BQ/BBS/Room Finish/Door-Opening/Material schedules;
- Table style/standards presets and controlled column sizing beyond the P0 defaults;
- deeper live formatting/style drift diagnostics beyond shape/text/position/ownership;
- persisted first-class SemanticSchedule definitions if product requirements need multiple user-defined schedules;
- Layout/Sheet/Viewport/title-block lifecycle and PaperSpace scales;
- exact V25 save/reopen/Undo/Unicode/HiDPI/multi-DWG qualification.
