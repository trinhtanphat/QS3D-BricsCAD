# Work claim — Issue #1101 Room Finish missing-Family smoke contract

- Status: `COMPLETED`
- Phase: `SOURCE_FIXED / EXACT_MAIN_VALIDATED / ISSUE_CLOSED`
- Agent: `chatgpt-web-gpt56sol-issue1101-20260814-0900`
- Registered: `2026-08-14T09:00:00+07:00`
- Completed: `2026-08-14`
- Baseline main SHA: `00d6e68c9492a0c9dbcb04215bab7ecbb9c1a006`
- Source-fix commit on main: `3aed2b5af29c33accb0e3df637e2f22e28c4e731`
- Exact validation SHA: `e98c30fb79abe41e0f9df6b5cd1d175152453675`
- Issue: `#1101` — CLOSED / completed

## Reserved scope

Reconcile the Room Finish missing-Family regression with the canonical shared reporting identity policy. The narrow fix was test-contract correction: a nonblank missing `FamilyId` must fail closed rather than being treated as a display fallback.

Reserved source/test surfaces were:

- `tests/QS3D.Core.SmokeTests/RoomFinishFamilyCategorySmoke.cs` — edit;
- `src/QS3D.Core/Reporting/ReportingProjectIdentityGuard.cs` — read-only policy verification;
- issue `#1101` validation/closeout metadata.

## Implemented correction

Commit `3aed2b5af29c33accb0e3df637e2f22e28c4e731` changes only the contradictory missing-Family smoke case:

- `MissingFamilyPreservesFallbackBehavior` -> `MissingFamilyFailsClosed`;
- the fixture still references `FamilyId = "MISSING"` with otherwise valid Floor/Zone state;
- the expected result is the canonical `InvalidOperationException` from reporting identity validation;
- the valid matching-Family inheritance case and mismatched-category fail-closed case are unchanged;
- `ReportingProjectIdentityGuard.cs` was verified and not modified.

## Exact validation closeout

Successor exact-main validation completed on `e98c30fb79abe41e0f9df6b5cd1d175152453675`, with `3aed2b5...` in ancestry:

- Core Release build: PASS, `0 warnings / 0 errors`;
- complete `QS3D.Core.SmokeTests`: `ALL PASS`;
- `preflight-family-relation-assignment-integrity.py`: PASS;
- `preflight-family-category-integrity.py`: PASS;
- an earlier exact run advanced past this corrected Room Finish case and exposed a separate stale Curtain schedule fixture; that was handed off as issue `#1105`, fixed independently, and the final rerun passed without weakening this report contract;
- issue `#1101` was closed as completed.

The aggregate preflight at the #1101 validation checkpoint still contained unrelated failures in other lanes; those were not attributed to this issue and were subsequently handled under their own claims. Later release workflow run #145 on descendant `82901db277eba9e685ebc356e97375138d0d538d` completed successfully through all source guards, Core smoke, V25 compile, package and prerelease publication.

## Excluded scope

This claim never reserved issue `#1099`, issue `#1005` / LOCAL-004, LOCAL-003 native Level qualification, Geometry quantity explainer, release/version workflows, or other LOCAL_ONLY runner/probe surfaces.

## Completion condition

Satisfied. The source/test correction is on `main`, exact full Core smoke validation is green, #1101 is closed, and this claim no longer reserves any implementation or validation surface. Future work must open a new claim rather than treating this completed claim as active ownership.
