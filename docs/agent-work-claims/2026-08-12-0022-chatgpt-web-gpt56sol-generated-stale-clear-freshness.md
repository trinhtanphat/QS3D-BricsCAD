# Work claim — generated stale clear freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-generated-stale-clear-freshness`
- Registered: `2026-08-12T00:22:00+07:00`
- Baseline main SHA: `61d4cf191cb41f6479104cc9c3404f75d3e2ec9f`
- Priority: deterministic CAD-independent persistence freshness defect found during owner-requested continue-all audit

## Confirmed defect

Generated stale inspection was intentionally made query-pure by `dba801b10c492376370886f304ccd873260f5e27`; stale marker cleanup is explicit through `ClearGenerated*Stale()` / `ClearGeneratedGeometryStale()`. Those explicit clear APIs remove persisted `ProjectElement.Properties` stale state/snapshot/aggregate metadata but currently never advance `UpdatedUtc`. A real persisted semantic-state mutation can therefore leave element freshness unchanged. Repeating an already-empty clear should remain a no-op.

## Reserved scope

- Update element freshness only when an explicit generated-stale clear actually removes one or more persisted stale metadata entries.
- Preserve query purity: `IsGenerated*Stale()` and health inspection remain read-only.
- Preserve generated handle/build metadata; only existing stale state/snapshot/aggregate keys are cleared as today.
- Preserve `Dirty` exactly.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectElement.cs` (explicit stale-clear helpers only)
- `tests/QS3D.Core.SmokeTests/ProjectElementGeneratedStaleClearFreshnessSmoke.cs`
- module-initializer registration in that new smoke file
- this claim file

## Excluded scope

- No generated ownership/handle replacement, rebuild, Health, stale detection or mark-stale algorithm changes.
- No `SetProperty`, `SetQuantity`, `MarkDirty`, `MarkClean`, Category or ProjectState ChangeVersion changes.
- No V25/native/runtime/UI work and no GitHub Actions dispatch.

## Validation plan

- Clearing one real stale output removes only its marker/snapshot, preserves other stale kinds, and advances `UpdatedUtc`.
- Clearing the final stale kind also removes aggregate stale metadata and advances `UpdatedUtc`.
- Repeating a per-kind clear after markers are gone preserves `UpdatedUtc`.
- `ClearGeneratedGeometryStale()` advances timestamp only when it actually removes stale metadata and remains timestamp-stable when there is nothing to clear.
- Generated handle/build-state properties survive explicit stale clear.
- `Dirty` remains unchanged across all clear operations.
- Existing query-purity behavior remains untouched.

## Coordination

Recent generated-stale work is completed and specifically separates query inspection from explicit cleanup. Recent commit search found no active claim for explicit stale-clear freshness. Current concurrent claims are on unrelated Grid/diagnostics/V25/revision/enumeration surfaces.

## Completion condition

Current `main` records freshness for actual explicit stale-metadata cleanup without introducing no-op churn or query-side mutation, focused deterministic regression coverage is present, and this claim is closed `COMPLETED` with exact commits and validation actually performed.
