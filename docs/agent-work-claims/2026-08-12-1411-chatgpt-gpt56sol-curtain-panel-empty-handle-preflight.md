# Work claim — Curtain Panel empty-handle preflight sync

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T14:11:00+07:00`
- Baseline main SHA: `dd1aff88224a592bb3ad7babe01f9e35781fdf5f`
- Priority: `P0 source/static gate regression — focused Curtain Panel empty-token preflight still hard-codes a removed local variable shape after handle-identity hardening`

## Reserved scope

Reconcile `scripts/preflight-curtain-panel-empty-handle-token.py` with the current `GeneratedCurtainPanelHealthService` implementation while preserving the underlying contract: delimiter-empty handle tokens must remain visible to validation and fail as `INVALID_CURTAIN_PANEL_GENERATED_HANDLE` rather than being removed before inspection.

## Expected surfaces

- `scripts/preflight-curtain-panel-empty-handle-token.py`
- Read-only verification of `src/QS3D.Core/Diagnostics/GeneratedCurtainPanelHealthService.cs`

## Excluded scope

- Curtain Panel production health semantics, numeric handle identity, live-handle ownership, metadata canonicality, runtime panel generation, probes, or LOCAL-only work.
- Current DependencyGraph dirty-order claim and all other active source lanes.
- GitHub Actions dispatch, BricsCAD runtime qualification, packaging/release.

## Evidence

PR #868 recorded this focused gate as known stale because it required the removed source token `var handle = token.Trim();`. Current production source deliberately keeps `StringSplitOptions.None`, captures `handleText = token ?? string.Empty`, trims that to `handle`, then rejects empty/invalid hexadecimal tokens before numeric-identity normalization. The behavior is correct; the static token shape is stale.

## Validation plan

- Update only the stale static source-shape expectations.
- Continue requiring `StringSplitOptions.None`, explicit trim, empty/invalid rejection, and `INVALID_CURTAIN_PANEL_GENERATED_HANDLE`.
- Forbid `RemoveEmptyEntries` as before.
- Read back the exact pushed diff; do not claim executable preflight/build/Actions/V25 runtime PASS unless actually run.

## Completion condition

A pushed `main` implementation updates only the focused preflight, followed by this claim marked `COMPLETED` with exact implementation SHA.
