# Agent Work Claim — ProjectElement dirty-none no-op

Task: Preserve ProjectElement persistence timestamp on no-op dirty-state commands
Agent: ChatGPT / GPT-5.6 Sol
Scope:
- `src/QS3D.Core/Domain/ProjectElement.cs`
- `tests/QS3D.Core.SmokeTests/DomainMutationAtomicitySmoke.cs`
- `docs/PROJECTELEMENT-DIRTY-NOOP-PLAN-2026-08-11.md`
- this claim file
Declaration: Source-verifiable QS3D.Core domain bugfix only. No BricsCAD runtime qualification, GitHub Actions dispatch, updater changes, or unrelated product-lane edits.
Status: `COMPLETED`
Created: `2026-08-11T22:58:00+07:00`
Updated: `2026-08-11T23:04:15+07:00`
Branch-Revision: `8d2d27c2cdc37811a1cc3fd41444446bf933f648`
Overlap-Count: `0`
Overlap-Claim-IDs: `[]`

## Verified defect

`ProjectElement.MarkDirty(ElementDirtyFlags.None)` reached `MarkDirtyCore`, performed no effective dirty-state transition, but still overwrote `UpdatedUtc`. `ProjectElement.MarkClean(ElementDirtyFlags.None)` likewise left `Dirty` unchanged but overwrote `UpdatedUtc`. This violated the no-op mutation invariant already used elsewhere in the domain model and could make persistence/revision consumers observe a semantic change that never happened.

## Implemented fix

- `MarkClean` now preserves invalid-bit validation and returns immediately for `ElementDirtyFlags.None` before changing `Dirty` or `UpdatedUtc`.
- `MarkDirtyCore` now preserves invalid-bit validation and returns immediately for `ElementDirtyFlags.None` before stale propagation, dirty-bit mutation, or timestamp mutation.
- Existing behavior for every non-empty valid flag combination is structurally unchanged.
- `DomainMutationAtomicitySmoke` now covers `MarkDirty(None)`, `MarkClean(None)`, clean-state preservation, timestamp preservation, and the pre-existing undefined-bit exception contract.

## Validation

- Planning record commit preceded source implementation.
- Source commit: `0581b5db3a0e185b6855d1dbfce58282439c74e6` (`fix(core): keep dirty-none mutations idempotent`).
- Regression commit: `be5f69bb70731ae5af1f0e5b31ef402a39e071a3` (`test(core): cover dirty-none mutation atomicity`).
- GitHub compare against later `main` confirmed the regression commit is the merge base with `behind_by: 0`; subsequent commits touched unrelated updater/revision lanes.
- Re-fetched scoped files from later `main` and verified both no-op guards and regression coverage remain present.
- The available execution environment has no `dotnet`, `csc`, `mcs`, or `msbuild`, so the smoke executable was not run here. No GitHub Actions were dispatched, per repository policy.
- No BricsCAD V25 runtime qualification is claimed or required for this pure core-domain invariant.

## Release conditions

1. Planning record committed before source implementation — `PASS`.
2. Both `MarkDirty(None)` and `MarkClean(None)` preserve `Dirty` and `UpdatedUtc` by source contract and regression coverage — `PASS (source/regression present; executable runner unavailable locally)`.
3. Existing non-`None` behavior structurally unchanged — `PASS`.
4. Changes landed on `main` without force update — `PASS`.
5. Claim closed only after source/test were re-fetched from later `main` — `PASS`.
