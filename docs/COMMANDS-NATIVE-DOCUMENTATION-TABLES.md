# Native Documentation Table Commands

Updated: 2026-08-10 (UTC+7)

This addendum is part of the QS3D command catalog while `docs/COMMANDS.md` remains a hot multi-agent file. These are BricsCAD V25 plugin commands, not standalone executables. All native Table operations remain subject to exact-SHA licensed BricsCAD V25 runtime qualification.

## Generic semantic element Table

- `QS3DELEMENTTABLE` — create/replace the project-owned generic semantic element native `Table` at a picked ModelSpace point.
- `QS3DELEMENTTABLEREFRESH` — rebuild the generic semantic element Table at its persisted drawing-local WCS position.
- `QS3DELEMENTTABLEREMOVE` — erase only the positively owned generic semantic element Table and clear its project metadata.
- `QS3DELEMENTTABLEHEALTH` — read-only metadata/ownership/fingerprint plus live shape/text/position drift health.

The generic table uses bounded `SemanticDocumentationTableBuilder` output and is **not** an authoritative BQ/BBS/Room Finish/Door/Material calculation replacement.

## Door / Opening authoritative Table

- `QS3DDOOROPENINGTABLE` — create/replace a native Door/Opening schedule `Table` from `DoorOpeningScheduleBuilder` rows.
- `QS3DDOOROPENINGTABLEREFRESH` — rebuild at the persisted WCS position from current authoritative Door/Opening schedule state.
- `QS3DDOOROPENINGTABLEREMOVE` — erase only the positively owned Door/Opening Table artifact and clear its project metadata.
- `QS3DDOOROPENINGTABLEHEALTH` — read-only authoritative fingerprint and live native shape/text/position/ownership health.

## Room Finish authoritative Table

- `QS3DFINISHTABLE` — create/replace a native HT_Phòng / Room Finish schedule `Table` from `RoomFinishScheduleBuilder` rows.
- `QS3DFINISHTABLEREFRESH` — rebuild at the persisted WCS position from current authoritative Room Finish state.
- `QS3DFINISHTABLEREMOVE` — erase only the positively owned Room Finish Table artifact and clear its project metadata.
- `QS3DFINISHTABLEHEALTH` — read-only Room Finish authoritative fingerprint and live native shape/text/position/ownership health.

## Material Usage authoritative Table

- `QS3DMATERIALTABLE` — create/replace a native Material Usage schedule `Table` from `MaterialUsageScheduleBuilder` rows.
- `QS3DMATERIALTABLEREFRESH` — rebuild at the persisted WCS position from current authoritative Material Usage state.
- `QS3DMATERIALTABLEREMOVE` — erase only the positively owned Material Usage Table artifact and clear its project metadata.
- `QS3DMATERIALTABLEHEALTH` — read-only Material Usage authoritative fingerprint and live native shape/text/position/ownership health.

## Shared lifecycle

The three specialized schedules use `ProjectOwnedNativeTableArtifactService` and dedicated project-level `QS3DDOC` ownership. They do not create a dummy semantic element or reuse `GeneratedSolidHandle`.

Creation is ModelSpace-only. A newly picked insertion point requires a planar UCS whose XY plane is parallel to WCS XY, then is transformed to drawing-local WCS. Refresh uses the persisted WCS position and therefore does not require the current UCS to match the creation UCS. Removal is ownership-scoped and can clean the tracked artifact without reinterpreting a new pick point.

Runtime health is fail-isolated per provider and is consumed by normal native Health plus `QS3DRELEASECHECK`. A clean source/project release check still does **not** replace the separate licensed BricsCAD V25 compile/NETLOAD/save-reopen/Undo/Unicode/HiDPI/multi-DWG qualification gate.

Related docs:

- `docs/NATIVE-SEMANTIC-ELEMENT-TABLE-P0.md`
- `docs/NATIVE-DOOR-OPENING-TABLE-P0.md`
- `docs/NATIVE-ROOM-FINISH-TABLE-P0.md`
- `docs/NATIVE-MATERIAL-USAGE-TABLE-P0.md`
- `docs/SEMANTIC-TAGS.md`
