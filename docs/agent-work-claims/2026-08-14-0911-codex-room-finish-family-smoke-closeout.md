# Work claim — Issue #1101 Room Finish smoke validation and closeout

- Status: `ACTIVE`
- Agent: `codex-/root/fix_room_finish_family-20260814-0911`
- Registered: `2026-08-14T09:11:20+07:00`
- Baseline main SHA: `b4059961315ba7e6b5455ea8d41af65fe3c23227`
- Priority: exact-main validation successor for issue `#1101`, which blocks the LOCAL-003 prerequisite smoke.

## Reserved scope

Validate the already-landed Room Finish missing-Family smoke correction at commit `3aed2b5af29c33accb0e3df637e2f22e28c4e731` on a fresh exact `main`, decide whether any further source change is justified by actual test evidence, and publish the issue/claim closeout. The historical mismatch is a stale fixture expectation unless fresh validation contradicts that diagnosis: the shared reporting identity guard intentionally rejects every nonblank dangling Family reference.

## Expected surfaces

- Full `tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj` Release execution.
- Focused readback of `tests/QS3D.Core.SmokeTests/RoomFinishFamilyCategorySmoke.cs` and `src/QS3D.Core/Reporting/ReportingProjectIdentityGuard.cs`.
- Closeout metadata in this claim and `docs/agent-work-claims/2026-08-14-0900-chatgpt-web-gpt56sol-issue-1101-room-finish-smoke.md`.
- Issue `#1101` closeout after exact-main evidence passes.

## Excluded scope

- No change to `RoomFinishScheduleBuilder`, the shared reporting identity policy, or any production source unless the reserved full smoke proves the landed fixture correction insufficient.
- No BricsCAD runtime, P10/Curtain files, LOCAL_ONLY probe, private data, release/version work, GitHub Actions, or issue `#1099` work.
- No unrelated Core failure is absorbed; a distinct blocker is handed off under its own non-overlapping claim.

## Validation plan

- Fetch current `origin/main` and prove `3aed2b5af29c33accb0e3df637e2f22e28c4e731` is in ancestry.
- Build `QS3D.Core` Release and run the complete Core smoke executable on the exact baseline or a newer collision-checked main.
- Run relevant Room Finish/reporting static gates and `scripts/preflight-all.py` when feasible.
- Review `git diff --check` and publish only validation/closeout metadata if no source defect remains.

## Coordination

This claim is an explicit validation-only successor to the active issue `#1101` claim owned by `chatgpt-web-gpt56sol-issue1101-20260814-0900`. Its source/test correction remains authoritative and is not duplicated. The predecessor claim is amended in the same registration-only commit to record this split.

## Completion condition

A fresh exact-main full Core smoke proves the missing-Family case follows the canonical fail-closed identity contract, any newly exposed unrelated blocker is separately handed off, issue `#1101` is closed, and both issue claims record the exact passing SHA/commands as `COMPLETED` on current `main`.
