# Work claim — Semantic documentation persisted empty-payload integrity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-semantic-documentation-empty-payload-20260812-1529`
- Registered: `2026-08-12T15:29:00+07:00`
- Baseline main SHA: `9a3f6b5fee70d79233c01237d8e8cb783a1f52a1`
- Priority: P1 semantic documentation persistence fail-closed integrity
- Task Key: `CORE-SEMANTIC-DOCUMENTATION-EMPTY-PAYLOAD-INTEGRITY`

## Confirmed defect

`SemanticDocumentationCatalogStore.Load()` currently combines an absent metadata key and `string.IsNullOrEmpty(payload)` into the same empty-catalog return path. This silently accepts a persisted metadata key whose value is the empty string even though an existing catalog payload is supposed to be structured, versioned XML. Presence with an empty payload is therefore corruption, not the representation of an empty catalog.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticDocumentationCatalogStore.cs`
- focused Core semantic documentation regression coverage
- this claim file

## Intended contract

- A missing `QS3D.Documentation.Catalog.v1` metadata key continues to represent an empty documentation catalog.
- A present key with `""` fails closed with the store's persistence-data failure semantics instead of being treated as empty.
- Preserve existing payload size, XML parsing/schema/version, planner validation, canonical serialization, save no-op and empty-save metadata-removal behavior.
- Do not broaden into unrelated documentation identity or persistence normalization.

## Validation boundary

Focused Core regression/source readback and available repository checks only. No GitHub Actions, executable smoke PASS, or licensed BricsCAD runtime PASS will be claimed unless actually observed.
