# ProjectElement Dirty-None No-Op Plan — 2026-08-11

## Objective

Eliminate false persistence mutation when callers pass `ElementDirtyFlags.None` to `ProjectElement.MarkDirty` or `ProjectElement.MarkClean`, while preserving every existing non-empty dirty/stale transition.

## Evidence

Current `ProjectElement` validates flag ranges but does not treat `None` as an idempotent command. `MarkDirtyCore` always assigns `UpdatedUtc = DateTime.UtcNow`; `MarkClean` also always assigns `UpdatedUtc = DateTime.UtcNow`. Therefore a command that changes no dirty bit still looks like a changed element to timestamp-based persistence/revision consumers.

The existing smoke suite already centralizes failed/no-op mutation invariants in `DomainMutationAtomicitySmoke`, so regression coverage belongs there rather than in a new test framework or BricsCAD-host test.

## Scope

Implementation scope is intentionally narrow:

1. `src/QS3D.Core/Domain/ProjectElement.cs`
   - retain invalid-bit validation;
   - return without mutation when validated flags are exactly `ElementDirtyFlags.None`;
   - preserve existing behavior for every non-empty flag combination.
2. `tests/QS3D.Core.SmokeTests/DomainMutationAtomicitySmoke.cs`
   - add a regression proving both dirty and clean `None` calls preserve dirty flags and `UpdatedUtc`;
   - also prove the fixture is meaningful by starting from a controlled dirty state.
3. Work claim and this plan only.

## Non-goals

- No change to quantity semantics, generated geometry stale metadata, BricsCAD host integration, UI, updater/install/uninstall, release packaging, or CI workflows.
- No claim of BricsCAD V25 runtime qualification.
- No GitHub Actions dispatch.
- No broad refactor of timestamp/revision architecture.

## Implementation sequence

1. Re-fetch current `main` and both scoped files after the planning commit.
2. Confirm no concurrent commit changed either scoped file.
3. Add the smallest source guards after existing flag validation so invalid flags still throw exactly as before.
4. Extend `DomainMutationAtomicitySmoke.Run()` with a dedicated no-op dirty-state regression.
5. Prefer deterministic timestamp setup available to the smoke assembly; otherwise use a bounded delay only if the model exposes no deterministic persistence-state fixture.
6. Perform available local/static validation without GitHub Actions.
7. Re-fetch current `main`; if it moved, preserve unrelated commits and update with a fast-forward-only atomic commit.
8. Fetch the resulting commit/files from GitHub and verify the exact patch.
9. Mark the work claim `COMPLETED` with validation limits recorded.

## Acceptance criteria

- `MarkDirty(ElementDirtyFlags.None)` does not change `Dirty`.
- `MarkDirty(ElementDirtyFlags.None)` does not change `UpdatedUtc`.
- `MarkClean(ElementDirtyFlags.None)` does not change `Dirty`.
- `MarkClean(ElementDirtyFlags.None)` does not change `UpdatedUtc`.
- Undefined flag bits still throw `ArgumentOutOfRangeException`.
- All pre-existing non-empty dirty/stale code paths are untouched except for control flow around the `None` case.
- Source/test changes land on `main` without force-push and without Actions execution.

## Validation boundary

This is a pure `QS3D.Core` domain invariant and can be source/smoke validated without a BricsCAD host. Repository-level BricsCAD V25 compile/NETLOAD/UI/rollback qualification remains separately runtime-gated and must not be inferred from this change.
