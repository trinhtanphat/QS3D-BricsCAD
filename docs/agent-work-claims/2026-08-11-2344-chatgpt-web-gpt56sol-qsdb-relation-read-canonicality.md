# Work claim — QSDB persisted relation/source identity canonical read

- Status: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-qsdb-relation-read-canonicality`
- Registered: `2026-08-11T23:44:00+07:00`
- Baseline main SHA: `e9bb3ca787dc3554a75cf8a55dbd190810823ab3`
- Priority: concrete persistence fail-open regression found during owner-requested continue-all audit

## Confirmed defect

QSDB save-side validation already requires canonical unpadded project/element relation ids, source handles and dependencies. However the current load path trims these persisted values after `QsdbProjectXmlSchemaValidator` structural validation:

- root `activeZoneId` / `activeFloorId`;
- element `familyId` / `floorId` / `zoneId`;
- `<h>` source handle text;
- `<d>` dependency text.

The schema validator currently does not validate those values before the trim. A tampered/padded persisted reference can therefore be silently repaired on load even though the same value would be rejected if present in memory during Save.

## Reserved scope

Harden only current-schema pre-load XML validation for these already-governed relation/source identity surfaces. Canonical writer output and empty optional relation ids remain valid. Do not broaden this lane to display names, arbitrary property values, project/family/element primary ids, quantity-rule identifiers, migration policy, or other XML fields.

## Expected surfaces

- `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs`
- `tests/QS3D.Core.SmokeTests/QsdbPersistedRelationCanonicalReadSmoke.cs`
- module-initializer registration in that new smoke file
- this claim file

## Excluded scope

- No schema-version bump or legacy migration change.
- No `QsdbProjectStore` writer/atomic-save/backup behavior change.
- No native BricsCAD project lifecycle or Save/SaveAs runtime work.
- No project/family/element primary-ID canonicality expansion in this lane.
- No GitHub Actions dispatch or V25 runtime qualification.

## Validation plan

- Padded root active Floor/Zone id is rejected before loader normalization.
- Padded element Family/Floor/Zone relation id is rejected.
- Padded source handle text is rejected.
- Padded dependency text is rejected.
- Canonical values and empty optional relation ids remain accepted.
- Existing map-key canonicality and XML structure validation remain untouched.
- Focused smoke auto-registers without shared test-registration edits.
- Re-fetch current target blob after claim publication, inspect exact implementation diff, and read back final source/test from `main`; never force-push.

## Coordination

Earlier `qsdb map-key load canonicality` work is completed and remains upstream; this lane deliberately covers different XML values. No current/recent QSDB claim was found for persisted relation/source identity read canonicality.

## Completion condition

Current `main` rejects padded persisted relation/source identities before loader trim, focused deterministic regression coverage is present, and this claim is closed `COMPLETED` with exact commits and actual validation scope.
