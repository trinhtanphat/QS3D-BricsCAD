# Work claim — semantic tag reference canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-semantic-tag-reference-canonicality-20260812-1005`
- Registered: `2026-08-12T10:05:00+07:00`
- Baseline main SHA: `8238196ef8ee5fb1096c58061a5992e27ed0d38b`
- Priority: owner-requested continue-all Core integrity hardening

## Confirmed defect

`SemanticTagRenderContext.ResolveReference(...)` trims `ProjectElement.FamilyId`, `FloorId`, and `ZoneId` before lookup. Because those relation properties are mutable, a non-canonical padded reference can therefore resolve to a valid Family/Floor/Zone and render documentation as if the relation were healthy. This conflicts with Core fail-closed identity handling, where whitespace-padded relation/dependency IDs are treated as non-canonical rather than silently normalized at read time.

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

## Validation plan

- Re-fetch current `main` and both reserved source/test files after claim registration before editing.
- Reject non-canonical raw relation IDs before dictionary lookup; retain case-insensitive lookup for canonical IDs.
- Extend the existing `SemanticTagRendererSmoke` regression surface with padded Family/Floor/Zone relation cases.
- Re-fetch final source/test from current `main`, verify exact committed contents and claim ancestry, then mark this claim `COMPLETED` with exact integration evidence.

## Completion condition

Completed only when semantic tag rendering no longer normalizes padded Family/Floor/Zone references into valid documentation output, existing canonical/ambiguous/missing behavior remains intact, focused regression coverage is committed, and this claim is closed on `main` with exact commit evidence.
