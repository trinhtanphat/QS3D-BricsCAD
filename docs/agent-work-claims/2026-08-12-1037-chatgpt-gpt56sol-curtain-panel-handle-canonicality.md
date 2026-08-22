# Work claim — Generated Curtain Panel handle token canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-curtain-panel-handle-canonicality`
- Registered: `2026-08-12T10:37:00+07:00`
- Completed: `2026-08-12T10:46:00+07:00`
- Baseline main SHA: `3ae1b1c4050a1033083208ac19478c2e3c85b887`
- Pull Request: `#765`
- Reviewed head: `9b162e8620a8bc3a6cacdf5504aaac7c14203c27`
- Merge SHA: `49acd4c3c9f6300f2493eccf109e93552fa86ce6`
- Priority: P1 — malformed persisted generated Curtain Panel owner handles must be fail-visible instead of silently canonicalized by diagnostics
- Task Key: `CORE-CURTAIN-PANEL-HANDLE-CANONICALITY`

## Confirmed defect

`GeneratedCurtainPanelHealthService.Inspect(...)` preserved delimiter tokens with `StringSplitOptions.None` but trimmed each `GeneratedCurtainPanelHandles` token before validating it. A persisted token such as `" A "` therefore passed as a valid hexadecimal handle with no canonicality Error.

## Completed implementation

- Valid non-empty Curtain Panel hex handle tokens with surrounding whitespace now emit Error `CURTAIN_PANEL_GENERATED_HANDLE_NON_CANONICAL`.
- Duplicate, generated-ownership, SourceHandles, liveness, count/build-state/geometry/mode/category/stale checks continue using the trimmed handle.
- Lower-case canonical hex remains accepted; no casing rule was added.
- Empty delimiter tokens continue to emit existing `INVALID_CURTAIN_PANEL_GENERATED_HANDLE` diagnostics.
- Curtain panel builders/fingerprint/planner, generated ownership policy, CAD runtime code, persistence and unrelated diagnostics were not modified.

## Regression evidence

`tests/QS3D.Core.SmokeTests/GeneratedCurtainPanelHandleCanonicalitySmoke.cs` covers padded handle + trimmed live lookup/ownership, lowercase canonical control and preservation of `A;;B` empty-token invalid behavior.

PR #765 exact diff was reviewed as two files only (105 additions, 1 deletion). GitHub initially rejected guarded merge attempts while `main` moved, but later completed the originally reviewed head as merge `49acd4c3c9f6300f2493eccf109e93552fa86ce6`. A fresh-base recovery PR #766 carrying the same diff was closed unmerged to avoid duplication. Merged-main readback confirms source blob `974d1d30ecd9e9fc073b6547f55c17b76a0955fc` and smoke blob `8a135de3ae63aa7bc6b707e0841c192354d93921`. Comparison from the merge SHA to moving `main` reported `behind_by=0` with merge base equal to the merge SHA.

## Validation boundary

No GitHub Actions, full build, executable smoke or licensed BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

Satisfied: padded generated Curtain Panel handle tokens are fail-visible without changing downstream trimmed-handle semantics, focused regression evidence is merged to current `main`, and this claim is closed `COMPLETED` with exact PR/merge/readback evidence.
