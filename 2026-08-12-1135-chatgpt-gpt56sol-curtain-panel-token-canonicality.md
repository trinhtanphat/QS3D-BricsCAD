# Work claim — Curtain Panel mode/source-kind canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-curtain-panel-token-canonicality-20260812-1135`
- Registered: `2026-08-12T11:35:00+07:00`
- Completed: `2026-08-12T11:39:00+07:00`
- Priority: P1 generated-output health parity

## Confirmed defect

Native Curtain Panel writers persist exact writer-owned enum-like tokens: `LinePanelSolids`, `LinePanelSolids.OpeningAware`, `PathPanelSolids`, `PathPanelSolids.OpeningAware`, and path `GeneratedCurtainPanelSourceKind=OpenPolyline`. `GeneratedCurtainPanelHealthService.Mode(...)` trimmed and compared these tokens case-insensitively, so padded/case-varied aliases could remain health-clean.

## Integrated contract

Supported mode/source-kind tokens are still resolved semantically first, preserving existing invalid-token and opening-aware mismatch precedence. Resolved aliases must now match the exact writer-owned ordinal spelling or emit `CURTAIN_PANEL_MODE_NON_CANONICAL` / `CURTAIN_PANEL_PATH_SOURCE_KIND_NON_CANONICAL` as `HealthSeverity.Error`. Downstream path/opening checks continue using the resolved semantic value.

## Evidence

- PR: `#825`
- Squash merge: `c27e6b814a198bfa35be2ba8b2e3351669b6194b`
- Source read back from `main`: `src/QS3D.Core/Diagnostics/GeneratedCurtainPanelHealthService.cs`
- Regression read back from `main`: `tests/QS3D.Core.SmokeTests/GeneratedCurtainPanelTokenCanonicalitySmoke.cs`
- Regression covers padded/case-varied line/path modes, path source-kind aliases, exact canonical controls, and invalid-token precedence.

## Exclusions preserved

No build-state, handles, integers, fingerprint, floating metadata, stale logic, writers/native runtime, or persistence-format changes were made in this lane.

## Validation boundary

Source and smoke were read back from remote `main` after merge. No GitHub Actions/full build/executable smoke or BricsCAD V25/V26 runtime PASS is claimed without execution.
