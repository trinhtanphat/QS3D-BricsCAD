# Work claim — Revision capture invalid element-ID fixture

- Status: `COMPLETED`
- Agent: `/root/fix_level_curtain_frame_z`
- Registered: `2026-08-14T14:15:12+07:00`
- Completed: `2026-08-14T14:18:14+07:00`
- Baseline main SHA: `aa9c6d9981093afbda98bfaebaca17d1afcf4af8`
- Priority: first independent Core smoke blocker after the snapshot null-canonicalization correction

## Reserved scope

Reconcile only `tests/QS3D.Core.SmokeTests/RevisionCaptureXmlTextIntegritySmoke.cs` with the canonical `ProjectElement` ID contract. The fixture must create a valid element, prove that construction succeeded, then use the established test-only `<Id>k__BackingField` reflection seam to inject XML-invalid text so the real `RevisionService.Capture` producer boundary remains exercised.

## Contract and precedent

- `ProjectElement.RequireId` canonically rejects control characters before a `ProjectElement` can be constructed.
- The original revision-capture contract still requires project-derived XML-invalid payloads to fail closed with `InvalidOperationException` before a snapshot is returned.
- `GeneratedHandleOwnershipIndexSmoke` and `MeasurementWorkItemCoverageSmoke` already use the exact `ProjectElement.<Id>k__BackingField` test-only seam; the latter injects a control character to exercise otherwise unreachable corrupt-project state.

## Excluded scope

- No production/domain/revision/persistence change.
- Do not modify the other invalid payload cases or valid-Unicode coverage in the smoke.
- No Level, probe, runner, BricsCAD adapter/runtime, private drawing, packaging, release, or GitHub Actions work.

## Validation plan

- Build `QS3D.Core.SmokeTests` in Release.
- Run focused revision-capture/XML static gates found in the repository.
- Run the full registered Core smoke executable and report the next first independent blocker if any.
- Review the final diff and read back the merged implementation from current `main`.

## Completion condition

A normal merged PR changes only the reserved smoke fixture, retains `RevisionService.Capture` `InvalidOperationException` coverage plus all remaining payload/Unicode cases, records exact validation evidence, and closes this claim on `main`.

## Completion record

- Claim-only merge: `f676d1e0d710295a631dd24317aae4c83757a91d` via PR #1184.
- Implementation merge: `f98acde4e007c831abf18b0e42621f7d41a0f7b1` via PR #1185.
- Readback from current `main` confirms the smoke constructs and asserts valid ID `E-1`, injects and verifies `E-\u0001-1` through `<Id>k__BackingField`, then retains the real `RevisionService.Capture` `InvalidOperationException` assertion. All other invalid-payload and valid-Unicode cases are unchanged; production was not modified.
- `dotnet build tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release --no-restore`: PASS, 0 warnings / 0 errors.
- `preflight-revision-store-integrity.py`: PASS.
- `preflight-revision-snapshot-schema.py`: PASS.
- `preflight-revision-source-handles.py`: PASS.
- `preflight-revision-snapshot-backup-preservation.py`: PASS.
- The full registered Core smoke advances beyond this fixture and next stops in `RoomFinishScheduleGroupKeyCollisionSmoke.Run` line 14 because `FloorDefinition` now rejects the fixture's control-character ID before the intended schedule collision assertion.
