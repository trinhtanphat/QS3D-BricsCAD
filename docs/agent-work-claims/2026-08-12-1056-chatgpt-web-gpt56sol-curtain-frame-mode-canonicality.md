# Work claim — Curtain Frame generated mode canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-curtain-frame-mode-canonicality`
- Registered: `2026-08-12T10:56:00+07:00`
- Baseline main SHA: `e99299de298fec1412f1c17f0ea562b968f29d46`
- Priority: P1 — generated Curtain Frame mode metadata must preserve the exact writer-owned mode token.
- Task Key: `CORE-CURTAIN-FRAME-MODE-CANONICALITY`

## Confirmed defect

The two Curtain Frame writers own exactly four persisted mode spellings:

- `CurtainWallFrameSolidBuilder`: `LineFrameOverlay` / `LineFrameOverlay.OpeningAware`
- `CurtainWallPathFrameSolidBuilder`: `PathFrameOverlay` / `PathFrameOverlay.OpeningAware`

Both writers assign one of those constants directly to `GeneratedCurtainFrameMode`. `GeneratedCurtainFrameHealthService` previously trimmed the stored value and compared it case-insensitively, allowing padded/case-varied aliases to pass mode health even though no writer emits those spellings.

## Completed implementation

- Claim commit: `794c2c2aeec1ee06332b5dcac080b922200cad49`.
- Branch source commit: `702433290e6fd4f737657c46c79329194dabcbe4`.
- Branch smoke commit: `8dbcc550f6bf42beb82c959c821a1b08621bbe16`.
- PR: `#786` (`chatgpt-curtain-frame-mode-canon-20260812`).
- Squash merge on `main`: `2efc361dc2c106a276636863a5cd2b08070e586c`.
- Merged source and `GeneratedCurtainFrameModeCanonicalitySmoke.cs` were read back from `main`.
- Ancestry was verified from squash merge `2efc361dc2c106a276636863a5cd2b08070e586c` to `main` snapshot `c43285b4da7dc199b23fbfc8bffb40dcdd6bab2b`; the intervening commit did not touch this lane.

## Resulting contract

- Case-varied or outer-whitespace aliases of the four supported modes emit `CURTAIN_FRAME_MODE_NON_CANONICAL` as `HealthSeverity.Error`.
- Existing `CURTAIN_FRAME_MODE_INVALID` remains the diagnostic for genuinely unsupported normalized values.
- Existing opening-aware/count mismatch behavior continues to operate on normalized semantic mode.
- Exact writer-owned mode tokens preserve existing behavior.
- Inspection remains read-only and deterministic.

## Verification boundary

No GitHub Actions were dispatched. No full local .NET build PASS and no BricsCAD V25/V26 runtime PASS are claimed by this lane.
