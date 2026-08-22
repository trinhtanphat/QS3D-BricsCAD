# Worker-3 claim: documentation catalog load bounds

- Issue: #2151
- Agent: `worker-3`
- Baseline: `main@5c998d065667cb66f0142b2a51eecd2fcdbc8696`
- Branch: `agent/worker-3/documentation-catalog-load-bounds-2151-r2`
- Scope: enforce the existing 10,000-view and 10,000-sheet limits when loading persisted semantic documentation catalog metadata; add deterministic exact-bound and over-bound smoke coverage below the 1 MiB metadata cap.
- Expected implementation paths: `src/QS3D.Core/Documentation/SemanticDocumentationCatalogStore.cs`, `tests/QS3D.Core.SmokeTests/SemanticDocumentationCatalogStoreSmoke.cs`.
- Main remains read-only; stop before merge.
