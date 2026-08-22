# Work claim — Curtain frame empty generated-handle token

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T08:15:00+07:00`
- Completed: `2026-08-12T08:17:00+07:00`
- Baseline main SHA: `9cc952dc2457c558dca2d81ffbc366a202b365e7`
- Priority: evidence-driven Core health fail-visible regression

## Reason

`GeneratedCurtainFrameHealthService.Inspect()` split `GeneratedCurtainFrameHandles` with `StringSplitOptions.RemoveEmptyEntries`, so malformed persisted metadata such as `AA;;BB`, `;AA` or `AA;` silently dropped empty tokens before the existing `INVALID_CURTAIN_FRAME_GENERATED_HANDLE` branch could observe them. Health inspection now fails visible on malformed generated handle lists.

## Changed scope

- `src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs`
- `scripts/preflight-curtain-frame-empty-handle-token.py`
- this claim file

## Completion record

- Claim commit: `9c0a4ffe403b98167f790648aaec51333bdb7498`.
- Implementation commit: `693e29c49dbde8ece431e41ed410ed0eda1d92b3` — preserve delimiter-empty tokens in the inspected Curtain frame handle stream while retaining valid hex, duplicate, ownership, live-solid and count semantics.
- Regression commit: `99aca605d5fb73b96bac4125c6819df5b6b04353` — pin leading, interior, trailing and whitespace-empty token forms and forbid `RemoveEmptyEntries` in the inspected stream.

Validation actually performed:

- re-fetched current source and confirmed `GeneratedCurtainFrameHealthService.Inspect()` uses `StringSplitOptions.None` while the ownership-index normalization path remains unchanged;
- re-fetched the dedicated deterministic preflight and confirmed it locks the fail-visible empty-token contract;
- no GitHub Actions were dispatched or rerun;
- no repository `dotnet` test or BricsCAD runtime was executed in this hosted session.

## Excluded scope

- No frame generation/layout/config-fingerprint/runtime-CAD behavior changes.
- No null-element or redaction work; those claims are already completed separately.
- No ownership-index normalization change.

## Completion condition

Satisfied: current `main` reports delimiter-empty Curtain frame generated-handle tokens as `INVALID_CURTAIN_FRAME_GENERATED_HANDLE`, deterministic regression coverage prevents silent-drop behavior from returning, and this claim is released as `COMPLETED`.
