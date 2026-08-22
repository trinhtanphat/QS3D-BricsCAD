# Work claim — Revision capture invalid relation fixture

- Status: `COMPLETED`
- Agent: `/root/fix_level_curtain_frame_z`
- Registered: `2026-08-14T16:43:28+07:00`
- Baseline main SHA: `5bcc3af69425361f12e9f035ea332cee59aaf7b3`
- Priority: first independent Core smoke blocker after current canonical relation writers

## Reserved scope

Reconcile only `tests/QS3D.Core.SmokeTests/RevisionCaptureXmlTextIntegritySmoke.cs` with the canonical `ProjectElement` relation-ID writer contract. The fixture will construct valid elements, prove their relation fields are canonical, then use test-only reflection to inject XML-invalid raw `_familyId`, `_floorId`, and `_zoneId` values and prove each injection reached the getter before exercising the real `RevisionService.Capture` fail-closed boundary.

## Contract and precedent

- `ProjectElement.FamilyId`, `FloorId`, and `ZoneId` setters canonically reject control characters before invalid relation text can enter a project through the public API.
- The completed Revision Capture XML integrity contract still requires corrupt project-derived XML text to fail with `InvalidOperationException` before Capture returns a snapshot.
- The same smoke already uses reflection for the now-unreachable invalid `ProjectElement.Id` case; this claim extends that established test boundary only to the three relation fields.

## Excluded scope

- No production, gate, native adapter/runtime, LOCAL runner/probe, private-data, release, or GitHub Actions changes.
- Do not alter the property, quantity, source-handle, dependency, revision-ID, or valid-Unicode cases.

## Validation plan

- Build `QS3D.Core.SmokeTests` in Release.
- Run focused Revision capture/store/XML gates.
- Run the full registered Core smoke executable and report the next independent blocker, if any.
- Review the final diff and read back the merged implementation from current `main`.

## Completion condition

A normal merged PR changes only the claimed smoke fixture, retains real `RevisionService.Capture` `InvalidOperationException` assertions for all invalid payloads and exact valid-Unicode preservation, records exact validation evidence, and closes this claim on `main`.

## Completion record

- Claim-only merge: `77c247b645050ef45d9f48a2057e31c8703b83e5` via PR #1289.
- Implementation merge: `f2585a5abfe5c975ae114d00d6b30f10279a81dc` via PR #1291.
- Readback confirms the only implementation change is the claimed smoke: each valid Family/Floor/Zone relation is asserted, its private raw field is injected and asserted, and the real `RevisionService.Capture` `InvalidOperationException` check remains. Production and gates are unchanged.
- `dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release --no-restore`: PASS, 0 warnings / 0 errors.
- `dotnet build tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release --no-restore`: PASS, 0 warnings / 0 errors.
- Revision store-integrity, snapshot-schema, source-handles, and snapshot-backup-preservation preflights: PASS.
- Full registered Core smoke advances beyond Revision Capture and stops at independent `SemanticSelectionRelationCanonicalitySmoke.Rejects` line 60 because its padded-FamilyId fixture is canonicalized by the public setter; no full-smoke PASS is claimed.
