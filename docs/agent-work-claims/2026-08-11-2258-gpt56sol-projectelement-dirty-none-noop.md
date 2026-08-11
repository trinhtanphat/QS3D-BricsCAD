# Agent Work Claim — ProjectElement dirty-none no-op

Task: Preserve ProjectElement persistence timestamp on no-op dirty-state commands
Agent: ChatGPT / GPT-5.6 Sol
Scope:
- `src/QS3D.Core/Domain/ProjectElement.cs`
- `tests/QS3D.Core.SmokeTests/DomainMutationAtomicitySmoke.cs`
- `docs/PROJECTELEMENT-DIRTY-NOOP-PLAN-2026-08-11.md`
- this claim file
Declaration: Source-verifiable QS3D.Core domain bugfix only. No BricsCAD runtime qualification, GitHub Actions dispatch, updater changes, or unrelated product-lane edits.
Status: `ACTIVE`
Created: `2026-08-11T22:58:00+07:00`
Updated: `2026-08-11T22:58:00+07:00`
Branch-Revision: `de9c9d15ed6c203a91edb8f61007b543dccfd25a`
Overlap-Count: `0`
Overlap-Claim-IDs: `[]`

## Verified defect

`ProjectElement.MarkDirty(ElementDirtyFlags.None)` reaches `MarkDirtyCore`, performs no effective dirty-state transition, but still overwrites `UpdatedUtc`. `ProjectElement.MarkClean(ElementDirtyFlags.None)` likewise leaves `Dirty` unchanged but overwrites `UpdatedUtc`. This violates the no-op mutation invariant already used elsewhere in the domain model and can make persistence/revision consumers observe a semantic change that never happened.

## Preservation contract

- Keep existing validation for undefined dirty-flag bits.
- Keep all non-`None` dirty/stale propagation behavior unchanged.
- Do not alter generated-output stale semantics.
- Add regression coverage to the existing domain mutation smoke suite.
- Do not dispatch GitHub Actions; repository policy keeps CI manual-only unless explicitly requested.

## Release conditions

1. Planning record is committed before source implementation.
2. Both `MarkDirty(None)` and `MarkClean(None)` are proven not to change `Dirty` or `UpdatedUtc`.
3. Existing non-`None` behavior remains structurally unchanged.
4. Final commit is based on current `main` without force update.
5. Claim is marked `COMPLETED` only after final source/test files are present on `main`.
