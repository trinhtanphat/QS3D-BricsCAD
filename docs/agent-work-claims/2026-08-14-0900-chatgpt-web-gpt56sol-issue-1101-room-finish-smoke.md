# Work claim — Issue #1101 Room Finish missing-Family smoke contract

- Status: `COMPLETED`
- Phase: `SOURCE_FIXED / EXACT_MAIN_VALIDATED / ISSUE_CLOSED`
- Agent: `chatgpt-web-gpt56sol-issue1101-20260814-0900`
- Registered: `2026-08-14T09:00:00+07:00`
- Baseline main SHA: `00d6e68c9492a0c9dbcb04215bab7ecbb9c1a006`
- Source-fix commit on main: `3aed2b5af29c33accb0e3df637e2f22e28c4e731`
- Issue: `#1101` — Core smoke: Room Finish missing-Family regression contradicts identity guard
- Priority: P0 CAD-independent Core-smoke unblock handed off by LOCAL-003.

## Reserved scope

Reconcile the Room Finish missing-Family regression with the canonical shared reporting identity policy. The narrow intended fix is test-contract correction: a nonblank missing `FamilyId` must remain fail-closed rather than being treated as a display fallback.

## Reserved surfaces

- Edit: `tests/QS3D.Core.SmokeTests/RoomFinishFamilyCategorySmoke.cs`.
- Read-only policy verification: `src/QS3D.Core/Reporting/ReportingProjectIdentityGuard.cs`.
- Issue `#1101` validation/closeout metadata.

No other source/test/fixture/workflow surface may be read for targeted implementation diagnosis or edited without a separate scope-amendment commit first.

## Implemented correction

Commit `3aed2b5af29c33accb0e3df637e2f22e28c4e731` changes only the contradictory missing-Family smoke case:

- `MissingFamilyPreservesFallbackBehavior` -> `MissingFamilyFailsClosed`;
- the fixture still references `FamilyId = "MISSING"` with otherwise valid Floor/Zone state;
- the expected result is now the canonical `InvalidOperationException` from reporting identity validation;
- the valid matching-Family inheritance case and mismatched-category fail-closed case are unchanged;
- `ReportingProjectIdentityGuard.cs` was read for policy verification and not modified.

## Excluded scope

- Do not weaken or change the shared reporting identity policy unless a separate amendment is landed first with cross-report coordination.
- Do not touch issue `#1099`, issue `#1005` / LOCAL-004, LOCAL-003 native Level qualification, Geometry quantity explainer, auto-layout documentation lanes, release/version workflows, or any LOCAL_ONLY runner/probe.
- Do not rerun stale cloud workflow run #144 (`31761935152`, SHA `b8df1d0915ea69aa18313c0c593680f44660d3dc`): it failed feature guards before Core smoke and predates this fix.

## Acceptance

- The missing-Family Room Finish case asserts the existing fail-closed `InvalidOperationException` contract from `ReportingProjectIdentityGuard.RequireExistingFamilyReference(...)` rather than expecting a fallback row. **Implemented on main.**
- Existing valid-Family/category behavior remains covered and unchanged. **Read-back verified.**
- Full `QS3D.Core.SmokeTests` passes on an exact qualifying `main` SHA, or the next unrelated blocker is reported precisely without weakening its test. **Pending fresh execution.**
- Claim remains `ACTIVE` until validation evidence is recorded; do not duplicate the source/test correction.

## Validation status

- Commit diff read-back: PASS; exactly one test file changed.
- GitHub commit statuses on `3aed2b5...`: none published at the time of this update.
- Cloud run #144 is stale and not qualifying evidence.
- This execution environment has no `dotnet` executable, so no local full-smoke PASS is claimed.

Successor closeout on `2026-08-14`:

- Final exact tested main SHA: `e98c30fb79abe41e0f9df6b5cd1d175152453675`; source correction `3aed2b5af29c33accb0e3df637e2f22e28c4e731` is in ancestry.
- Local .NET SDK `10.0.302`; Core Release build PASS with `0 warnings / 0 errors`.
- Complete Core smoke: `ALL PASS`. An earlier exact `f11488e81f73ac4454b637e2fa4bd5660e90d85e` run advanced past the corrected Room Finish case and exposed the separate stale Curtain schedule fixture; it was handed off as issue `#1105`, fixed independently by `8637605f4`, and the final rerun passed without weakening either report contract.
- `preflight-family-relation-assignment-integrity.py` and `preflight-family-category-integrity.py`: PASS.
- Aggregate preflight discovered 781 gates and reported four unrelated failures on final exact SHA `e98c30fb...`: product-boundary, research-implementation-status, V25 NETLOAD/update UX and wall-junctions. None is part of issue `#1101`.
- Issue `#1101` was closed as completed with the exact evidence above. No GitHub Actions or BricsCAD runtime was used.

## Coordination

Immediately before every write, refresh current `main`, inspect commits since the claim baseline, and recheck claims for `#1101`, `RoomFinishFamilyCategorySmoke`, and reporting identity surfaces. Stop/re-scope on overlap. The LOCAL-003 worker only handed off this CAD-independent blocker and must not edit Core/test to make it pass. Once a fresh exact-SHA full Core smoke passes with `3aed2b5...` in ancestry, close #1101 and mark this claim `COMPLETED`; if it exposes another unrelated blocker, hand that off under a separate claim instead of weakening tests.

Validation/closeout split recorded `2026-08-14T09:11:20+07:00`: `codex-/root/fix_room_finish_family-20260814-0911` owns only the fresh exact-main Core smoke, relevant static gates, issue closeout and claim evidence under `docs/agent-work-claims/2026-08-14-0911-codex-room-finish-family-smoke-closeout.md`. The original source correction remains owned by this claim and must not be duplicated or weakened.
