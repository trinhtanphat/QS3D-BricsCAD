# Work claim — Duplicate stored SourceHandle ownership scan

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-source-handle-duplicate-owner-scan`
- Registered: `2026-08-12T08:24:00+07:00`
- Completed: `2026-08-12T08:30:00+07:00`
- Baseline main SHA: `4e74d36cbfcec75998cbca55f14fc6a858aea7b1`
- Claim commit: `69d6acc86d887cd5c3b3199387b7922666927ca2`
- Implementation/regression commit: `0a569381a849fc8bd13138b533e7cebd6b35957a`
- Priority: P1 — fail closed on in-memory source-ownership state already rejected by QSDB persistence.

## Confirmed defect

QSDB writer preflight rejects exact and case-only duplicate `ProjectElement.SourceHandles` because source identity is case-insensitive. `SemanticHandleOwnershipResolver` already rejected blank and padded stored source handles, but it did not reject duplicate stored handles within one semantic element. `ResolveUniqueSourceOwner(...)` simply observed the same owner repeatedly, `Resolve(...)` treated repeated claims by the same element as a no-op, and `ResolveCaptureTarget(...)` could therefore continue on a project state that cannot be canonically persisted.

This lane aligns ownership/capture resolution with the existing persistence identity contract; it does not introduce a new source-handle policy.

## Completed scope

- `src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs`
- `tests/QS3D.Core.SmokeTests/SemanticHandleOwnershipDuplicateSourceSmoke.cs`
- `tests/QS3D.Core.SmokeTests/SemanticHandleOwnershipDuplicateSourceRegistration.cs`
- this claim file

## Resulting contract

- Every project-owned element scanned for source ownership must have case-insensitively unique canonical `SourceHandles`.
- Exact or case-only duplicates fail closed before owner/capture/selection resolution returns a result.
- User-supplied source-handle query/selection normalization and de-duplication remain unchanged.
- Cross-element ownership ambiguity behavior remains unchanged.

## Implementation

`SemanticHandleOwnershipResolver` now materializes each element's stored source handles through one `GetCanonicalUniqueStoredSourceHandles(...)` preflight. The helper reuses existing blank/padded canonical validation and adds a case-insensitive uniqueness check before ownership resolution consumes the list. `ResolveUniqueSourceOwner(...)` and `Resolve(...)` use this validated list; `ResolveCaptureTarget(...)` inherits the same guard through its unique-owner lookup.

The focused smoke covers exact duplicates, case-only duplicates, all three ownership entry points, canonical unique-handle behavior, unchanged duplicate user-selection normalization and failure-side project `ChangeVersion` stability. Registration uses a dedicated module initializer.

## Validation actually performed

- Re-fetched the claimed source after claim publication and confirmed its blob remained `f1f386cdc131673d84f9f63cbf9f955aefa77007` before the final write.
- Prepared source plus both regression files as one coherent tree and repeatedly refreshed moving `main` before integration.
- Two stale-parent fast-forward attempts were rejected safely by GitHub; no force update was used. Intervening commit comparisons showed no overlap with the three reserved paths before rebuilding the reviewed tree on the latest head.
- Successfully fast-forwarded `main` with implementation/regression commit `0a569381a849fc8bd13138b533e7cebd6b35957a`.
- Reviewed the exact pushed commit diff; it contains only the intended source-handle uniqueness preflight plus the two new regression files.
- Read back current `main` source, smoke and module registration and confirmed the expected blobs are present.
- GitHub Actions were not dispatched.
- No local .NET SDK/Core smoke execution or licensed BricsCAD V25/V26 runtime PASS is claimed from this remote session.

## Excluded scope honored

- No changes to QSDB reader/writer/schema, `SourceHandleResolver`, generated-handle ownership, Source Reconcile, native BricsCAD selection/mutation, or automatic repair.

## Coordination

Recent source-handle canonical-owner, Locate canonicality/root-bound, QSDB duplicate-list and Locate missing-dependency lanes remain separate and completed. Concurrent work during this lane touched Floor, measured quantities, health, Browser, physical opening, Template and other unrelated surfaces; those changes were preserved.

## Completion

Semantic source-ownership resolution now fails closed on duplicate project-owned `SourceHandles` using the same case-insensitive identity already enforced by persistence, while canonical ownership/query behavior remains intact. The claim is released as completed.
