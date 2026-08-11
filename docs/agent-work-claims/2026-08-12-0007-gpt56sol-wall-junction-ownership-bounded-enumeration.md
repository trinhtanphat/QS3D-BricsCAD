# Work claim — Wall junction ownership bounded enumeration

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-wall-junction-ownership-bounded-enumeration-20260812-0007`
- Registered: `2026-08-12T00:07:00+07:00`
- Baseline main SHA: `e22ce35530a78df4a536c7d2bf1eeb908d91b593`
- Priority: evidence-driven Core resource-bound hardening during owner-requested `continue all`

## Reserved scope

Make `WallJunctionOwnershipPlanner.Plan` enforce its existing `MaxJunctions = 10000` and `MaxOwnerMappings = 20000` contracts while enumerating both `IEnumerable` inputs, rather than after unrestricted materialization.

## Expected surfaces

- `src/QS3D.Core/Geometry/WallJunctionOwnershipPlanner.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defects

`Plan` currently calls both `junctions.ToList()` and `ownerMappings.ToList()` before checking their declared limits. Either source can therefore be huge or non-terminating and consume unbounded resources before the 10,000-junction / 20,000-mapping batch guards execute.

## Explicit exclusions

- No WJP1/WJX1/WJF1 identity/fingerprint semantics, owner canonicalization, project/drawing boundaries, vertical/profile consistency, occurrence ordering, native V25 materialization, UI, Actions, release, or LOCAL_PASS behavior changes.

## Validation plan

- Preserve all existing ownership behavior.
- Add focused non-terminating probes for both inputs: junction source rejects after exactly 10,001 yields without touching the mapping source; mapping source rejects after exactly 20,001 yields after a bounded junction input.
- Re-fetch current source before implementation and do not overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

Both ownership batch limits bound enumeration/allocation as well as accepted cardinality, focused regression is integrated on current `main`, and this claim is marked `COMPLETED` with exact implementation SHA(s) and validation performed.
