# Work claim — Semantic Tag ownership-version canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-semantic-tag-ownership-version-canonicality`
- Registered: `2026-08-12T12:42:00+07:00`
- Baseline main SHA: `8c3b345daee431141b7b5df779f99a23841e3c40`
- Priority: P1 — writer-owned Semantic Tag ownership schema version must preserve its exact token.
- Task Key: `CORE-SEMANTIC-TAG-OWNERSHIP-VERSION-CANONICALITY`

## Confirmed defect

`SemanticTagBuilder.Build(...)` always persists `GeneratedSemanticTagOwnershipVersion` as the exact `GeneratedSemanticTagHealthService.OwnershipVersion` constant, currently `"1"`. `GeneratedSemanticTagHealthService` validates that field through the generic owner helper, which trims the stored value and compares case-insensitively. A persisted alias such as `" 1 "` therefore passes ownership-version health even though the writer never emits it.

## Non-overlap check

Recent commit search found no Semantic Tag ownership-version canonicality lane. Open PR #890 owns Project Browser workspace selection freshness and does not overlap Semantic Tag diagnostics. Owner project/element IDs are explicitly excluded because their semantic casing contract is distinct.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedSemanticTagHealthService.cs`
- one focused Core smoke regression for ownership-version canonicality
- this claim file

Do not modify owner project/element ID comparison, rotation/placement/template/text semantics, Semantic Tag builder/runtime health, generated handle ownership, persistence format, command wrappers, or BricsCAD runtime code.

## Intended contract

- Missing/blank or semantically different ownership version continues to emit `SEMANTIC_TAG_OWNERSHIP_VERSION_INVALID` as Error.
- A stored version whose trimmed text equals `OwnershipVersion` but whose raw text is not exactly the writer-owned token emits `SEMANTIC_TAG_OWNERSHIP_VERSION_NON_CANONICAL` as Error.
- Exact writer-owned ownership version preserves existing behavior.
- Elements without generated Semantic Tag handles remain unaffected.

## Completion condition

Padded ownership-version aliases are fail-visible without changing invalid/missing semantics, focused smoke coverage pins alias/invalid/canonical/no-handles controls, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
