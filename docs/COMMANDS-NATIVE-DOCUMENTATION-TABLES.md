# Native Documentation Table Commands

Updated: 2026-08-10 (UTC+7)

This addendum is part of the QS3D command catalog while `docs/COMMANDS.md` remains a hot multi-agent file. These are BricsCAD V25 plugin commands, not standalone executables. All native Table operations remain subject to exact-SHA licensed BricsCAD V25 runtime qualification.

## Generic semantic element Table

- `QS3DELEMENTTABLE` — create/replace the project-owned generic semantic element native `Table` at a picked ModelSpace point.
- `QS3DELEMENTTABLEREFRESH` — rebuild at its persisted drawing-local WCS position.
- `QS3DELEMENTTABLEREMOVE` — erase only the positively owned generic Table and clear project metadata.
- `QS3DELEMENTTABLEHEALTH` — read-only metadata/ownership/fingerprint plus live shape/text/position health.

The generic Table uses bounded `SemanticDocumentationTableBuilder` output. It is not an authoritative BQ/BBS/Room Finish/Door/Material calculation replacement.

## BQ authoritative Table

- `QS3DBQTABLE` — regenerate dirty semantic quantities and create/replace a native BQ Table from `ProjectQuantityReportBuilder` rows.
- `QS3DBQTABLEREFRESH` — regenerate dirty semantic quantities and rebuild at the persisted WCS position.
- `QS3DBQTABLEREMOVE` — erase only the positively owned BQ Table artifact and clear project metadata.
- `QS3DBQTABLEHEALTH` — read-only BQ fingerprint/live native health plus a dirty-semantic warning; it never regenerates.

The native BQ Table mirrors all 19 `XlsxQuantityExporter` columns, including `QS3D Element ID`, `CAD Handle (hex)` and `QS3D Drawing Fingerprint` traceability.

## Door / Opening authoritative Table

- `QS3DDOOROPENINGTABLE` — create/replace from `DoorOpeningScheduleBuilder` rows.
- `QS3DDOOROPENINGTABLEREFRESH` — rebuild at the persisted WCS position.
- `QS3DDOOROPENINGTABLEREMOVE` — erase only the positively owned Door/Opening Table artifact.
- `QS3DDOOROPENINGTABLEHEALTH` — read-only authoritative fingerprint and live native health.

## Room Finish authoritative Table

- `QS3DFINISHTABLE` — create/replace from `RoomFinishScheduleBuilder` rows.
- `QS3DFINISHTABLEREFRESH` — rebuild at the persisted WCS position.
- `QS3DFINISHTABLEREMOVE` — erase only the positively owned Room Finish Table artifact.
- `QS3DFINISHTABLEHEALTH` — read-only Room Finish fingerprint and live native health.

## Material Usage authoritative Table

- `QS3DMATERIALTABLE` — create/replace from `MaterialUsageScheduleBuilder` rows.
- `QS3DMATERIALTABLEREFRESH` — rebuild at the persisted WCS position.
- `QS3DMATERIALTABLEREMOVE` — erase only the positively owned Material Usage Table artifact.
- `QS3DMATERIALTABLEHEALTH` — read-only Material Usage fingerprint and live native health.

## Shared lifecycle

The four specialized schedules use `ProjectOwnedNativeTableArtifactService` and dedicated project-level `QS3DDOC` ownership. They do not create a dummy semantic element or reuse `GeneratedSolidHandle`.

Creation is ModelSpace-only. A newly picked point requires a planar UCS whose XY plane is parallel to WCS XY and is transformed to drawing-local WCS. Refresh uses persisted WCS and does not require the current UCS to match creation. Removal is ownership-scoped.

Runtime health is fail-isolated per provider and consumed by normal native Health plus `QS3DRELEASECHECK`. `.qsdb` persists project-level artifact metadata; portable Semantic Snapshot interchange deliberately excludes it. A clean source/project release check does not replace licensed V25 compile/NETLOAD/save-reopen/Undo/Unicode/HiDPI/multi-DWG qualification.

Related docs:

- `docs/NATIVE-SEMANTIC-ELEMENT-TABLE-P0.md`
- `docs/NATIVE-BQ-TABLE-P0.md`
- `docs/NATIVE-DOOR-OPENING-TABLE-P0.md`
- `docs/NATIVE-ROOM-FINISH-TABLE-P0.md`
- `docs/NATIVE-MATERIAL-USAGE-TABLE-P0.md`
- `docs/SEMANTIC-TAGS.md`
