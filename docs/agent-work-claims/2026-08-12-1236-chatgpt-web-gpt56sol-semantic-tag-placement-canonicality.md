# Work claim — Semantic Tag placement snapshot canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-semantic-tag-placement-canonicality`
- Registered: `2026-08-12T12:36:00+07:00`
- Completed: `2026-08-12T12:40:00+07:00`
- Baseline main SHA: `65a61d91a5ffbeb4bbc426b367c9cf1d255e16fa`
- Priority: P1 — writer-owned Semantic Tag placement snapshots must preserve exact serialization.
- Task Key: `CORE-SEMANTIC-TAG-PLACEMENT-CANONICALITY`

## Confirmed defect

`SemanticTagBuilder.Build(...)` persists `GeneratedSemanticTagTextHeightM` and `GeneratedSemanticTagPositionX/Y/Z` with `double.ToString("R", CultureInfo.InvariantCulture)`, and persists `GeneratedSemanticTagPositionScope` as the exact `DrawingLocalWcs` token. `GeneratedSemanticTagHealthService` previously validated the numeric values only after trimming and validated scope through a trimmed property helper. Alternate writer-impossible spellings such as `0.180`, `0.0`, or padded ` DrawingLocalWcs ` could therefore pass health.

## Completed implementation

- Claim commit: `7b0ac3733a216729a6fa3c9a3bc237321d7c85e2`.
- Source commit: `e20e0ce7218bbcf83db4cf855e1366b4339ac4ab`.
- Smoke commit: `13480bf09c8a74a8023d3740f4b3811d9f644ee6`.
- PR #889 squash merge: `4d3cd692ffbe3e44dd1bab0e615b749ba87ae608`.
- Merged source blob read back from `main`: `5acecbc23acb05f9ea2d45864d651214e6368585`.
- Merged smoke blob read back from `main`: `544c546cbc8cc76b17eb3336c6be736e66100321`.
- `main` readback immediately after merge was `4d3cd692ffbe3e44dd1bab0e615b749ba87ae608`, so the merge is the current verified ancestor/root of the snapshot.

## Final contract

- A finite positive text height must use exact round-trip invariant spelling or emits `SEMANTIC_TAG_TEXT_HEIGHT_NON_CANONICAL` as Error.
- A finite X/Y/Z position component must use exact round-trip invariant spelling or emits `SEMANTIC_TAG_POSITION_NON_CANONICAL` as Error.
- Scope classification remains backward compatible: values whose trimmed text is not exactly `DrawingLocalWcs` remain `SEMANTIC_TAG_POSITION_SCOPE_INVALID`; a padded exact token emits `SEMANTIC_TAG_POSITION_SCOPE_NON_CANONICAL` as Error.
- Existing invalid/nonfinite/nonpositive diagnostics retain precedence and do not receive canonicality noise.
- Exact writer-owned placement metadata preserves existing behavior.
- Rotation, owner, template and rendered text logic remain unchanged.

No GitHub Actions were dispatched. No full local .NET build PASS, executable smoke PASS, or BricsCAD V25/V26 runtime PASS is claimed for this lane.
