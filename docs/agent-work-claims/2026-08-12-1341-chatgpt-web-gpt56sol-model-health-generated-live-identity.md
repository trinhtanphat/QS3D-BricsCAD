# Work claim — Model Health generated live numeric handle identity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-model-health-generated-live-identity`
- Registered: `2026-08-12T13:41:00+07:00`
- Baseline main SHA: `35393f4e939c856b853aa4cc6c934215fb762f7c`
- Priority: P0 — generated-solid live membership must use the same numeric CAD Handle identity as generated providers.
- Task Key: `CORE-MODEL-HEALTH-GENERATED-LIVE-HANDLE-IDENTITY`

## Confirmed defect

`ComprehensiveModelHealthService.NormalizeHandleSet(...)` trims/case-folds `liveGeneratedSolidHandles` but does not apply numeric CAD Handle identity. Downstream generated providers now compare canonical numeric identities, while generic `ModelHealthService` still calls `Contains(rawGeneratedHandle)`. A live alias such as `0A` can therefore represent the same CAD object as persisted `A` yet produce a false `GENERATED_SOLID_MISSING` depending on spelling direction.

Global generated ownership diagnostics already cover duplicate owner aliases, so the lane is intentionally refined to the live-set boundary only.

## Reserved scope

- `src/QS3D.Core/Diagnostics/ComprehensiveModelHealthService.cs`
- one focused Core smoke regression
- this claim file

Do not modify `ModelHealthService`, `liveSourceHandles`, semantic SourceHandle duplicate/orphan behavior, generated-handle validity/persistence, builders, or runtime code.

## Intended contract

- `liveGeneratedSolidHandles` is represented by a set whose equality/hash semantics use `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)`.
- This makes `Contains("A")`, `Contains("0A")`, and equivalent case/leading-zero aliases identify the same live CAD object even for consumers that pass raw persisted text to `Contains`.
- `liveSourceHandles` keeps its existing trim/case-only semantics.
- Invalid generated-live text retains shared normalizer fallback semantics; truly missing generated handles remain missing.

## Completion condition

Focused regression proves numeric-equivalent generated live handles do not report missing in both spelling directions, truly missing handles still report, and source-live behavior is unchanged; merged source + smoke are read back from current `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
