# Agent work claim — Native Table zero-row metadata integrity

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12 (UTC+7)
- Status: `COMPLETED`
- Baseline main SHA observed: `cd05005f3c0d6fd2abb381e1db822777f7631131`
- Scope: fail closed on impossible persisted zero-row metadata for project-owned native documentation Tables.
- Files reserved during implementation:
  - `src/QS3D.BricsCAD.V25/Cad/ProjectOwnedNativeTableArtifactService.cs` — persisted `RowCount` validation only
  - `scripts/preflight-native-table-zero-row-metadata.py`
  - this claim file for close-out

## Confirmed defect

`ValidateSnapshot(...)` requires every generated native documentation Table snapshot to contain `1..5000` rows, and `Build(...)` persists `snapshot.Rows.Count` into the owned Table `RowCount` metadata. `ValidatePersistedState(...)` accepted `RowCount = 0` because it rejected only `rows < 0`. Zero was therefore an impossible builder-produced state that could pass persisted-state validation and reach replace/remove/health paths instead of being rejected as malformed metadata.

## Completed contract

1. Persisted `RowCount` now matches the builder invariant and must be within `1..MaxRows`.
2. Zero, negative, non-numeric and over-limit row counts fail closed.
3. Valid native Table ownership, CAD lifecycle, TableStyle/format, coordinates, fingerprints and command behavior remain unchanged.
4. This does not claim BricsCAD V25/V26 runtime qualification or close issue #77.

## Completion evidence

- Claim commit: `27111fb1370887380f35334ecbb076b664ea039f`
- Source fix: `fa8d36fb718de348acfe7fefde751119a974d0c5`
- Static regression gate: `aa44a2dac520204fc69f8e4537153ef0a66abf21`
- Connector-side source review confirmed the source condition is now `rows <= 0 || rows > MaxRows` and the legacy `rows < 0` acceptance was removed.
- The Python preflight was committed but not executed in this web session. No GitHub Actions, local compile, or licensed BricsCAD V25/V26 runtime PASS is claimed.

Reservation released.
