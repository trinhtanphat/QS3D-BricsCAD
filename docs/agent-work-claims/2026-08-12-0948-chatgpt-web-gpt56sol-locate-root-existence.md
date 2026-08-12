# Work claim — Locate requested-root existence integrity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:48:00+07:00`
- Baseline main SHA: `4fd253b56a62576f9c9f7f99fe4ccf50fd847a1e`
- Priority: P1 Core Locate integrity during owner-requested `continue all`
- Task Key: `CORE-LOCATE-ROOT-EXISTENCE-INTEGRITY`

## Confirmed defect

`SourceHandleResolver.Resolve(...)` materializes requested semantic root IDs, validates root-enumeration freshness, and builds a fail-closed project element index. However, during traversal it silently `continue`s when a requested root ID is absent from that index. A stale or invalid semantic selection can therefore be converted into an empty/partial Locate result instead of surfacing that the explicitly requested semantic element no longer exists.

This differs from the same resolver's missing-dependency behavior and from other semantic planning boundaries that reject explicitly requested missing element IDs. The defect is limited to caller-requested roots; traversal-derived Room provenance semantics are not part of this lane.

## Reserved scope

- `src/QS3D.Core/Services/SourceHandleResolver.cs`
- one focused auto-registered Core smoke for requested-root existence
- this claim file for close-out

## Contract

- after root materialization/freshness and full project identity indexing, every nonblank requested root must resolve in the current project;
- reject a missing requested root before handle traversal/partial result construction;
- preserve blank-root filtering, current trim/case-insensitive root lookup behavior, root input bounds and enumeration freshness;
- preserve dependency validation, Auto Room traversal, boundary/generated-owner fallback and valid direct-handle resolution;
- keep resolver read-only and do not broaden into UI PICKFIRST or native BricsCAD behavior.

## Validation plan

Add deterministic ModuleInitializer smoke coverage for a single missing root, a mixed valid+missing root set, a valid direct-handle root, and zero persistence mutation on rejection. Re-fetch source before write and inspect exact pushed diffs. No GitHub Actions dispatch, executable .NET smoke/build PASS, or licensed BricsCAD V25/V26 runtime qualification will be claimed unless actually executed.
