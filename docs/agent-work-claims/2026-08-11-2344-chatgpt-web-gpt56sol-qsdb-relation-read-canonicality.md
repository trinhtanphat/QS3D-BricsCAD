# Work claim — QSDB persisted relation/source identity canonical read

- Status: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-qsdb-relation-read-canonicality`
- Registered: `2026-08-11T23:44:00+07:00`
- Baseline main SHA: `e9bb3ca787dc3554a75cf8a55dbd190810823ab3`
- Priority: concrete persistence fail-open regression found during owner-requested continue-all audit

## Confirmed defect

QSDB save-side validation already requires canonical unpadded project/element relation ids, source handles and dependencies. However the load path trimmed these persisted values after structural validation:

- root `activeZoneId` / `activeFloorId`;
- element `familyId` / `floorId` / `zoneId`;
- `<h>` source handle text;
- `<d>` dependency text.

Before this lane, the XML schema validator did not validate those values before the trim, so a tampered/padded persisted reference could be silently repaired on load even though the same value would be rejected if present in memory during Save.

## Reserved scope

Harden only pre-load XML validation for these already-governed relation/source identity surfaces. Canonical writer output and empty optional relation ids remain valid. This lane does not expand canonicality to display names, arbitrary property values, primary project/family/element ids, quantity-rule identifiers or other XML fields.

## Expected surfaces

- `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs`
- `tests/QS3D.Core.SmokeTests/QsdbPersistedRelationCanonicalReadSmoke.cs`
- this claim file

## Excluded scope

- No schema-version bump and no migration algorithm rewrite.
- The existing migration pipeline continues to run the current schema validator after migration, so migrated documents are subject to the same final canonical-state checks before object materialization.
- No `QsdbProjectStore` writer/atomic-save/backup behavior change.
- No native BricsCAD project lifecycle or Save/SaveAs runtime work.
- No primary-ID canonicality expansion in this lane.
- No GitHub Actions dispatch or V25 runtime qualification.

## Delivered behavior

- Non-empty root active Zone/Floor ids must already be unpadded before the loader can normalize them.
- Non-empty element Family/Floor/Zone references must already be unpadded.
- Persisted source-handle and dependency text must be non-empty and unpadded.
- Empty optional root/element relation ids remain allowed.
- Existing map-key canonicality and XML structural validation remain unchanged.

## Commits

- Registration: `2b7ea59f0d05a7bf8b0aa7cde5e5f93b877776c0` — `chore(agent): claim qsdb relation read canonicality`.
- Implementation: `c559eab27cb07d13c704f2283734b46eca0a595d` — `fix(persistence): reject padded persisted relation ids`.
- Regression: `7eb12faeb7b26aeb8472f36c7ac74ef1fb2e65c1` — `test(persistence): guard persisted relation canonicality`.

## Validation actually performed

- Inspected the exact implementation diff; it only adds root/element relation canonicality checks plus source-handle/dependency text validation in `QsdbProjectXmlSchemaValidator`.
- Re-fetched the validator from current remote `main` and confirmed all intended checks are present.
- Re-fetched the focused smoke from current remote `main`; it covers padded active Floor, padded element Family, padded source handle, padded dependency, empty source handle and a canonical relation/source roundtrip load.
- The smoke auto-registers with a module initializer and does not edit the shared smoke registration file.
- No force-push was used; unrelated concurrent commits remain intact.
- No GitHub Actions were dispatched.
- This hosted environment has no local .NET SDK/compiler and no licensed BricsCAD V25 runtime, so no unexecuted build/runtime PASS is claimed. This is a persistence/Core contract and introduces no new native V25 runtime scenario.

## Completion condition

Satisfied: current `main` rejects padded persisted relation/source identities before loader trimming, focused deterministic regression coverage is present, and this claim is closed `COMPLETED`.
