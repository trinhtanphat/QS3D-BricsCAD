# Work claim — Curtain Frame generated mode canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-curtain-frame-mode-canonicality`
- Registered: `2026-08-12T10:56:00+07:00`
- Baseline main SHA: `e99299de298fec1412f1c17f0ea562b968f29d46`
- Priority: P1 — generated Curtain Frame mode metadata must preserve the exact writer-owned mode token.
- Task Key: `CORE-CURTAIN-FRAME-MODE-CANONICALITY`

## Confirmed defect

The two Curtain Frame writers own exactly four persisted mode spellings:

- `CurtainWallFrameSolidBuilder`: `LineFrameOverlay` / `LineFrameOverlay.OpeningAware`
- `CurtainWallPathFrameSolidBuilder`: `PathFrameOverlay` / `PathFrameOverlay.OpeningAware`

Both writers assign one of those constants directly to `GeneratedCurtainFrameMode`. `GeneratedCurtainFrameHealthService` currently trims the stored value and compares it case-insensitively. Persisted aliases such as `" lineframeoverlay "` or `"pathframeoverlay.openingaware"` can therefore pass mode health even though no writer emits those spellings.

## Non-overlap check

Recent claim/commit search found no Curtain Frame mode canonicality lane. The completed Curtain Frame handle canonicality lane owns only generated handle token spacing. Health-command error-redaction owns only the BricsCAD command wrapper. Other active Curtain/Revision/XLSX/Reporting lanes do not reserve this mode validation.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs`
- one focused Core smoke regression for mode canonicality
- this claim file

Do not modify Curtain Frame builders, handle/fingerprint/geometry/count/opening calculations, native ownership/XData, persistence format, command wrappers, or BricsCAD runtime code.

## Intended contract

- A stored mode that normalizes case/outer whitespace to one of the four writer-owned modes emits `CURTAIN_FRAME_MODE_NON_CANONICAL` as `HealthSeverity.Error`.
- Existing `CURTAIN_FRAME_MODE_INVALID` remains the diagnostic for genuinely unsupported normalized values.
- Existing opening-aware/count mismatch behavior continues to operate on the normalized semantic mode so malformed spelling cannot hide a real mismatch.
- Exact writer-owned mode tokens preserve existing behavior.
- Inspection remains read-only and deterministic.

## Completion condition

Case-varied/padded mode aliases are fail-visible without changing invalid/opening-mismatch semantics, focused smoke coverage pins line/path aliases plus invalid and canonical controls, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
