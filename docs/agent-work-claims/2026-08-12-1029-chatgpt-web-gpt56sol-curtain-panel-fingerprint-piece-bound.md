# Work claim — Curtain panel fingerprint piece bound

- Status: `COMPLETED`
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

## Completion evidence

- Claim registration: `4874abb8444a7b2caceab068c49679321633f892`.
- Source branch fix: `d7c48540c7cc3a57617e1873dc88923f24bd7768`.
- Focused smoke source: `0322c278fb47a38a5d15b02043261e57ba8fca5d`.
- PR: `#759`.
- Squash integration on `main`: `7ea97ab7a28c60629b4fce2fe0a7b080821d8d84`.
- Post-merge readback confirmed `main` snapshots the captured bounded count via indexed access before sorting/hashing and contains `BoundedIndexedSnapshotDoesNotTrustEnumerator()`.
- Existing signed-zero, finite-area, deterministic ordering/hash and `MaxPieces` semantics remain in the merged source.

## Validation boundary

Focused smoke coverage was added and read back from `main`, but it was not executed in this remote session. No GitHub Actions, local .NET build, BricsCAD V25/V26 runtime, release or signing PASS is claimed.