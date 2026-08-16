# Work claim — Semantic Documentation persisted count bounds

- Owner/session: `chatgpt-gpt56sol`
- Issue: #2151
- Baseline: `main@d68cc5161b216782d702fc5615e6dc93b9ca3da0`
- Branch: `agent/chatgpt-gpt56sol/documentation-load-count-bounds-2151`

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticDocumentationCatalogStore.cs`
- focused deterministic Core smoke coverage for persisted view/sheet cardinality bounds
- focused auto-discovered preflight for the load-side count contract

## Defect / intent

`Save` already rejects more than 10,000 views or sheets, but `Load` currently materializes every persisted `<view>` / `<sheet>` element before planner validation. Persisted metadata can therefore exceed the documented catalog cardinality while remaining under the 1 MiB XML cap. The load boundary must reject element 10,001 deterministically instead of accepting state that `Save` cannot produce.

## Acceptance

- `ReadViews` rejects before materializing persisted view 10,001 with `InvalidDataException`.
- `ReadSheets` rejects before materializing persisted sheet 10,001 with `InvalidDataException`.
- exactly 10,000 persisted views / sheets remain accepted when otherwise valid and under the XML character cap.
- over-bound fixtures stay below the 1 MiB metadata cap so regression proves the count guard rather than the document-size guard.
- no schema/native/UI/runtime changes; no licensed BricsCAD PASS claim.

## Integration boundary

Commit/push only on the task branch. Reconcile latest `main` non-force before PR. Open PR only after automatic exact-head branch CI succeeds; merge only after exact-head PR CI succeeds and current-main freshness is clean.
