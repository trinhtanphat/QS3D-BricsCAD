# Work claim — Curtain panel empty generated-handle token

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T08:12:00+07:00`
- Baseline main SHA: `a9faf00389de8e4d5140005ae2f25bb59aeeffac`
- Priority: evidence-driven Core health fail-visible regression

## Reason

`GeneratedCurtainPanelHealthService.Inspect()` currently splits `GeneratedCurtainPanelHandles` with `StringSplitOptions.RemoveEmptyEntries`. Malformed persisted metadata such as `AA;;BB`, `;AA` or `AA;` therefore discards empty tokens before the existing `INVALID_CURTAIN_PANEL_GENERATED_HANDLE` branch can observe them. The health contract should report malformed generated ownership metadata instead of silently normalizing it.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedCurtainPanelHealthService.cs`
- focused deterministic preflight for empty Curtain panel generated-handle tokens
- this claim file

## Excluded scope

- No curtain panel generation/layout/runtime-CAD behavior changes.
- No null-element or fingerprint work; those claims are already completed separately.
- No GitHub Actions dispatch and no BricsCAD runtime claim.

## Validation plan

- Preserve delimiter-empty tokens in the inspected handle stream with `StringSplitOptions.None`.
- Keep valid hexadecimal, duplicate, ownership, live-solid and count semantics unchanged.
- Add source regression coverage for leading, interior, trailing and whitespace-empty token forms.
- Re-fetch source before write and close the claim only after read-back from current `main`.

## Completion condition

Current `main` surfaces delimiter-empty Curtain panel generated-handle tokens as `INVALID_CURTAIN_PANEL_GENERATED_HANDLE`, regression coverage prevents silent-drop behavior from returning, and this claim is marked `COMPLETED` with exact SHAs.
