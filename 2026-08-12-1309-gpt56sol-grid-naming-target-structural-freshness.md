# Work claim — Grid naming target structural freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-grid-naming-target-structural-freshness-20260812-1309`
- Registered: `2026-08-12T13:09:00+07:00`
- Completed: `2026-08-12T13:16:00+07:00`
- Baseline main SHA observed before registration: `688827f27ff832dbc380a2a7f82353eb956471e7`
- Claim commit: `705682b5833af9a631b97a56de6656d21e483ab2`
- Source fix commit: `4c5245c498420236cdcbdc62a37579b45b1e4c9d`
- Source indexing refinement: `2ab703bb9a23ab5ad7c5c58495f182ff78873027`
- Regression commit: `a6fe624374c62e4d1e453f9f4abec9e9c287f461`
- Priority: evidence-driven remote-safe Core structural freshness

## Completed scope

`GridNamingService.Renumber(...)` now snapshots semantic element object references before invoking the caller-supplied target-ID enumerable, retains the existing `ProjectState.ChangeVersion` freshness check, and after enumeration verifies that every requested Grid ID still resolves to the same `ProjectElement` object.

The initial implementation established the object-identity guard; the follow-up source refinement indexes only requested IDs in one pass over the captured element snapshot instead of scanning the entire snapshot once per target. This preserves bounded batch behavior without introducing O(project-size × target-count) work.

Same-ID target replacement/addition during caller enumeration therefore fails closed before `project.Touch()` or Grid label mutation. Stable targets retain ordinary behavior. Structural replacement of an unrelated element does not falsely invalidate a target whose object identity is unchanged.

## Implemented surfaces

- `src/QS3D.Core/Domain/GridNamingService.cs`
- `tests/QS3D.Core.SmokeTests/GridNamingTargetStructuralFreshnessSmoke.cs`
- this claim file

## Coordination / concurrency evidence

- Claim commit parent is exactly the recorded baseline `688827f27ff832dbc380a2a7f82353eb956471e7`.
- Product update used expected pre-edit blob `1866fc3a211c6c26cfe4780ba2d7c980c14862de`; the indexing refinement used expected intermediate blob `65b259c4124524204b5413917dc35a6db4f8b7bc`. Same-file concurrent edits would therefore have failed rather than been overwritten.
- Current-main source readback after the regression commit confirms the target reference snapshot, one-pass requested-ID index and `ReferenceEquals` freshness check remain present.
- Current-main smoke readback confirms stable-target, same-ID replacement, and unrelated-replacement cases remain present.
- Later concurrent `main` commit `3de60ce39149fee75f01bd6d4751967f6ab5c035` is a documentation-preflight claim and has parent `a6fe624374c62e4d1e453f9f4abec9e9c287f461`; it does not overlap this source/test lane.

## Validation actually performed

- Exact remote source readback on current `main`.
- Exact focused smoke source readback on current `main`.
- Static review of the first source patch identified and removed an avoidable O(project-size × target-count) lookup before completion.
- Existing Grid naming version-based input freshness, 2,000-target bound, canonical target IDs, option validation, collision handling, no-op detection and project-touch semantics are preserved by inspection.
- No GitHub Actions were dispatched.
- No local .NET build/smoke execution PASS is claimed from this connector-only session.
- No licensed BricsCAD V25/V26 runtime PASS or release qualification is claimed.

## Completion condition

Satisfied. Current `main` rejects silent same-ID Grid target retargeting across caller enumeration before naming mutation, focused regression evidence is present, and this reservation is released.
