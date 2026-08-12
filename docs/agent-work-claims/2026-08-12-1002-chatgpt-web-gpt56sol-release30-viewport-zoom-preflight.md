# Work claim — release #30 viewport zoom preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release30-viewport-zoom-preflight`
- Registered: `2026-08-12T10:02:00+07:00`
- Baseline main SHA: `bcc3d13fca83ee747cec362945883bc6686b3a08`
- Priority: QS3D Cloud V25 Preview Build & Release #30 reports `TryZoomSelection/WorldToDisplay boundary is missing` because the static gate still searches a private helper signature after `TryZoomSelection` was intentionally exposed as internal while its DCS framing behavior remains unchanged.

## Reserved scope

Reconcile only `scripts/preflight-viewport-zoom.py` with the current `internal static bool TryZoomSelection(Document document)` signature. Preserve ViewportCommands production behavior unchanged.

## Canonical evidence

- `ViewportCommands.TryZoomSelection` is currently `internal static bool TryZoomSelection(Document document)`.
- It still gets the current view, builds `worldToDisplay = WorldToDisplay(view)`, transforms every geometric extent before union/framing, rejects non-finite extents, preserves current view direction/target/twist and sets only center/width/height.
- `WorldToDisplay(ViewTableRecord view)` remains a private helper immediately after the zoom method and retains PlaneToWorld/Displacement/Rotation/inverse construction.
- Model-space commands still use idempotent `EnsureTiledModelSpace` and do not blindly call `SwitchToModelSpace()`.

## Expected surfaces

- `scripts/preflight-viewport-zoom.py`
- this claim file for close-out

## Excluded scope

- No edits to ViewportCommands.cs, viewport behavior, command wiring or model-space logic.
- No unrelated run #30 failures, GitHub Actions dispatch, build/release publication or BricsCAD runtime qualification.

## Validation plan

- Require the current `internal static bool TryZoomSelection(Document document)` signature.
- Slice the zoom body from the current signature to `private static Matrix3d WorldToDisplay` and preserve the existing transform-before-union and no-camera-mutation assertions.
- Preserve all DCS tokens, command uniqueness, finite/min-span and tiled-model-space checks.
- Re-fetch exact gate before write, read back after commit, verify ancestry and close with exact SHA.

## Coordination

Repository search found no active reservation for viewport zoom/TryZoomSelection or this preflight.

## Completion condition

The viewport zoom gate follows the current helper visibility without weakening WCS→DCS framing or model-space safety, is pushed to `main`, and this claim is closed with exact evidence.
