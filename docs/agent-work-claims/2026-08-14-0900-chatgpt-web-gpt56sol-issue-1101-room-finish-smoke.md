# Work claim — Issue #1101 Room Finish missing-Family smoke contract

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-issue1101-20260814-0900`
- Registered: `2026-08-14T09:00:00+07:00`
- Baseline main SHA: `00d6e68c9492a0c9dbcb04215bab7ecbb9c1a006`
- Issue: `#1101` — Core smoke: Room Finish missing-Family regression contradicts identity guard
- Priority: P0 CAD-independent Core-smoke unblock handed off by LOCAL-003.

## Reserved scope

Reconcile the Room Finish missing-Family regression with the canonical shared reporting identity policy. The narrow intended fix is test-contract correction: a nonblank missing `FamilyId` must remain fail-closed rather than being treated as a display fallback.

## Reserved surfaces

- Edit: `tests/QS3D.Core.SmokeTests/RoomFinishFamilyCategorySmoke.cs`.
- Read-only policy verification: `src/QS3D.Core/Reporting/ReportingProjectIdentityGuard.cs`.
- Issue `#1101` validation/closeout metadata.

No other source/test/fixture/workflow surface may be read for targeted implementation diagnosis or edited without a separate scope-amendment commit first.

## Excluded scope

- Do not weaken or change the shared reporting identity policy unless a separate amendment is landed first with cross-report coordination.
- Do not touch issue `#1099`, issue `#1005` / LOCAL-004, LOCAL-003 native Level qualification, Geometry quantity explainer, auto-layout documentation lanes, release/version workflows, or any LOCAL_ONLY runner/probe.
- No GitHub Actions dispatch is needed for this test-only Core lane.

## Acceptance

- The missing-Family Room Finish case asserts the existing fail-closed `InvalidOperationException` contract from `ReportingProjectIdentityGuard.RequireExistingFamilyReference(...)` rather than expecting a fallback row.
- Existing valid-Family/category behavior remains covered and unchanged.
- Full `QS3D.Core.SmokeTests` passes on the exact qualifying `main` SHA, or the next unrelated blocker is reported precisely without weakening its test.
- Claim remains `ACTIVE` until implementation is on `main` and validation evidence is recorded.

## Coordination

Immediately before every write, refresh current `main`, inspect commits since the claim baseline, and recheck claims for `#1101`, `RoomFinishFamilyCategorySmoke`, and reporting identity surfaces. Stop/re-scope on overlap. The LOCAL-003 worker only handed off this CAD-independent blocker and must not edit Core/test to make it pass.
