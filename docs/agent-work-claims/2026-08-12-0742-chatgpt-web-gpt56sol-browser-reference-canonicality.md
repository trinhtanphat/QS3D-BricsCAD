# Work claim — Project Browser reference canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-browser-reference-canonicality-20260812-0742`
- Registered: `2026-08-12T07:42:00+07:00`
- Baseline main SHA: `53d6a8e3148c33ba3c9f719799dd77df9d6dd51a`
- Priority: P2 — keep Browser grouping from silently normalizing semantic relation state that QSDB persistence rejects.

## Reserved scope

`ProjectElement.FloorId` and `ZoneId` are mutable. `QsdbProjectStore.ValidateProject(...)` rejects non-empty relation IDs with surrounding whitespace, but `ProjectBrowserPlanner.ValidateReferences(...)` trims both IDs and the grouping paths also trim them. A directly mutated `" F1 "` / `" Z1 "` relation is therefore silently accepted and displayed as valid Browser structure even though the same project cannot be persisted canonically.

## Reserved surfaces

- `src/QS3D.Core/Navigation/ProjectBrowserPlanner.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserReferenceCanonicalitySmoke.cs` (new focused module-initializer regression)
- this claim file

## Intended fix

- Fail closed when a non-empty element `FloorId` or `ZoneId` has leading/trailing whitespace before Browser grouping/reference lookup.
- Preserve empty/unassigned relations, case-insensitive valid reference lookup, grouping/order/count semantics, element/floor/zone capacity guards and unrelated Project Browser workspace/query files.
- Add focused smoke coverage for padded floor/zone references and ordinary canonical references.

## Explicit coordination

The concurrent Browser workspace container-order claim owns `ProjectBrowserWorkspaceStateStore.cs`; this lane does not touch that file or its smoke surfaces.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions dispatch; no BricsCAD V25 runtime PASS claimed.
