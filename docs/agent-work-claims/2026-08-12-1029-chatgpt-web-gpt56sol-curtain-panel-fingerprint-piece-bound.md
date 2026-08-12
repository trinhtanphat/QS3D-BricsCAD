# Work claim — Curtain panel fingerprint piece bound

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-curtain-panel-fingerprint-piece-bound-20260812-1029`
- Registered: `2026-08-12T10:29:00+07:00`
- Baseline main SHA: `c67af958f080eeb1fa970f3064860c7cdcc222da`
- Priority: P1 deterministic Core resource-bound integrity

## Reserved scope

Harden `CurtainWallPanelFingerprint.Compute(...)` so the public `IReadOnlyList<CurtainWallPanelPiece>` input cannot bypass `MaxPieces` through an inconsistent or mutating enumerable after the current `Count` preflight. Snapshot exactly the indexed, bounded list before validation/sorting/fingerprint construction, and never enumerate caller-owned `Pieces` directly.

## Expected surfaces

- `src/QS3D.Core/Geometry/CurtainWallPanelFingerprint.cs`
- `tests/QS3D.Core.SmokeTests/CurtainWallPanelFingerprintAreaFiniteSmoke.cs`
- this claim file for close-out

## Excluded scope

- No changes to Curtain panel layout/materialization, ownership/XData, native BricsCAD geometry, regeneration, Health, signed-zero/area semantics, or V25/V26 runtime behavior.
- No changes to `CurtainWallOpeningPanelPlanner.MaxOutputPieces` or product limits.
- No GitHub Actions/build/release dispatch.

## Validation plan

Add focused deterministic smoke coverage with a hostile `IReadOnlyList<CurtainWallPanelPiece>` whose `Count` is within the budget but whose enumerator exposes extra/throwing data. The fingerprint path must use bounded indexed access only, preserve deterministic fingerprints for ordinary lists, and retain all existing validation.

## Coordination

Recent Curtain panel fingerprint work completed signed-zero and finite-area contracts. This reservation is limited to caller-owned piece enumeration/resource-bound integrity and does not overlap those completed semantics or current licensing/health/LOCAL_ONLY lanes.

## Completion condition

Current `main` contains the bounded indexed snapshot fix plus focused smoke coverage; the source/test are read back after integration; this claim is updated to `COMPLETED` with exact implementation/merge evidence. No CI or licensed BricsCAD runtime PASS is claimed.