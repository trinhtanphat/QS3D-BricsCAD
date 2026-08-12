# Work claim — Generated Semantic Tag handle token canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-semantic-tag-handle-canonicality`
- Registered: `2026-08-12T10:44:00+07:00`
- Completed: `2026-08-12T10:46:00+07:00`
- Baseline main SHA: `df0a2626a3fb99bd70af57e75660a3fd0a0f496e`
- Pull Request: `#775`
- Reviewed head: `9273a012fec7781388e334568794c3751552341c`
- Merge SHA: `113cf5c5664f20981bc0cb556d3db3a14a61369a`
- Priority: P1 — malformed persisted generated Semantic Tag handles must be fail-visible instead of silently canonicalized by diagnostics
- Task Key: `CORE-SEMANTIC-TAG-HANDLE-CANONICALITY`

## Confirmed defect

`GeneratedSemanticTagHealthService.ParseHandles(...)` preserved delimiter tokens with `StringSplitOptions.None` but immediately trimmed each token before hexadecimal validation. A persisted writer-owned token such as `" A "` therefore passed as valid `"A"` with no canonicality Error.

## Completed implementation

- Valid non-empty Semantic Tag hex handle tokens with surrounding whitespace now emit Error `SEMANTIC_TAG_HANDLE_NON_CANONICAL`.
- Duplicate/source-handle/owner/render/position checks continue using the trimmed handle set.
- Lower-case canonical hex remains accepted; no casing rule was added.
- Empty delimiter tokens continue to emit existing `SEMANTIC_TAG_HANDLE_INVALID` diagnostics.
- Tag generation/rendering, V25 commands, persistence, ownership policy and unrelated diagnostics were not modified.

## Regression evidence

`tests/QS3D.Core.SmokeTests/GeneratedSemanticTagHandleCanonicalitySmoke.cs` covers padded handle canonicality, lowercase canonical control and preservation of `A;;B` empty-token invalid behavior.

PR #775 exact diff was reviewed as two files only (88 additions, 1 deletion). Guarded squash merge succeeded as `113cf5c5664f20981bc0cb556d3db3a14a61369a`. Merged-main readback confirms source blob `076fdcdc4f9ca4bbf31ba1bc30254fd91dc5aa17` and smoke blob `b84f9fd8c881ce23e2c0e3b17ae29aa4f721a855`. Comparison from the merge SHA to moving `main` reported `behind_by=0` with merge base equal to the merge SHA.

## Validation boundary

No GitHub Actions, full build, executable smoke or licensed BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

Satisfied: padded generated Semantic Tag handle tokens are fail-visible without changing downstream trimmed-handle semantics, focused regression evidence is merged to current `main`, and this claim is closed `COMPLETED` with exact PR/merge/readback evidence.
