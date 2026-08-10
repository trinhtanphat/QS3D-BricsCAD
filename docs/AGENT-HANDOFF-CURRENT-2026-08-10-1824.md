# QS3D-BricsCAD — current agent handoff (2026-08-10 18:24 UTC+7)

**Repository:** `trinhtanphat/QS3D-BricsCAD`  
**Canonical branch:** `main`

This is the newest short source delta. Fetch `main` before every write. Current source wins if this note becomes stale.

Read `AGENTS.md`, `docs/PRODUCT-BOUNDARY.md`, `docs/REMOTE-AGENT-SCOPE.md` and `CI_POLICY.md` before substantive work. Real BricsCAD V25/Windows/private-DWG/signing/performance proof remains `LOCAL_ONLY`; remote/static evidence never creates `LOCAL_PASS`.

## 1. Semantic interchange — read-only export + strict read-only validation

`QS3DINTERCHANGEJSON` is genuinely read-only end-to-end:

- Save dialog occurs before project acquisition;
- `ProjectStateSnapshot.CreateDetachedCopy(...)` deep-copies the live state;
- dirty semantic regeneration runs only on the detached copy;
- the exporter temporary-file/replace boundary writes only the detached snapshot;
- live project object references, dirty flags, quantities and timestamps are not replaced/restored/mutated by export.

`QS3DINTERCHANGEVALIDATE` validates an external `QS3D.SemanticSnapshot` v1 without constructing/replacing a live `ProjectState` and without touching DWG entities. Current fail-closed contract includes:

- strict UTF-8 file decoding (`JSON_UTF8` on invalid byte sequences);
- exact format/version and SI units;
- required `zones` / `floors` / `families` / `elements` collections;
- required project/catalog names and required Family/element container fields;
- stable-ID/reference/category/source-scope checks;
- generated/native ownership-smuggling rejection;
- bounded values/counts and finite numbers;
- iterative/Kahn dependency-cycle detection to avoid recursive stack growth.

Project Tools and the Project Ribbon expose both export and read-only validation. `QS3DINTERCHANGEVALIDATE PASS` is **not** permission to import. JSON import/merge/round-trip, identity collision, current-DWG rebinding, ownership reconstruction, migration/rollback and IFC/Revit/BCF/cloud formats remain open design work.

Read `docs/INTERCHANGE-JSON.md` and preserve `scripts/preflight-interchange-json.py` / `scripts/preflight-interchange-validation.py`.

## 2. Grid/reference workflow — Capture → Number → Annotate

Current guarded commands:

- `QS3DGRID` — finite positive LINE/ARC semantic capture;
- `QS3DGRIDNUMBER` — explicit-order numeric/alphabetic semantic numbering;
- `QS3DGRIDANNOTATE` — replace owned native endpoint annotation for selected tracked Grid sources;
- `QS3DGRIDANNOTATEALL` — replace annotation for all labeled semantic Grids.

Project Ribbon discoverability for all four actions is statically guarded.

Core/reference support includes:

- `GridNamingService` / `GridNamingHealthService`;
- bounded `GridIntersectionPlanner` for finite LINE×LINE, LINE×ARC and ARC×ARC geometry;
- `GridSpatialOrderingPlanner` for a bounded **parallel LINE Grid family** using an explicit 2D ordering axis, with fail-closed non-parallel/ambiguous/ARC cases;
- comprehensive health integration.

`GridSpatialOrderingPlanner` is only a CAD-independent planning primitive. It does not choose the axis, extract V25 geometry, group systems, or silently replace the explicit-review workflow. Mixed LINE+ARC/radial ordering still requires a separate reviewed policy.

Native annotation source includes:

- extension + Circle bubble + DBText at each endpoint;
- QS3D XData ownership and `GeneratedGridAnnotationHandles`;
- ownership-checked replacement;
- one native batch transaction plus semantic snapshot rollback;
- persisted generated-annotation health;
- source-plane-aware geometry: ARC uses its native normal; a 3D-sloped LINE fails closed instead of being projected silently.

Live V25-side Grid annotation health is also source-implemented through `GeneratedGridAnnotationRuntimeHealthService` and is aggregated into the existing runtime health path. It read-only checks persisted annotation handles against live CAD entity existence, deterministic `Line/Circle/DBText` slot type, matching QS3D XData ownership and current semantic `GridLabel` text. It never repairs/erases mismatched CAD.

Do **not** call native Grid annotation V25-certified yet. Exact-SHA local proof is still required for actual geometry/text alignment, live-health behavior, Undo/Redo, save/reopen, multi-DWG, Unicode/HiDPI and ownership mismatch behavior.

