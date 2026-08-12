# Work claim — Dependency health duplicate element identity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-dependency-duplicate-element-id`
- Registered: `2026-08-12T08:37:00+07:00`
- Baseline main SHA: `bbbca9ea8674dacef9a471aff2916eb1d044de1e`
- Priority: P1 — dependency health must fail visible when semantic graph identities are duplicated.
- Task Key: `CORE-DEPENDENCY-DUPLICATE-ELEMENT-ID`

## Confirmed defect

`DependencyHealthService.Inspect(...)` counts duplicate semantic element IDs and excludes those IDs from graph construction so traversal cannot bind an ambiguous node. However, it only emits `DEPENDENCY_TARGET_AMBIGUOUS` when another dependency points at the duplicate ID. A project containing two elements with the same ID and no dependency edge can therefore return zero dependency-health issues even though the graph identity is invalid.

## Non-overlap check

Existing Dependency Health lanes already completed null-element, blank/missing target, canonical relation, cycle/self-reference and ambiguous-target behavior. No recent claim/commit was found for surfacing the duplicate graph identities themselves.

## Reserved scope

- `src/QS3D.Core/Diagnostics/DependencyHealthService.cs`
- one focused Core smoke regression for duplicate element IDs
- this claim file

Do not modify dependency mutation APIs, graph scheduling/regeneration, unrelated model-health aggregation, persistence or BricsCAD runtime code.

## Intended contract

- Each duplicate semantic element ID produces deterministic dependency-health error evidence even when no element references that ID.
- Existing `DEPENDENCY_TARGET_AMBIGUOUS`, missing/blank/noncanonical/duplicate relation, self-cycle/cycle and null-element behavior remain unchanged.
- Inspection remains read-only and deterministic.
- No GitHub Actions/build/release dispatch and no BricsCAD V25 runtime PASS claim from this remote lane.

## Completion condition

Duplicate semantic graph identities are fail-visible without requiring an incoming dependency, focused Core smoke coverage pins the false-clean regression, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
