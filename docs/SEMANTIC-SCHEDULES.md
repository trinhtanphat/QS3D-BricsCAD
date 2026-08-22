# User-defined semantic schedules

Status: `SOURCE_IMPLEMENTED` for persisted Core definitions, deterministic rendering, and BricsCAD V25 native custom-schedule Table lifecycle. Exact V25 runtime qualification remains `LOCAL_ONLY`.

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

A valid definition whose current filters match zero Elements produces a **header-only** `SemanticDocumentationTable` with zero rows. Zero current matches are a normal model state, not a malformed schedule definition. The native custom-schedule Table adapter preserves this contract by materializing title + header rows even when there are no data rows.

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

Persisted v1 XML is also a strict canonical contract. Every schedule must carry the canonical `id`, `name`, `title`, `floorId` and `zoneId` attributes and **exactly one canonical** `categories`, `include`, `exclude` and `columns` container, including empty containers when a filter/list is unused. Category values must use the exact, case-sensitive **canonical ElementCategory names** emitted by the serializer; numeric enum values, case-shifted names and padded category text fail closed. The loader rejects a noncanonical v1 payload instead of silently normalizing it and rewriting different XML on the next Save.

Saving identical normalized content does not touch the project change version again.

## Native BricsCAD Table lifecycle

The V25 adapter exposes four commands:

- `QS3DSCHEDULETABLE` — choose a persisted custom schedule, choose a ModelSpace insertion point, then create/replace that schedule's project-owned native Table;
- `QS3DSCHEDULETABLEREFRESH` — rebuild the chosen schedule at its persisted WCS position;
- `QS3DSCHEDULETABLEHEALTH` — read-only native ownership/content/position diagnostics;
- `QS3DSCHEDULETABLEREMOVE` — ownership-safe removal, including an orphan owner slot after its schedule definition has been deleted.

The Schedule Hub exposes the same four operations. Commands remain **selection/prompt first and mutation second**: they use read-only project state while the user chooses a schedule/point, then bind the canonical existing project only after the prompt succeeds. Build/refresh/remove verify the same `ProjectId` and `ChangeVersion` observed before prompting, so a model reload/edit while a prompt is open fails closed instead of mutating a changed project.

Each custom schedule gets an independent owner slot under `QS3D.Documentation.NativeSemanticScheduleTable.<schedule-token>.*`, where the token is a deterministic SHA-256 identity derived from the stable schedule ID. Native `QS3DDOC` XData binds the Table to ownership version, ProjectId, custom-schedule document kind, schedule ID and semantic snapshot fingerprint. Multiple custom schedules can therefore coexist and do not reuse or erase the fixed `SemanticElementSchedule` Table slot.

Create/refresh/remove take a semantic `ProjectStateSnapshot` before mutation and perform native replacement inside one BricsCAD transaction. A live previous object is erased only when its type and exact project/schedule/fingerprint ownership match persisted metadata; partial metadata or an ownership mismatch fails closed instead of deleting an arbitrary CAD object.

P0 native placement is deliberately bounded to ModelSpace. Build additionally requires a UCS whose XY plane is parallel to WCS XY before transforming the picked insertion point to WCS. PaperSpace/Layout/title-block behavior remains a separate sheet lifecycle rather than being silently approximated here.

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

The V25 native adapter separately reports `CUSTOM_SCHEDULE_TABLE_*` project-artifact issues for partial/corrupt owner metadata, deleted definitions, stale rendered fingerprints, missing/wrong native objects, XData ownership mismatch, shape/text drift and WCS position drift. These issues intentionally carry an empty semantic `ElementId`; `QS3DHEALTHALL` and `QS3DRELEASECHECK` locate the owned native Table handles instead of interpreting a schedule ID as a semantic Element ID.

## Stale references

Persisted definitions may outlive model edits. Rendering therefore fails closed when an explicitly referenced Floor/Zone or include/exclude Element no longer exists. The renderer does not silently retarget a schedule to another object with a similar name.

This stale-reference rule is intentionally separate from a zero-match result. A schedule can legitimately match no current rows while all explicit references remain valid.

## Portable interchange boundary

The schedule catalog is project documentation metadata, not portable semantic Element ownership. Current portable interchange intentionally excludes project metadata, so custom schedule definitions do not silently become cross-DWG interchange authority.

A future explicit schedule-export/import format would need its own version/collision policy.

## Qualification boundary

Native custom-schedule Table source is implemented, but source review is not a native runtime PASS. Exact-SHA BricsCAD V25 qualification still belongs to `docs/LOCAL-AGENT-INBOX.md` and must cover create/refresh/health/remove, multiple coexisting schedules, a header-only zero-match schedule, prompt cancel, stale prompt freshness, definition edits/deletion, save/reopen, Undo/Redo, Unicode, multi-DWG, ModelSpace/UCS behavior and native ownership drift.

Native Table style/layout, PaperSpace placement, title-block integration and exact-SHA BricsCAD V25 qualification remain separate runtime/product work.