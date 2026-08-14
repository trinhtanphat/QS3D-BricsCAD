# Work claim — Issue #1105 Curtain schedule missing-Family smoke

- Status: `COMPLETED`
- Phase: `SOURCE_FIXED / EXACT_MAIN_VALIDATED / ISSUE_CLOSED`
- Agent: `chatgpt-web-gpt56sol-issue1105-curtain-family-smoke-20260814-0923`
- Registered: `2026-08-14T09:23:00+07:00`
- Baseline main SHA: `77ebd673a9f81ca3628e75328319427fa298a33f`
- Priority: `P0 Core smoke blocker` — reconcile a stale Curtain schedule test fixture with the canonical reporting Family-identity guard.

## Confirmed defect

GitHub issue #1105 recorded a fresh Core build PASS followed by full Core smoke failure in `CurtainWallScheduleFamilyCategorySmoke.MissingFamilyPreservesFallbackBehavior()`. The fixture constructed a GlassWall with nonblank `FamilyId = "MISSING"` and expected fallback schedule projection, while `ReportingProjectIdentityGuard.RequireExistingFamilyReference(...)` intentionally fails closed for dangling nonblank Family references. This was the same stale-fixture class already corrected for Room Finish by `3aed2b5af29c33accb0e3df637e2f22e28c4e731`.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/CurtainWallScheduleFamilyCategorySmoke.cs`
- this claim file
- issue #1105 close-out metadata

## Implemented acceptance

1. The stale missing-Family smoke is now named `MissingFamilyFailsClosed()`.
2. It requires `InvalidOperationException` from `CurtainWallScheduleBuilder.Build(project)` for a dangling nonblank Family reference.
3. The matching GlassWall Family projection case and mismatched-category fail-closed case remain unchanged.
4. Production reporting identity guards were not weakened and no fallback behavior was added.
5. The exact pushed test was re-fetched from a newer `main` and retained the intended fail-closed contract.
6. A separate capable runner subsequently validated a fresh exact-main Core Release build and complete Core smoke after this fix.

## Explicit non-scope

No changes to issue #1099 Update/version validation, Selection, LOCAL-002/P10 Curtain native runner/probes/evidence, production Curtain geometry/materialization, reporting production code, persistence, release workflows or GitHub Actions.

## Completion record

- Claim-only commit: `c8ae5eb20b9851a46f0fd6caeea692c45aff95e9`.
- Source/test correction: `8637605f4a00cd71a46ae1eba35dc18eae704c2f` (`test(report): align missing Curtain Family with identity guard`).
- Remote read-back after concurrent `main` movement confirmed `MissingFamilyFailsClosed()` and `Throws<InvalidOperationException>(...)` remained present.
- Fresh managed validation recorded by the issue-#1101 successor closeout: exact tested main SHA `e98c30fb79abe41e0f9df6b5cd1d175152453675`; .NET SDK `10.0.302`; `dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release --nologo` PASS with `0 warnings / 0 errors`; complete `QS3D.Core.SmokeTests` run `ALL PASS`. The validation record explicitly identifies `8637605f4` as the independent Curtain fix that cleared the follow-on blocker.
- Aggregate preflight on that exact SHA still exposed four unrelated gates (product-boundary, research-implementation-status, V25 NETLOAD/update UX, wall-junctions); none changes the successful Core smoke acceptance for #1105.
- GitHub Actions: `NOT_DISPATCHED`.
- BricsCAD native runtime: `NOT_RUN` / not required for this CAD-independent smoke contract.

## Completion

Satisfied. The Curtain schedule missing-Family fixture now follows the canonical fail-closed reporting identity policy, the valid/mismatch coverage remains intact, and a fresh exact-main full Core smoke passed with the correction in ancestry. Issue #1105 can be closed as completed.
