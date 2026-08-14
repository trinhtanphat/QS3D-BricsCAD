# Work claim — QSDB raw relation canonicality smoke reconciliation

- Status: `RELEASED_DUPLICATE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-14T16:14:00+07:00`
- Released: `2026-08-14T16:18:00+07:00`
- Baseline main SHA: `552aa7433ab6fe438076337bc9ba7c86cb9c1cbe`
- Priority: next deterministic full Core smoke blocker reported independently by PRs #1262 and #1266

## Confirmed fixture drift

`QsdbRelationIdentityCanonicalSmoke` is intended to prove that QSDB Save rejects raw noncanonical optional relation ids without silently normalizing the in-memory project. Current public `ProjectState.ActiveFloorId` / `ActiveZoneId` and `ProjectElement.FamilyId` / `FloorId` / `ZoneId` setters canonicalize those values on assignment, so the current padded setup no longer reaches the persistence boundary in a noncanonical state.

Production `QsdbProjectStore.ValidateProject(...)` still fail-closes through `ValidateOptionalCanonicalValue(...)` for the two project active-context ids and the three element relation ids. The source gate also locks those validators. Therefore this lane must restore the legacy/corrupt raw-state regression rather than weaken production validation or change public canonical setters.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/QsdbRelationIdentityCanonicalSmoke.cs`
- this claim document only

The intended approach was to keep the padded public-setter literals, prove current canonicalization first, then inject only the relevant private backing field through test-local reflection to represent raw legacy/corrupt persisted state before Save.

## Release reason

An earlier owner had already registered the exact same raw-relation fixture lane through claim PR `#1267`, then merged the validated implementation/gate update through PR `#1269` as main SHA `e367f9193322c35819fcf44a4ce3dd66bfe49d97`. Their closeout PR `#1272` merged as `d0918936f791494a468e61f0056e23284ac38340` and records Core Release 0/0, five focused QSDB gate PASS results, and full Core smoke advancing beyond this fixture.

This later claim PR `#1270` was therefore a duplicate. The implementation PR `#1273` was explicitly closed without merge after compare showed the winning changes had already modified both the smoke and its focused gate. No stale implementation from this lane was merged into `main`.

## Preserved outcome

- Winning QSDB implementation remains authoritative; this claim releases ownership immediately.
- No production, QSDB schema/migration, native/LOCAL, workflow, release, private-data, or Actions changes were made by this duplicate lane.
- The next independent full-smoke blocker reported by the winning closeout is `QuantityRuleFamilyIdCanonicalitySmoke.PaddedFamilyIdFailsBeforeStaleCleanup`.
