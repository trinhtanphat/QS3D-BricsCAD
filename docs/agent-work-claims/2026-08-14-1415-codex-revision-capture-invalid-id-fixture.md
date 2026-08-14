# Work claim — Revision capture invalid element-ID fixture

- Status: `ACTIVE`
- Agent: `/root/fix_level_curtain_frame_z`
- Registered: `2026-08-14T14:15:12+07:00`
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
