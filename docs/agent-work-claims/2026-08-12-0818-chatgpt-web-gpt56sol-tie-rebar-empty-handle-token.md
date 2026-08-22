# Work claim — Tie rebar empty generated-handle token

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T08:18:00+07:00`
- Baseline main SHA: `d55050727de0ca4d238174625bbccc6624b09d61`
- Priority: evidence-driven Core health fail-visible regression

## Reason

`GeneratedTieRebarHealthService.Inspect()` currently builds its inspected handle array with `StringSplitOptions.RemoveEmptyEntries` and then filters `Where(x => x.Length > 0)`. Malformed persisted metadata such as `AA;;BB`, `;AA`, `AA;` or `AA; ;BB` therefore loses empty tokens before `INVALID_TIE_REBAR_GENERATED_HANDLE` validation. Health inspection should fail visible on malformed generated ownership metadata rather than silently normalize it.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedTieRebarHealthService.cs`
- focused deterministic preflight for empty Tie Rebar generated-handle tokens
- this claim file

## Excluded scope

- No tie generation, quantity, CAD replacement, spacing or engineering behavior changes.
- No null-element work; that claim is already completed separately.
- No ownership-index normalization change; only the inspected generated-handle stream is in scope.
- No GitHub Actions dispatch and no BricsCAD runtime claim.

## Validation plan

- Preserve delimiter-empty/whitespace-empty tokens in the inspected stream with `StringSplitOptions.None` and trimming without filtering them out.
- Explicitly classify empty tokens through the existing `INVALID_TIE_REBAR_GENERATED_HANDLE` issue code.
- Preserve valid hex, duplicate, ownership, live-solid and valid-count semantics.
- Pin leading, interior, trailing and whitespace-empty malformed forms.
- Re-fetch source/preflight from current `main` before closure.

## Completion condition

Current `main` reports empty Tie Rebar generated-handle tokens as `INVALID_TIE_REBAR_GENERATED_HANDLE`, deterministic regression coverage prevents silent-drop behavior from returning, and this claim is marked `COMPLETED` with exact SHAs.
