# Work claim — Snapshot duplicate identity integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-snapshot-duplicate-identity-integrity`
- Registered: `2026-08-12T09:33:00+07:00`
- Baseline main SHA: `4ea70225d91dbc07edfa256c8e29884156f2f932`
- Regression commit: `6dd689434e069d838f9368bd64ad0156b2ca5caf`
- Completed source commit: `bafdcc18baaa0eb79c9aa7397fdcd90da353cbfd`
- Readback main SHA before close-out: `eb56a4c5add6c2707ef5d3ff2fcfafd0f3515d15`
- Priority: P1 snapshot fail-closed integrity found during owner-requested `continue all` audit.

## Confirmed defect

`ProjectState.FindZone/FindFloor/FindFamily/FindElement/FindQuantityRule` and QSDB persistence treat duplicate semantic IDs as invalid, but public `ProjectStateSnapshot.CreateDetachedCopy(...)` previously validated only null entries before cloning. A malformed in-memory project could therefore be copied with duplicate semantic identities and fail only later during lookup or other integrity-sensitive work. QuantityRule duplicate IDs were not rejected by snapshot validation at all.

## Implemented contract

1. Snapshot `ValidateCollectionEntries(...)` still performs all existing null checks first.
2. It now rejects duplicate Zone, Floor, Family, Element and QuantityRule IDs case-insensitively before any target collection is mutated.
3. A shared `RequireUniqueIds(...)` also rejects missing/whitespace IDs if malformed objects reach the snapshot boundary.
4. Existing same-project Zone/Floor/Family/Element identity restoration remains unchanged.
5. Valid `CreateDetachedCopy(...)` behavior remains non-aliasing across all semantic collections, including QuantityRules.
6. Focused smoke coverage independently injects case-variant duplicate IDs into all five collections and confirms a valid project still clones all entries without reference aliasing.

## Verification

- Current-main source readback confirmed null preflight followed by duplicate-ID validation for all five semantic collections.
- Current-main smoke readback confirmed Zone/Floor/Family/Element/QuantityRule duplicate cases and valid detached-clone behavior.
- `bafdcc18baaa0eb79c9aa7397fdcd90da353cbfd...main` compared as `ahead` with the source commit as merge base; later concurrent changes touched unrelated reporting/legacy-preflight/grid smoke files.
- Smoke source was committed but not executed from this remote connector session. Full Core smoke execution/build and GitHub Actions were not run; no PASS is fabricated.
- This is Core snapshot integrity work and makes no licensed BricsCAD runtime claim.

## Excluded

- No rule engine/provenance changes.
- No QSDB schema/token, ProjectSession, adapter/UI, installer or release changes.
