# Work claim — Regeneration work profile null-entry integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-regeneration-profile-null-entry-integrity`
- Registered: `2026-08-12T10:21:00+07:00`
- Completed: `2026-08-12T10:24:00+07:00`
- Baseline main SHA: `9db003355eb08e582e4a61c9748cb40d4db11c40`
- Claim commit: `fb0390b2969662348df1293cb16eb715c0621904`
- Source fix commit: `bac4a1f4f1e4b1dc3b636b4ec3aaf6138372e36d`
- Regression commit: `e2854306333da422ea6b664a8c6aab4c52b84dd7`
- Registration commit: `6cada2382110b982fc9f0f98735fc5943501f58f`
- Priority: P2 — public regeneration profile DTOs must reject null collection entries at construction instead of failing later during metric access.

## Confirmed defect

`RegenerationWorkProfile` is public and materializes `targetElementIds`, `items`, and `categories` through `MaterializeBounded<T>(...)`. That helper bounded collection size but accepted null entries. A profile constructed with a null `RegenerationWorkItem` therefore succeeded, then public computed members such as `SemanticDirtyElementCount` dereferenced that entry and threw `NullReferenceException`. Null target/category entries were likewise exposed through a DTO that otherwise validates its public invariants.

The file already had completed lanes for DTO scalar invariants and collection count bounds; this lane only closes the null-entry gap in the shared materializer.

## Implemented surfaces

- `src/QS3D.Core/Services/RegenerationWorkProfiler.cs` (`MaterializeBounded<T>` null-entry guard only)
- `tests/QS3D.Core.SmokeTests/RegenerationWorkProfileNullEntrySmoke.cs`
- `tests/QS3D.Core.SmokeTests/RegenerationWorkProfileNullEntryRegistration.cs`
- this claim file

## Implemented contract

- `MaterializeBounded<T>` now rejects null entries before count/materialization with an `ArgumentException` tied to the owning collection parameter.
- The shared guard covers target IDs, work items, and category work entries without altering valid profiles.
- Existing collection count bounds remain unchanged.
- No blank/padded/duplicate target-ID or cross-count semantics were added.

## Excluded scope honored

- No dependency graph/topological-order changes.
- No regeneration execution/preview/runtime changes.
- No subset target semantics beyond null collection entries.
- No GitHub Actions dispatch and no BricsCAD runtime qualification claim.

## Validation actually performed

- Direct path history was inspected; latest source changes were the completed DTO invariant and bounded-collection lanes, with no current overlapping source change observed before registration.
- Claim was published before substantive writes; the exact source blob was re-fetched from `main` after claim publication as `76ce1f3dcc54c607b807b9c87d5269a9408dc908`.
- Source update used that exact blob SHA as a guard.
- Exact source commit diff was reviewed: only two lines were added inside `MaterializeBounded<T>` to reject `ReferenceEquals(value, null)` before existing count/materialization logic.
- Focused smoke was read back from `main`; it covers null target ID, null work item, null category entry, and a valid one-item profile whose planned/semantic/geometry-only metrics remain usable.
- Module-initializer registration is committed as `6cada2382110b982fc9f0f98735fc5943501f58f`.
- No local .NET compile/test execution is claimed in this connector-only lane.
- No BricsCAD V25/V26 runtime qualification is claimed.
- No GitHub Actions were dispatched and no force-push was used.

## Completion condition

Completed. Public regeneration work profiles now reject null collection entries during construction rather than exposing delayed null dereferences, focused regression source is on `main`, and exact implementation/test SHAs are recorded above.
