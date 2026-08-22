# Work claim — Semantic documentation persisted empty-payload integrity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-semantic-documentation-empty-payload-20260812-1529`
- Registered: `2026-08-12T15:29:00+07:00`
- Baseline main SHA: `9a3f6b5fee70d79233c01237d8e8cb783a1f52a1`
- Priority: P1 semantic documentation persistence fail-closed integrity
- Task Key: `CORE-SEMANTIC-DOCUMENTATION-EMPTY-PAYLOAD-INTEGRITY`
- Source fix: `53393ad99fa8a801e3da4fdaaf36bab8dbb255b6`
- Regression: `2ddda3274ed6780aed12f039408056bc22f80508`

## Confirmed defect

`SemanticDocumentationCatalogStore.Load()` combined an absent metadata key and `string.IsNullOrEmpty(payload)` into the same empty-catalog return path. This silently accepted a persisted metadata key whose value was the empty string even though an existing catalog payload is supposed to be structured, versioned XML. Presence with an empty payload was therefore corruption, not the representation of an empty catalog.

## Completed contract

- A missing `QS3D.Documentation.Catalog.v1` metadata key continues to represent an empty documentation catalog.
- A present key with `""` now fails closed with `InvalidDataException` instead of being treated as empty.
- Existing payload size, XML parsing/schema/version, planner validation, canonical serialization, save no-op and empty-save metadata-removal behavior remain unchanged.
- Added focused Core smoke coverage for both missing-key and present-empty-payload behavior, including no project mutation during `Load()`.
- Scope remained limited to documentation catalog empty-payload integrity.

## Validation boundary

Source and smoke were re-read from `main` after both commits and contained the intended changes. GitHub returned no combined status checks for regression commit `2ddda3274ed6780aed12f039408056bc22f80508`; no GitHub Actions, executable smoke PASS, or licensed BricsCAD runtime PASS is claimed.
