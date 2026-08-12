# Work claim — Model Health generated live numeric handle identity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-model-health-generated-live-identity`
- Registered: `2026-08-12T13:41:00+07:00`
- Baseline main SHA: `35393f4e939c856b853aa4cc6c934215fb762f7c`
- Priority: P0 — generated-solid live membership must use the same numeric CAD Handle identity as generated providers.
- Task Key: `CORE-MODEL-HEALTH-GENERATED-LIVE-HANDLE-IDENTITY`

## Confirmed defect

`ComprehensiveModelHealthService.NormalizeHandleSet(...)` trims/case-folds `liveGeneratedSolidHandles` but does not canonicalize numeric CAD Handle aliases. `ModelHealthService.ValidateGeneratedGeometry(...)` likewise keys a valid `GeneratedSolidHandle` by trimmed raw text for duplicate/live membership. Thus persisted `A` and caller live handle `0A` can represent the same CAD object while Model Health reports `GENERATED_SOLID_MISSING`; numeric aliases can also evade the local generated-solid duplicate owner check.

The provider-specific generated health services now use the shared numeric identity policy, so this boundary mismatch is isolated to the generic generated-solid / comprehensive live-set path.

## Reserved scope

- `src/QS3D.Core/Diagnostics/ModelHealthService.cs`
- `src/QS3D.Core/Diagnostics/ComprehensiveModelHealthService.cs`
- one focused Core smoke regression
- this claim file

Do not change `liveSourceHandles`, semantic SourceHandle duplicate/orphan behavior, malformed generated-handle validity, persistence spelling, builders, or BricsCAD runtime code.

## Intended contract

- Once `GeneratedSolidHandle` passes the existing hexadecimal validity rule, use `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)` for local generated ownership and generated live membership.
- Comprehensive health normalizes only `liveGeneratedSolidHandles` through numeric identity; source live sets retain their existing semantics.
- `A`, `0A`, and equivalent leading-zero/case aliases are one generated CAD object.
- Existing invalid/whitespace diagnostics and truly missing generated-solid behavior remain unchanged.

## Completion condition

Focused regression proves numeric-equivalent live handles do not report missing, generated aliases collide locally, truly missing handles still report, and source-live normalization behavior is unchanged; merged source + smoke are read back from current `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
