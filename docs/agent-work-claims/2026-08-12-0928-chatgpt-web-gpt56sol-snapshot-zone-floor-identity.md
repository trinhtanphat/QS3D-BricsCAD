# Work claim — Snapshot Zone/Floor rollback identity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-snapshot-zone-floor-identity`
- Registered: `2026-08-12T09:28:00+07:00`
- Baseline main SHA: `640b41b4178a290b06f45aae1b166a007cd8243b`
- Regression commit: `7c553f8cf0b07915ced53ab833aff495aedbdc3a`
- Completed source commit: `27801abeae7795e49746be5f08b9e47ac12b8fd2`
- Readback main SHA before close-out: `2afd127c27924d9574921afe9a5bb145696d8a07`
- Priority: P1 transaction rollback identity integrity found during owner-requested `continue all` audit.

## Confirmed defect

`ProjectStateSnapshot` already preserved captured `ProjectElement` and `ProjectFamily` instances during rollback into the exact `ProjectState` that was captured, but `Zones` and `Floors` were always cleared and recreated. Pre-transaction references returned by `FindZone(...)` / `FindFloor(...)` therefore became stale after rollback even when the same semantic identities existed at capture time.

## Implemented contract

1. Snapshot capture now records canonical Zone and Floor objects in case-insensitive id dictionaries alongside Family/Element references, rejecting null/empty/duplicate identities consistently.
2. Restore into the exact captured `ProjectState` reuses captured Zone/Floor instances, restores Zone `Name` and Floor `Name`/`ElevationM` in place, reinserts captured objects removed after capture, and removes post-capture additions.
3. `FindZone(...)` / `FindFloor(...)` therefore continue to resolve to the original pre-transaction objects after same-project rollback.
4. `CreateDetachedCopy(...)` and restore into a different same-id `ProjectState` still clone Zone/Floor objects and never alias the captured canonical instances.
5. Existing Family/Element identity behavior and project persistence-state restoration are preserved.
6. Focused smoke coverage locks same-project identity/value restoration plus detached/foreign non-aliasing for both Zone and Floor.

## Verification

- Current-main source readback confirmed captured Zone/Floor dictionaries, exact-project preservation selection, in-place copy helpers and detached cloning.
- Current-main smoke readback confirmed same-project remove/reinsert/new-item/value restoration plus detached/foreign isolation.
- `27801abeae7795e49746be5f08b9e47ac12b8fd2...main` compared as `ahead` with the source commit as merge base; seven subsequent concurrent commits touched unrelated release/reporting/curtain/opening/interchange files.
- Smoke source was committed but not executed from this remote connector session. Full Core smoke execution/build and GitHub Actions were not run; no PASS is fabricated.
- This is Core transaction/persistence work and makes no licensed BricsCAD runtime claim.

## Excluded

- No further Element/Family snapshot behavior changes beyond preserving their existing contract.
- No ProjectSession, QSDB schema/token, adapter/UI, installer or release changes.
