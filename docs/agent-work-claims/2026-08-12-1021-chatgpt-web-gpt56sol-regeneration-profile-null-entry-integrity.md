# Work claim — Regeneration work profile null-entry integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-regeneration-profile-null-entry-integrity`
- Registered: `2026-08-12T10:21:00+07:00`
- Baseline main SHA: `9db003355eb08e582e4a61c9748cb40d4db11c40`
- Priority: P2 — public regeneration profile DTOs must reject null collection entries at construction instead of failing later during metric access.

## Confirmed defect

`RegenerationWorkProfile` is public and materializes `targetElementIds`, `items`, and `categories` through `MaterializeBounded<T>(...)`. That helper bounds collection size but currently accepts null entries. A profile constructed with a null `RegenerationWorkItem` therefore succeeds, then public computed members such as `SemanticDirtyElementCount` dereference that entry and throw `NullReferenceException`. Null target/category entries are likewise exposed through a DTO that otherwise validates its public invariants.

The file already has completed lanes for DTO scalar invariants and collection count bounds; this lane only closes the null-entry gap in the shared materializer.

## Reserved scope

- `src/QS3D.Core/Services/RegenerationWorkProfiler.cs` (`MaterializeBounded<T>` null-entry guard only)
- `tests/QS3D.Core.SmokeTests/RegenerationWorkProfileNullEntrySmoke.cs`
- `tests/QS3D.Core.SmokeTests/RegenerationWorkProfileNullEntryRegistration.cs`
- this claim file

## Intended contract

- Public profile construction rejects null target-id, work-item, or category entries immediately with `ArgumentException` tied to the relevant collection parameter.
- Existing collection count bounds and all valid profiler-generated profiles remain unchanged.
- No new blank/padded/duplicate target-ID or cross-count semantics are introduced in this lane.

## Excluded scope

- No dependency graph/topological-order changes.
- No regeneration execution/preview/runtime changes.
- No subset target semantics beyond null collection entries.
- No GitHub Actions dispatch and no BricsCAD runtime qualification claim.

## Validation plan

- Verify claim ancestry and re-fetch exact source blob before write.
- Add one generic null-entry guard in `MaterializeBounded<T>`.
- Add focused module-initializer smoke for null target ID, null work item, null category entry, plus one valid profile sanity case.
- Review exact source diff/read-back, close claim with exact SHAs, and verify ancestry.
- No local compile/runtime PASS will be claimed unless actually executed.
