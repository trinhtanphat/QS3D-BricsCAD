# User-defined semantic schedules

Status: `SOURCE_IMPLEMENTED` for persisted Core definitions and deterministic rendering.

`SemanticScheduleCatalog` adds first-class persisted **user-defined semantic schedule** definitions without creating a second quantity/reporting engine.

## Definition

Each schedule stores:

- stable schedule ID and user-visible name/title;
- optional Element categories;
- optional Floor/Zone filter;
- optional include/exclude Element ID lists;
- 1..32 semantic documentation columns using the existing tag-template language.

Definitions are persisted in project metadata under `QS3D.Documentation.SemanticSchedules.v1` and therefore travel with `.qsdb` project state.

The collection snapshots exposed by `SemanticScheduleDefinition` are **defensively immutable**. Callers cannot cast the public category/include/exclude/column lists back to a mutable list and change a definition behind the catalog's validation boundary.

## Rendering

`SemanticScheduleCatalog.Build(...)` resolves the current semantic project, applies category/Floor/Zone/include/exclude filters, orders selected Elements deterministically by Element ID, then delegates cell rendering to the existing `SemanticDocumentationTableBuilder`.

A valid definition whose current filters match zero Elements produces a **header-only** `SemanticDocumentationTable` with zero rows. Zero current matches are a normal model state, not a malformed schedule definition. The legacy/default documentation table API still requires at least one row unless a caller explicitly opts into the empty-table path.

Header-only output does not weaken template validation. `SemanticDocumentationTableBuilder` validates every column through `SemanticTagRenderer.ValidateTemplate(...)` before row resolution, so malformed template syntax, unsupported tokens and attempts to expose blocked **generated/native ownership** properties fail closed even when the schedule currently has zero rows. Row-dependent missing property/quantity values remain valid empty cells, matching normal non-empty schedule behavior.

Malformed semantic model state is different: if the project's Element collection contains a **null semantic Element**, custom schedule rendering fails closed rather than silently skipping it. Explicit stale Floor/Zone/include/exclude references also continue to fail closed.

The schedule layer does not calculate BQ and does not calculate BBS; it also does not regenerate semantic quantities. `{Q:...}` columns only display quantity values already present on the semantic Element. Authoritative BQ, BBS, Door/Opening, Room Finish and Material calculators/schedules remain separate domain sources.

This prevents a custom schedule from silently becoming a competing quantity engine.

## Persistence and CRUD

The catalog supports deterministic `Load`, `Save`, `Upsert` and `Remove` operations.

Persistence is bounded:

- maximum 128 schedule definitions;
- maximum 5000 include/exclude IDs per definition;
- maximum 32 columns;
- maximum 1 MiB XML metadata payload;
- unique schedule IDs and names, case-insensitive;
- unique column headers;
- no overlap between include and exclude ID lists;
- DTD processing prohibited and external XML resolution disabled.

Saving identical normalized content does not touch the project change version again.

## Model Health

`SemanticScheduleHealthService` is a **read-only** Core diagnostic provider and is included in `ComprehensiveModelHealthService`.

It reports bounded `SEMANTIC_SCHEDULE_*` issues for:

- malformed/corrupt persisted schedule catalog data;
- missing or ambiguous Floor/Level references;
- missing or ambiguous Zone references;
- missing or ambiguous include/exclude Element IDs;
- invalid column templates, including blocked generated/native ownership properties;
- diagnostic truncation if the bounded issue cap is reached.

The provider builds case-insensitive identity-count indexes once, then checks schedule references without repeatedly scanning the whole project. It does not `Touch` the project, save/upsert/remove schedule definitions or rewrite metadata.

A valid **zero-match** schedule is not a Model Health error. Zero selected rows remain a normal current-model result while stale explicit identities, invalid templates and corrupt catalog data are errors.

## Stale references

Persisted definitions may outlive model edits. Rendering therefore fails closed when an explicitly referenced Floor/Zone or include/exclude Element no longer exists. The renderer does not silently retarget a schedule to another object with a similar name.

This stale-reference rule is intentionally separate from a zero-match result. A schedule can legitimately match no current rows while all explicit references remain valid.

## Portable interchange boundary

The schedule catalog is project documentation metadata, not portable semantic Element ownership. Current portable interchange intentionally excludes project metadata, so custom schedule definitions do not silently become cross-DWG interchange authority.

A future explicit schedule-export/import format would need its own version/collision policy.

## Native boundary

This Core feature does not automatically create a native BricsCAD Table, Layout, Viewport or title block. A future native custom-schedule command should render the authoritative `SemanticDocumentationTable` result into a project-owned native artifact using the same ownership/health/rollback conventions as existing schedule tables.

Native Table style/layout, PaperSpace placement, Undo/save-reopen/Unicode/HiDPI and exact-SHA BricsCAD V25 qualification remain separate runtime work.
