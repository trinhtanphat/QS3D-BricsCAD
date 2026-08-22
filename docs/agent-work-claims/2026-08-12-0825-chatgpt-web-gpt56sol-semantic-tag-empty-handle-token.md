# Work claim — Semantic tag empty generated-handle token

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T08:25:00+07:00`
- Baseline main SHA: `4200277050bffa3389fe23d70dde2db74557c918`
- Priority: evidence-driven Core health fail-visible regression

## Reason

`GeneratedSemanticTagHealthService.ParseHandles()` currently splits `GeneratedSemanticTagHandles` with `StringSplitOptions.RemoveEmptyEntries` even though its validation branch explicitly checks `handle.Length == 0`. Delimiter-empty tokens are therefore silently discarded and cannot raise `SEMANTIC_TAG_HANDLE_INVALID`.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedSemanticTagHealthService.cs`
- focused deterministic preflight for empty Semantic Tag generated-handle tokens
- this claim file

## Excluded scope

- No tag rendering, template, ownership, position, CAD runtime or command behavior changes.
- No null-element/redaction work; those claims are already completed separately.
- No GitHub Actions dispatch and no BricsCAD runtime claim.

## Validation plan

- Preserve delimiter-empty/whitespace-empty tokens in `ParseHandles()` with `StringSplitOptions.None`.
- Keep the existing explicit empty/hex invalid branch, duplicate detection and valid-handle set semantics unchanged.
- Pin leading, interior, trailing and whitespace-empty malformed forms.
- Re-fetch source/preflight from current `main` before closure.

## Completion condition

Current `main` reports empty Semantic Tag generated-handle tokens as `SEMANTIC_TAG_HANDLE_INVALID`, deterministic regression coverage prevents silent-drop behavior from returning, and this claim is marked `COMPLETED` with exact SHAs.