Still open: native V25 geometry extraction/review UI around Core spatial ordering/intersections, mixed/radial/general ordering policy, rectangular/radial system authoring, associative constraints/dimensions/intersection markers, Direct Draw Grid/repeat UX, automatic structure hosting/snapping and paper-space annotation lifecycle.

Read `docs/GRID-WORKFLOW.md`, `docs/GRID-INTERSECTIONS.md`, `docs/GRID-SPATIAL-ORDERING.md` and `docs/GRID-NATIVE-ANNOTATION.md`.

## 3. Level/reference boundary

Reuse existing `FloorDefinition` as the Level model. Do not introduce a second Level store.

Bottom/Top Level IDs + offsets and `ElementVerticalPlacementService` exist in Core. Valid Level references still intentionally produce `LEVEL_REFERENCE_NATIVE_INTEGRATION_PENDING` because `LevelReferenceNativeIntegrationPolicy` keeps categories unqualified until native host solids + dependent opening/Curtain/rebar/quantity placement consume the same resolver and exact-V25 proof exists.

Do not remove this release gate merely because semantic references validate.

## 4. Source edit / semantic atomicity

`QS3DSYNCSOURCE` is the guarded authoritative-source reconcile path after native CAD edits. It rejects generated/ambiguous ownership, snapshots semantic state, invalidates owned downstream output while native rollback is available, refreshes source-derived state and regenerates dependency closure. Post-commit UI work stays best-effort.

Core `RegenerationEngine.RegenerateDirty(...)` / subset regeneration use project-snapshot rollback. Preserve this atomicity when adding regenerators.

Do not compose source reconcile + native rebuild and call it one atomic operation without a shared transaction/recovery design.

## 5. Native/generated work truthfulness

Generated replacement families must preserve:

- canonical ownership slots/XData;
- project snapshot before cross-layer mutation;
- semantic ownership/audit publication before native commit while rollback remains possible;
- ownership verification before erasing a live generated entity;
- post-commit Palette/Editor refresh as non-fatal UI work.

`QS3DCURTAIN3D` remains a multi-phase **PARTIAL COMMIT** orchestration until a shared native transaction or persisted compensation/recovery journal is implemented and proven locally.

Standard-specific fabrication-grade rebar remains blocked on an explicitly selected engineering standard/revision and approval evidence.

## 6. Documentation layer

Core semantic documentation foundations exist:

- `SemanticTagRenderer` provides bounded semantic-ID/property/quantity-driven labels while blocking generated/native ownership fields;
- `SemanticDocumentationTableBuilder` provides explicit-order bounded generic semantic table snapshots and reuses the tag renderer rather than becoming a second BQ/BBS calculation engine;
- public table `Rows`, `Headers` and row `Cells` are defensively copied into read-only collections, so caller-owned mutable lists or collection casts cannot rewrite a previously built snapshot.

Native BricsCAD MText/MLeader/Table/Layout/Viewport lifecycle is not complete. Keep issue #77 open until native ownership/replacement/stale/update and runtime workflows exist.

## 7. Local-only / policy gates — do not re-audit remotely

Already parked for local/policy-capable agents:

- exact-SHA V25 adapter build + NETLOAD/DemandLoad + interactive runtime;
- private-DWG save/reopen/multi-DWG;
- real WPF/Ribbon/Unicode/HiDPI/editor behavior;
- DrawJig/repeat/OSNAP/ORTHO/ESC/UNDO;
- coherent native Level placement;
- Curtain whole-command recovery/panel glass;
- physical L/T/X/Multi wall-solid reconciliation;
- production Authenticode/timestamp installer/updater;
- commercial license enforcement until owner supplies actual SKU/seat/trial/binding/offline/key-rotation policy;
- engineering-standard-specific fabrication behavior;
- real large-model performance profiling.

Use `docs/LOCAL-V25-QUALIFICATION.md` and the current local handoffs. Never manufacture `LOCAL_PASS` from source inspection.

## 8. Open issue truth

- #79 Grid/reference: substantially advanced by capture, numbering/naming health, finite intersections, guarded parallel-LINE spatial-order planning, owned native annotation and read-only live annotation health; native extraction/review UI, mixed/radial/general ordering, constraints/hosting and Level-native work remain.
- #84 Interchange: substantially advanced by detached read-only export + strict read-only validator; mutation/import/round-trip and broader formats remain.
- #77 Documentation: Core renderer/table foundation is bounded and immutable; native annotation/table/sheet workflows remain.
- #72/#73/#74/#75/#76/#81/#82/#83 retain their runtime/advanced-geometry/signing/engineering/performance scopes unless current source proves a smaller source-safe subtask is still open.

## 9. CI / release

GitHub Actions remain manual-only. `continue all`, commits, docs, preflights, PR review or local handoff preparation do **not** authorize workflow dispatch/rerun or GitHub Release publication. A separate explicit owner request is required.
