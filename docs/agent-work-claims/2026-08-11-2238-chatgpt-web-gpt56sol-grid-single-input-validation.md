# Work claim — single-grid intersection input validation

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T22:38:00+07:00`
- Baseline main SHA: `29bbffe36ad42d000ad93c573bff36b7c49166d9`
- Priority: evidence-driven remote-safe Core regression hardening

## Confirmed defect

`GridIntersectionPlanner.FindIntersections` materializes input and returns an empty intersection set immediately when fewer than two curves are supplied. Its per-curve `Validate` loop runs only after that return. As a result, a single malformed Grid reference (for example a degenerate LINE or a curve with non-finite geometry) is silently accepted as “no intersections”, while the same curve is rejected once a second curve is present.

## Reserved scope

Ensure every supplied Grid reference is validated before the planner's fewer-than-two-curves early return. Preserve empty-input behavior, pair intersection math, duplicate-id checks, intersection caps, identity planning, and all V25/native behavior.

## Expected surfaces

- `src/QS3D.Core/Geometry/GridIntersectionPlanner.cs`
- `tests/QS3D.Core.SmokeTests/GridIntersectionSingleInputValidationSmoke.cs`
- `tests/QS3D.Core.SmokeTests/GridIntersectionSingleInputValidationRegistration.cs`
- this claim file

## Excluded scope

- No Grid naming/renumbering/annotation, Grid identity tokens, browser/UI, source reconcile, native V25 adapters, reporting, quantity, persistence, rebar or project mutation changes.
- No changes to intersection formulas or tolerance defaults.
- No GitHub Actions dispatch.

## Validation plan

- Empty curve collection remains a valid empty intersection set.
- A valid single LINE remains a valid empty intersection set.
- A single degenerate LINE fails closed instead of returning empty.
- A single non-finite LINE fails closed instead of returning empty.
- Use a dedicated module initializer to avoid shared smoke registration contention.
- Re-fetch the target blob after claim publication and review exact pushed diffs/ancestry.
- No `dotnet` or BricsCAD V25 PASS will be claimed unless actually executed.

## Coordination

Recent Grid work in history targets naming/renumber/annotation lifecycle and is not an active reservation of `GridIntersectionPlanner.cs`; the planner's existing intersection smoke confirms malformed geometry is expected to fail closed when it reaches validation. This lane is limited to validation ordering for cardinality 0/1.

## Completion condition

All supplied Grid curves are validated consistently regardless of collection size, focused source regression coverage is registered on current `main`, concurrent work is preserved, and this claim is closed with exact commit SHAs.