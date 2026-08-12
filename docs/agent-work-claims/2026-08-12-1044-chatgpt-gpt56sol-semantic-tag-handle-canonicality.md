# Work claim — Generated Semantic Tag handle token canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-semantic-tag-handle-canonicality`
- Registered: `2026-08-12T10:44:00+07:00`
- Last Updated: `2026-08-12T10:44:00+07:00`
- Baseline main SHA: `df0a2626a3fb99bd70af57e75660a3fd0a0f496e`
- Priority: P1 — malformed persisted generated Semantic Tag handles must be fail-visible instead of silently canonicalized by diagnostics
- Task Key: `CORE-SEMANTIC-TAG-HANDLE-CANONICALITY`

## Confirmed defect

`GeneratedSemanticTagHealthService.ParseHandles(...)` preserves delimiter tokens with `StringSplitOptions.None` but immediately trims each token before hexadecimal validation. A persisted writer-owned token such as `" A "` therefore passes as valid `"A"` with no canonicality Error. Sibling generated-output health providers make surrounding token whitespace fail-visible while continuing downstream checks with the trimmed handle.

## Coordination

Semantic Tag null-health, empty-handle-token, reference canonicality, fatal/render health and command error-redaction lanes are completed. The command-redaction claim explicitly excludes Core health/runtime semantics. Exact commit search found no Semantic Tag handle-token canonicality lane.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedSemanticTagHealthService.cs`
- `tests/QS3D.Core.SmokeTests/GeneratedSemanticTagHandleCanonicalitySmoke.cs`
- this claim file

## Intended contract

- A valid non-empty Semantic Tag hex handle token with surrounding whitespace emits Error `SEMANTIC_TAG_HANDLE_NON_CANONICAL`.
- Continue duplicate/source-handle/owner/render/position checks using the trimmed handle set.
- Lower-case canonical hex remains accepted; no casing rule is added.
- Empty delimiter tokens continue to emit existing `SEMANTIC_TAG_HANDLE_INVALID` diagnostics.
- Do not modify tag generation/rendering, V25 commands, persistence, ownership policy or unrelated diagnostics.

## Validation plan

Add an auto-registered Core smoke covering padded handle canonicality, lowercase canonical control and preservation of empty-token invalid behavior. Review exact PR diff, merge guarded on moving `main`, read back source/test and verify ancestry.

## Validation boundary

No GitHub Actions, full build, executable smoke or licensed BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.

## Completion condition

Padded generated Semantic Tag handle tokens are fail-visible without changing downstream trimmed-handle semantics, focused regression evidence is merged to current `main`, and this claim is closed with exact commit/PR evidence.
