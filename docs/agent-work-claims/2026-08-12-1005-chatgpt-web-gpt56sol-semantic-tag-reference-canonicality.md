# Work claim — semantic tag reference canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-semantic-tag-reference-canonicality-20260812-1005`
- Registered: `2026-08-12T10:05:00+07:00`
- Baseline main SHA: `8238196ef8ee5fb1096c58061a5992e27ed0d38b`
- Priority: owner-requested continue-all Core integrity hardening

## Confirmed defect

`SemanticTagRenderContext.ResolveReference(...)` trimmed `ProjectElement.FamilyId`, `FloorId`, and `ZoneId` before lookup. Because those relation properties are mutable, a non-canonical padded reference could therefore resolve to a valid Family/Floor/Zone and render documentation as if the relation were healthy. This conflicted with Core fail-closed identity handling, where whitespace-padded relation/dependency IDs are treated as non-canonical rather than silently normalized at read time.

## Reserved scope

- Fail closed when a semantic tag relation reference contains leading or trailing whitespace.
- Preserve current case-insensitive canonical ID lookup and existing missing/ambiguous-reference behavior.
- Add focused smoke coverage for Family, Floor, and Zone padded references.
- Keep mutation semantics, CAD runtime, generated-tag health, and documentation template grammar unchanged.

## Expected surfaces

- `src/QS3D.Core/Documentation/SemanticTagRenderContext.cs`
- `tests/QS3D.Core.SmokeTests/SemanticTagRendererSmoke.cs`
- this claim file

## Excluded scope

- No Semantic Tag create/refresh/remove command changes.
- No BricsCAD V25/V26 runtime provider changes.
- No unrelated documentation, health, persistence, release, or active-claim changes.
- No GitHub Actions, force push, release publication, or BricsCAD runtime PASS claim.

## Validation and integration evidence

- Claim registration commit: `19f5cc41d3d84a337086189ad6a310beab6d404e`.
- Product fix commit: `310c238cfaf17d623028c4c540a3df0e37649016`.
- Regression commit: `ae8f98a96b400d908acfde2629274fafcc2a6eff`.
- Re-fetched `SemanticTagRenderContext.cs` at regression commit; blob `cc31f2170810db3fad1864ce7fc5d86c0cb67931` contains the ordinal raw-vs-trimmed canonicality guard before ambiguity/missing lookup.
- Re-fetched `SemanticTagRendererSmoke.cs` at regression commit; blob `e3edd3e78cc2628c81b439a97d9e1ac59a53ec3e` contains focused Family/Floor/Zone padded-reference fail-closed coverage.
- Verified `ae8f98a96b400d908acfde2629274fafcc2a6eff` is an ancestor of refreshed `main` SHA `77ff0259a0a85a9ced55518b9faff65e886b4cda` with no intervening changes to the two reserved files.
- GitHub Actions were not dispatched. The available execution environment has no `dotnet` runtime, so no local smoke execution is claimed; validation here is exact remote-content/ancestry verification only. No BricsCAD runtime PASS is claimed.

## Completion condition

Completed: semantic tag rendering no longer normalizes padded Family/Floor/Zone references into valid documentation output, existing canonical/ambiguous/missing behavior is preserved in the implementation path, focused regression coverage is committed, and the integration commits remain on current `main` ancestry.
