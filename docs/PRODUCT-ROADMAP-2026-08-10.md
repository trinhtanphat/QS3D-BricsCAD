# QS3D-BricsCAD product roadmap — 2026-08-10

This roadmap separates source-complete work from BricsCAD V25 runtime qualification. A feature is not considered production-qualified merely because its Core contract exists.

## Product logic

QS3D keeps `ProjectState` as semantic source of truth. Native BricsCAD entities are either source geometry selected by the user or generated output with explicit provenance. Source handles and generated handles are drawing-local and must never become portable semantic identity. Semantic values remain SI internally; adapter/UI presentation may use millimetres for linear authoring while areas and volumes remain square/cubic metres.

The intended authoring loop is:

1. capture or directly author semantic elements;
2. edit family/instance/level/zone properties;
3. propagate dependencies and regenerate semantic quantities;
4. explicitly build/regenerate native geometry;
5. document the model through semantic views, tags, tables, schedules and sheets;
6. export/import through validated, versioned interchange contracts;
7. qualify the exact release SHA in licensed BricsCAD V25 before release.

## P0 — correctness and release gates

- Keep project persistence attached to the DWG save lifecycle without mutating an unrelated drawing/project context.
- Harden drawing-unit conversion at every native geometry boundary; Core remains in metres.
- Finish exact-SHA BricsCAD V25 qualification: Release build, NETLOAD/DemandLoad, save/reopen, multi-DWG isolation, generated ownership, opening boolean, rebar/MEP/QTO, Unicode/HiDPI and screenshot evidence.
- Keep semantic/native mutations atomic. On failure, restore semantic snapshots and do not retain partially-owned generated handles.
- Do not dispatch CI automatically. Repository Actions remain manually authorized release/qualification tools.

## P1 — BLT-style BIM authoring

- Property workflow: family/instance split, level selectors, source-derived read-only fields, millimetre display/edit conversion, multi-selection and bulk editing.
- Native modify workflow: select tracked native object -> resolve semantic owner -> edit through guarded transaction -> invalidate/regenerate dependencies -> refresh selection/properties.
- Grid/Floor/Zone: native grid creation and labels, richer level references, semantic snapping/constraints, floor/zone browser grouping and deterministic naming.
- Direct Draw: continue wall/beam/column/slab/opening/door/structural/foundation coverage with preview, cancel/rollback and project ownership guards.
- Project browser/ribbon: model tree, saved filters/views, context actions and visible dirty/generated-state diagnostics.

## P1 — documentation and deliverables

- Semantic tags and deterministic tables remain handle-free and fail closed on missing/ambiguous references.
- Saved semantic views filter by floor, zone, category and explicit semantic IDs with deterministic ordering.
- Sheet composition uses paper millimetres, validates view identity, rejects out-of-bounds placements and overlapping view rectangles before native materialization.
- Follow-on adapter work materializes approved semantic sheet plans into BricsCAD Layout/Viewport/title-block objects while keeping native object IDs drawing-local.
- Add sheet index, automatic schedule placement, view templates and title-block parameter mapping after the persistence contract is versioned.

## P1 — model QA and scale

- Turn model health checks into a rule-oriented QA gate covering duplicate IDs, missing/ambiguous references, orphan source/generated handles, stale generated outputs, invalid level placement and release blockers.
- Run the deterministic Core performance harness against representative 20k+ element models; store baseline evidence outside source and optimize only measured hotspots.
- Add targeted regeneration telemetry without coupling Core to BricsCAD APIs.

## P2 — detailing and interoperability

- Rebar: richer bar marks, schedule/detail layout, fabrication grouping and native annotation qualification.
- Interchange: complete append/KeepTarget/UseSource execution contracts with explicit identity/collision policy, schema migrations and no portable native handles.
- Add external integration adapters only on top of validated semantic DTOs; never let an importer bypass project-integrity validation.
- Evaluate BCF-style issue exchange and standards-based BIM exchange after semantic identity, level/grid and documentation contracts are stable.

## Acceptance gates

### Remote/source gate

A remote-safe feature can be merged when it has deterministic Core/source behavior, bounded inputs, case-insensitive identity handling where project IDs are case-insensitive, fail-closed ambiguity handling, smoke coverage and no hidden native-handle assumptions.

### Local BricsCAD V25 gate

A native feature is complete only after the exact commit is built against the licensed V25 SDK/runtime and the prescribed local qualification matrix produces real evidence. Source review, mocks and screenshots from another revision are not substitutes.

## Current implementation slice

The first documentation-planning slice adds pure-Core semantic saved-view and sheet-composition planners. It intentionally does not create Layout/Viewport objects and does not persist native IDs. This gives the BricsCAD adapter a deterministic, validated contract for later layout materialization without weakening the source/generated ownership boundary.
