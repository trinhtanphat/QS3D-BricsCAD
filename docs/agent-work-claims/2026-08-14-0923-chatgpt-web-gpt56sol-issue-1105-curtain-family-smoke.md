# Work claim — Issue #1105 Curtain schedule missing-Family smoke

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-issue1105-curtain-family-smoke-20260814-0923`
- Registered: `2026-08-14T09:23:00+07:00`
- Baseline main SHA: `77ebd673a9f81ca3628e75328319427fa298a33f`
- Priority: `P0 Core smoke blocker` — reconcile a stale Curtain schedule test fixture with the canonical reporting Family-identity guard.

## Confirmed defect

GitHub issue #1105 records a fresh Core build PASS followed by full Core smoke failure in `CurtainWallScheduleFamilyCategorySmoke.MissingFamilyPreservesFallbackBehavior()`. The fixture constructs a GlassWall with nonblank `FamilyId = "MISSING"` and expects fallback schedule projection, while `ReportingProjectIdentityGuard.RequireExistingFamilyReference(...)` intentionally fails closed for dangling nonblank Family references. This is the same stale-fixture class already corrected for Room Finish by `3aed2b5af29c33accb0e3df637e2f22e28c4e731`.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/CurtainWallScheduleFamilyCategorySmoke.cs`
- this claim file
- issue #1105 close-out metadata if available

## Acceptance

1. Rename the stale missing-Family smoke to express fail-closed semantics.
2. Require `InvalidOperationException` from `CurtainWallScheduleBuilder.Build(project)` for a dangling nonblank Family reference.
3. Preserve the matching GlassWall Family projection case and mismatched-category fail-closed case unchanged.
4. Do not weaken production reporting identity guards or add fallback behavior.
5. Re-fetch the exact test after publish and record truthful validation. No GitHub Actions dispatch and no BricsCAD runtime claim.

## Explicit non-scope

No changes to issue #1099 Update/version validation, Selection, LOCAL-002/P10 Curtain native runner/probes/evidence, production Curtain geometry/materialization, reporting production code, persistence, release workflows or GitHub Actions.

## Validation plan

Publish this claim alone, refresh `main`, verify no overlapping claim appeared, patch only the stale test contract, re-fetch the commit/file, and close this claim `COMPLETED`. Full Core smoke execution will be recorded only if actually available; issue #1105 already provides the failing pre-fix full-smoke evidence.
