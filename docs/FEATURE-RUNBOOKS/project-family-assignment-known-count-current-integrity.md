# ProjectFamilyService known-Count Current observation integrity

Lane-Key: `issue-4591`

## Purpose

`ProjectFamilyService.Assign` accepts caller-controlled `IEnumerable<ProjectElement>` assignment targets. Existing hardening from #2971 binds supported collection Count surfaces, rejects conflicting/negative/oversized Counts, rejects under-yield and over-yield, preserves single-pass enumeration and keeps rejected operations mutation-free.

The remaining ordering gap was the C# `foreach` boundary: after a successful `MoveNext()`, `foreach` evaluates `IEnumerator.Current` before entering the loop body. An advertised Count=N could therefore expose the first N+1 `Current` before the existing over-yield guard rejected it. Enumerable-only input had the same observation gap at the 10,000-entry hard ceiling.

## Contract

For caller-controlled Family assignment target traversal:

1. Preserve initial generic `ICollection<ProjectElement>`, `IReadOnlyCollection<ProjectElement>`, and non-generic `ICollection` Count validation and conflict rejection.
2. Traverse explicitly as `successful MoveNext -> known-Count admission -> 10,000-entry admission -> Current`.
3. Never read `Current` for the first item beyond an admitted known Count.
4. Never read `Current` for the first item beyond the 10,000-entry hard ceiling for streaming input.
5. Preserve exact post-traversal under-yield rejection and project `ChangeVersion` freshness checks.
6. Preserve ownership/category validation, deterministic target de-duplication, canonical Family identity handling, and publication only after all target validation succeeds.
7. Rejected inputs must not change `ProjectState.ChangeVersion`, `UpdatedUtc`, target `FamilyId`, inherited properties, or dirty state.

## TDD evidence

RED commit `cc613805dddfa7487f04e681c6faafd97192f517` added the focused module-initializer smoke without changing production. Shared CI run `33245455279` built Core successfully and then failed with `POISON counted Current beyond known Count` from `ProjectFamilyService.ResolveOwnedElements`, proving the second `Current` was observed before the existing Count-overrun diagnostic.

GREEN commit `a8c8f976cd7504bf8689e989e00f8fc87c8f8daf` replaced only the caller-controlled target `foreach` with an explicit enumerator and moved Count/cap admission before `Current`. In Shared CI run `33245725227`, Core build and deterministic smoke both completed successfully before the V25 compile stages.

The focused regression additionally pins streaming hard-cap behavior: the 10,001st successful `MoveNext` is rejected with 10,000 `Current` reads.

## Runtime boundary

This is deterministic Core enumeration/atomicity behavior. Licensed BricsCAD runtime qualification is `NOT_APPLICABLE`; hosted CI must not be reported as `LOCAL_PASS`.

## Landing

Require exact-head Shared branch CI, fresh reservation/path collision validation, latest-main reconciliation without force, a canonical PR carrying `Lane-Key: issue-4591`, protected exact-candidate `preflight + core` SUCCESS, expected-head merge, and protected-main verification.