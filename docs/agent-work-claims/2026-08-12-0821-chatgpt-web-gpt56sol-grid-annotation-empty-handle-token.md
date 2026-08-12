# Work claim — Grid annotation empty generated-handle token

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T08:21:00+07:00`
- Baseline main SHA: `242a0995765dc2b47616789ad0bd6f92ad25f67e`
- Priority: evidence-driven Core health fail-visible regression

## Reason

`GeneratedGridAnnotationHealthService.Inspect()` currently removes empty entries and then filters trimmed empty tokens from `GeneratedGridAnnotationHandles`. A metadata stream containing the expected six valid handles plus an extra delimiter-empty token can therefore still report the expected count and never emit `GRID_ANNOTATION_HANDLE_INVALID`. Empty/whitespace-only metadata as a whole already has a dedicated `GRID_ANNOTATION_HANDLES_EMPTY` contract and must remain so.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedGridAnnotationHealthService.cs`
- focused deterministic preflight for empty Grid annotation handle tokens
- this claim file

## Excluded scope

- No Grid annotation generation, CAD/XData ownership, naming, sizing or runtime behavior changes.
- No null-element work; that claim is already completed separately.
- No GitHub Actions dispatch and no BricsCAD runtime claim.

## Validation plan

- Preserve the existing whole-value blank metadata warning (`GRID_ANNOTATION_HANDLES_EMPTY`).
- For nonblank handle metadata, preserve delimiter-empty/whitespace-empty tokens with `StringSplitOptions.None` and classify each empty token as `GRID_ANNOTATION_HANDLE_INVALID` before distinct-count accounting.
- Preserve nonempty valid/invalid hexadecimal, duplicate, source-handle and expected-count semantics.
- Pin six-valid-plus-empty, leading, trailing and whitespace-empty malformed forms.
- Re-fetch source/preflight from current `main` before closure.

## Completion condition

Current `main` fails visible on embedded/leading/trailing empty Grid annotation handle tokens without regressing the whole-value-empty warning, deterministic coverage locks the contract, and this claim is marked `COMPLETED` with exact SHAs.
