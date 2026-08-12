# Agent work claim — Native Table zero-row metadata integrity

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12 (UTC+7)
- Status: `ACTIVE`
- Baseline main SHA observed: `cd05005f3c0d6fd2abb381e1db822777f7631131`
- Scope: fail closed on impossible persisted zero-row metadata for project-owned native documentation Tables.
- Files reserved:
  - `src/QS3D.BricsCAD.V25/Cad/ProjectOwnedNativeTableArtifactService.cs` — persisted `RowCount` validation only
  - focused static preflight for this invariant
  - this claim file for close-out

## Confirmed defect

`ValidateSnapshot(...)` requires every generated native documentation Table snapshot to contain `1..5000` rows, and `Build(...)` persists `snapshot.Rows.Count` into the owned Table `RowCount` metadata. `ValidatePersistedState(...)` currently accepts `RowCount = 0` because it rejects only `rows < 0`. Zero is therefore an impossible builder-produced state that can pass persisted-state validation and reach replace/remove/health paths instead of being rejected as malformed metadata.

## Contract

1. Persisted `RowCount` must match the builder invariant and be within `1..MaxRows`.
2. Zero, negative, non-numeric and over-limit row counts fail closed.
3. Valid native Table ownership, CAD lifecycle, TableStyle/format, coordinates, fingerprints and command behavior remain unchanged.
4. This does not claim BricsCAD V25/V26 runtime qualification or close issue #77.

## Validation/closure

Use an exact one-condition source fix plus focused static regression guard. No GitHub Actions dispatch and no local/native runtime PASS claim from this web session.
