# Work claim — Curtain frame empty generated-handle token

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T08:15:00+07:00`
- Baseline main SHA: `9cc952dc2457c558dca2d81ffbc366a202b365e7`
- Priority: evidence-driven Core health fail-visible regression

## Reason

`GeneratedCurtainFrameHealthService.Inspect()` splits `GeneratedCurtainFrameHandles` with `StringSplitOptions.RemoveEmptyEntries`, so malformed persisted metadata such as `AA;;BB`, `;AA` or `AA;` silently drops empty tokens before the existing `INVALID_CURTAIN_FRAME_GENERATED_HANDLE` branch can observe them. Health inspection should fail visible on malformed generated handle lists.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs`
- focused deterministic preflight for empty Curtain frame generated-handle tokens
- this claim file

## Excluded scope

- No frame generation/layout/config-fingerprint/runtime-CAD behavior changes.
- No null-element or redaction work; those claims are already completed separately.
- No ownership-index normalization change; only the inspected generated-handle stream is in scope.
- No GitHub Actions dispatch and no BricsCAD runtime claim.

## Validation plan

- Preserve delimiter-empty tokens in the inspected handle stream with `StringSplitOptions.None`.
- Keep valid hex, duplicate, ownership, live-solid and count semantics unchanged.
- Pin leading, interior, trailing and whitespace-empty token forms.
- Read back source/preflight from current `main` before closure.

## Completion condition

Current `main` reports delimiter-empty Curtain frame generated-handle tokens as `INVALID_CURTAIN_FRAME_GENERATED_HANDLE`, deterministic regression coverage prevents silent-drop behavior from returning, and this claim is marked `COMPLETED` with exact SHAs.
