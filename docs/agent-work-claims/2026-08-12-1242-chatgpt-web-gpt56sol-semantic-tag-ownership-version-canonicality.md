# Work claim — Semantic Tag ownership-version canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-semantic-tag-ownership-version-canonicality`
- Registered: `2026-08-12T12:42:00+07:00`
- Completed: `2026-08-12T12:46:00+07:00`
- Baseline main SHA: `8c3b345daee431141b7b5df779f99a23841e3c40`
- Priority: P1 — writer-owned Semantic Tag ownership schema version must preserve its exact token.
- Task Key: `CORE-SEMANTIC-TAG-OWNERSHIP-VERSION-CANONICALITY`

## Confirmed defect

`SemanticTagBuilder.Build(...)` always persists `GeneratedSemanticTagOwnershipVersion` as the exact `GeneratedSemanticTagHealthService.OwnershipVersion` constant, currently `"1"`. `GeneratedSemanticTagHealthService` previously validated that field through the generic owner helper, which trimmed the stored value and compared case-insensitively. A persisted alias such as `" 1 "` therefore passed ownership-version health even though the writer never emits it.

## Completed implementation

- Claim commit: `a3722c8e4e288bc76cc5c3516f8772698bd3dec7`.
- Source commit: `8d1303857f43276e34c8663b56e8a1f1248eab96`.
- Smoke commit: `ef6d910e3375774e0b498da6c71025f4552a6a5d`.
- PR #895 squash merge: `40502704b402b1aa55300f7f187b4fabd355eb40`.
- Merged source blob read back from `main`: `c8bf984d445cb35349460a469da9373e292fb3ad`.
- Merged smoke blob read back from `main`: `f74f3b18c565f57706ce484abb4c3a2f482ced9b`.

## Final contract

- Missing/blank or semantically different ownership version continues to emit `SEMANTIC_TAG_OWNERSHIP_VERSION_INVALID` as Error.
- A stored version whose trimmed text equals `OwnershipVersion` but whose raw text is not exactly the writer-owned token emits `SEMANTIC_TAG_OWNERSHIP_VERSION_NON_CANONICAL` as Error.
- Exact writer-owned ownership version preserves existing behavior.
- Elements without generated Semantic Tag handles remain unaffected.
- Owner project/element ID comparison semantics remain unchanged.

No GitHub Actions were dispatched. No full local .NET build PASS, executable smoke PASS, or BricsCAD V25/V26 runtime PASS is claimed for this lane.
