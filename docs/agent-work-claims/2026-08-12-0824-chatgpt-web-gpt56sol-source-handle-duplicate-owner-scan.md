# Work claim — Duplicate stored SourceHandle ownership scan

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-source-handle-duplicate-owner-scan`
- Registered: `2026-08-12T08:24:00+07:00`
- Baseline main SHA: `4e74d36cbfcec75998cbca55f14fc6a858aea7b1`
- Priority: P1 — fail closed on in-memory source-ownership state already rejected by QSDB persistence.

## Confirmed defect

QSDB writer preflight rejects exact and case-only duplicate `ProjectElement.SourceHandles` because source identity is case-insensitive. `SemanticHandleOwnershipResolver` already rejects blank and padded stored source handles, but it does not reject duplicate stored handles within one semantic element. `ResolveUniqueSourceOwner(...)` simply observes the same owner repeatedly, `Resolve(...)` treats repeated claims by the same element as a no-op, and `ResolveCaptureTarget(...)` can therefore continue on a project state that cannot be canonically persisted.

This lane aligns ownership/capture resolution with the existing persistence identity contract; it does not introduce a new source-handle policy.

## Reserved scope

- `src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs`
- `tests/QS3D.Core.SmokeTests/SemanticHandleOwnershipDuplicateSourceSmoke.cs`
- `tests/QS3D.Core.SmokeTests/SemanticHandleOwnershipDuplicateSourceRegistration.cs`
- this claim file

## Intended contract

- Every project-owned element scanned for source ownership must have case-insensitively unique canonical `SourceHandles`.
- Exact or case-only duplicates fail closed before owner/capture/selection resolution returns a result.
- User-supplied source-handle query/selection normalization and de-duplication remain unchanged.
- Cross-element ownership ambiguity behavior remains unchanged.

## Excluded scope

- No changes to QSDB reader/writer/schema, `SourceHandleResolver`, generated-handle ownership, Source Reconcile, native BricsCAD selection/mutation, or automatic repair.
- No GitHub Actions dispatch and no V25/V26 runtime qualification claim.

## Validation plan

- Re-fetch `SemanticHandleOwnershipResolver.cs` after claim publication and write against the exact current blob SHA.
- Add focused auto-registered Core smoke source covering exact/case-only duplicate stored handles across unique-owner, capture-target and selected-handle resolution while preserving canonical success.
- Review exact pushed diffs and read back final source/test from current `main`.
- Close this claim with exact commit SHAs and verify claim/fix/test/close ancestry on current `main` without force-push.
- No compile/test-runtime PASS will be claimed unless actually executed.

## Coordination

Recent source-handle canonical-owner, Locate canonicality/root-bound, QSDB duplicate-list and Locate missing-dependency lanes are completed. Current active/recent lanes observed before registration cover Auto Room previous Family, Slab/Wall null fixtures, Start Center, Template XML, measured quantities, Release/BLT/reporting/health and other non-overlapping surfaces. This claim does not reopen those scopes.

## Completion condition

All semantic source-ownership resolver entry points fail closed on duplicate project-owned `SourceHandles`, canonical behavior is preserved, focused regression source is committed on `main`, and this claim is marked `COMPLETED` with truthful validation notes.
