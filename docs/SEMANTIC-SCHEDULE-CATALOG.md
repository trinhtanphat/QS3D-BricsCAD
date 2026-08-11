# Semantic Schedule Catalog

Status: **LANDED_SOURCE candidate / Core-only**

This document describes the CAD-independent semantic schedule model used by QS3D documentation. It is intentionally separate from native BricsCAD `Table` ownership and from authoritative domain schedules such as BQ/BBS.

## Product intent

A user-defined semantic schedule is a persisted documentation definition made from three things:

1. a stable schedule ID/name;
2. one existing semantic View whose kind is `SemanticViewKind.Schedule`;
3. an ordered bounded set of documentation columns using the existing semantic tag template language.

The schedule does **not** own CAD handles, ObjectIds, Layouts, Viewports or native Table objects. It also does **not** recalculate BQ, BBS, rebar fabrication rules or other domain quantities.

## Core model

`SemanticScheduleDefinition` stores:

- `Id`;
- `Name`;
- `ViewId`;
- `Columns` (`SemanticDocumentationColumn`).

`SemanticSchedulePlanner` resolves `ViewId` against the same validated semantic View catalog used by Sheets. The referenced View must use `SemanticViewKind.Schedule`. Its deterministic `ElementIds` become the schedule row set.

Columns reuse `SemanticDocumentationColumnPolicy` and `SemanticDocumentationTableBuilder`, so the existing bounds remain authoritative:

- maximum 32 columns;
- unique case-insensitive headers;
- header length limit 96;
- template length limit 512;
- table row limit 5000.

Cell rendering still goes through `SemanticTagRenderer`. Therefore generated/native ownership properties remain blocked and a schedule cannot expose drawing-local runtime ownership as portable semantic content.

## Persisted catalog schema v2

`SemanticDocumentationCatalogStore.MetadataKey` remains `QS3D.Documentation.Catalog.v1` for storage compatibility, while the XML payload format is now **schema v2**.

Schema v2 adds a top-level `schedules` collection beside `views` and `sheets`:

```xml
<documentation version="2">
  <views>...</views>
  <sheets>...</sheets>
  <schedules>
    <schedule id="SCH-BEAMS" name="Beam Schedule" viewId="V-BEAMS">
      <columns>
        <column header="Mark" template="{P:Mark}" />
        <column header="Length" template="{Q:LengthM}" />
      </columns>
    </schedule>
  </schedules>
</documentation>
```

The reader accepts both version 1 and version 2. A valid version-1 payload loads with an empty schedule collection. The next successful catalog save serializes the project as version 2. Unsupported versions still fail closed.

The 1 MiB metadata payload bound and DTD/XML resolver protections remain in force.

## Catalog editing

`SemanticDocumentationCatalogEditor` now preserves schedules during every View/Sheet edit and adds:

- `UpsertSchedule`;
- `RemoveSchedule`;
- `ScheduleCount` in edit results;
- `RewrittenScheduleReferenceCount` in edit results.

View identity changes are dependency-aware. A View referenced by a schedule cannot silently change identity. `ReplaceView` has an overload with an explicit `rewriteScheduleReferences` decision. View removal likewise has an overload with an explicit `removeSchedules` decision.

This keeps schedule references fail-closed instead of leaving dangling IDs. Case-only View ID spelling changes can be normalized without changing semantic identity.

## Building a schedule table

`SemanticScheduleService.BuildTable(project, scheduleId)`:

1. loads the persisted documentation catalog;
2. resolves exactly one schedule ID case-insensitively;
3. rebuilds/validates the referenced semantic View against the current project;
4. passes the View's deterministic element IDs and persisted columns to `SemanticDocumentationTableBuilder`.

A valid schedule is allowed to render zero rows. This is important for user-defined schedules whose filters currently match nothing; the definition remains valid instead of disappearing merely because the model has no matching element at that moment.

## What this is not

This source slice is **không phải** native sheet generation, native schedule placement or a second quantity engine.

It does not implement or qualify:

- BricsCAD `TableStyle` behavior;
- Layout/PaperSpace creation;
- Viewport creation, scale or lock;
- title block insertion;
- MLeader associativity;
- native table placement/ownership;
- Undo/save-reopen behavior for a future schedule editor UI;
- any standard-specific engineering calculation.

The existing specialized BQ/BBS/Door/Room Finish/Material native Tables remain authoritative for their domain outputs. A generic SemanticSchedule must not be used to bypass those calculation paths.

## Validation coverage

Source coverage is wired into already-registered smoke suites:

- `SemanticDocumentationCatalogStoreSmoke` — v2 persistence, `.qsdb` round-trip, v1 read + v2 migration, invalid-reference atomicity;
- `SemanticDocumentationCatalogEditorSmoke` — CRUD plus explicit View/schedule reference rewrite/removal policy;
- `SemanticDocumentationTableSmoke` — deterministic persisted schedule rendering and Schedule-View requirement;
- `scripts/preflight-semantic-schedule-catalog.py` — architecture/source invariant guard, auto-discovered by `preflight-all.py`.

These files being present is not proof that they were executed in the current remote session.

## BricsCAD V25 qualification boundary

This feature is Core/source-safe. There is no new native BricsCAD command in this slice.

If a later adapter exposes schedule editing or native Table placement, exact-SHA licensed BricsCAD V25 qualification must separately cover cancel/confirm behavior, document affinity, Unicode, save/reopen, Undo, multi-DWG isolation, TableStyle/layout behavior and generated ownership. Until such evidence exists, do not label this feature `LOCAL_PASS` or claim native schedule authoring is complete.
