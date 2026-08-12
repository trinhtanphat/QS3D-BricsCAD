# Work claim — Project Browser workspace revision atomicity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:20:00+07:00`
- Baseline main SHA: `1ee9cd3d18c30a9549ee056e3ccff838bc4d8981`
- Priority: P1 — deterministic Core mutation atomicity at the project revision boundary.

## Reserved scope

Fix `ProjectBrowserWorkspaceStateStore.Save()` and `Clear()` so a `ProjectState.Touch()` overflow cannot occur after workspace metadata has already been mutated. Preserve current validation, serialized format, no-op behavior, and successful revision semantics.

## Expected surfaces

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs` — mutation ordering only for persisted workspace save/clear.
- Focused Core smoke regression proving overflow leaves metadata, `ChangeVersion`, and `UpdatedUtc` unchanged, while successful save/clear still advance the revision exactly once.

## Excluded scope

- Workspace XML schema/canonicality, query/grouping/primary-id rules, collection bounds, query/selection/virtualization planners, browser UI, V25/V26 adapter/runtime behavior.
- Semantic Schedule definition bounds, formula parsing/reference behavior, release/package work, and any currently ACTIVE/BLOCKED neighboring claim.
- GitHub Actions or licensed BricsCAD qualification.

## Validation plan

- Re-fetch current `main` and reserved blobs before editing.
- Add source-level ordering guard by reserving the project revision before metadata mutation.
- Add a deterministic `long.MaxValue` project-version regression using persisted Core state, plus normal save/clear revision checks.
- Review exact commit diff and verify the claim/implementation remain ancestors of moving `main`.
- Do not dispatch GitHub Actions and do not claim BricsCAD runtime PASS.

## Coordination

Recent browser-workspace canonicality lanes are historical/completed and are excluded. Current ACTIVE lanes observed for Semantic Schedule definition bounds and formula reference-token parity are explicitly non-overlapping.

## Completion condition

Focused source + regression are pushed to `main`, the claim is marked `COMPLETED` with exact commit evidence, and no LOCAL_ONLY gate is introduced because this is deterministic `QS3D.Core` state behavior.
