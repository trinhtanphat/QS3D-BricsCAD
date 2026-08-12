# Work claim — Model Health generated live numeric handle identity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-model-health-generated-live-identity`
- Registered: `2026-08-12T13:41:00+07:00`
- Completed: `2026-08-12T14:52:00+07:00`
- Baseline main SHA: `35393f4e939c856b853aa4cc6c934215fb762f7c`
- Priority: P0 — generated-solid live membership must use the same numeric CAD Handle identity as generated providers.
- Task Key: `CORE-MODEL-HEALTH-GENERATED-LIVE-HANDLE-IDENTITY`

## Confirmed defect

`ComprehensiveModelHealthService.NormalizeHandleSet(...)` trimmed/case-folded `liveGeneratedSolidHandles` but did not apply numeric CAD Handle identity. Downstream generated providers compare canonical numeric identities, while generic `ModelHealthService` calls `Contains(rawGeneratedHandle)`. A live alias such as `0A` could therefore represent the same CAD object as persisted `A` yet produce a false `GENERATED_SOLID_MISSING` depending on spelling direction.

Global generated ownership diagnostics already cover duplicate owner aliases, so the lane remained intentionally scoped to the live-set boundary only.

## Implemented contract

`ComprehensiveModelHealthService` now uses a dedicated generated-handle set comparer whose equality and hash code both flow through `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)`. Only `liveGeneratedSolidHandles` uses that comparer; `liveSourceHandles` remains trim/case-only. This makes `A`, `0A`, `00A`, and case aliases resolve as one generated CAD Handle identity for downstream `ISet.Contains(...)` consumers without changing persisted spelling or malformed-handle diagnostics.

## Regression coverage

The auto-registered `tests/QS3D.Core.SmokeTests/ComprehensiveGeneratedLiveHandleIdentitySmoke.cs` covers:

- live `00A` satisfying persisted generated handle `A`;
- live `A` satisfying persisted generated handle `00A`;
- a genuinely different generated handle remaining `GENERATED_SOLID_MISSING`;
- source-live semantics remaining textual (`0A` source vs `A` live still reports `ORPHAN_HANDLE`).

## Landing evidence

- Claim: `cbeec56a08be2962df8ff27d9d46bde55db8e73a`
- Scope refinement: `0d84bc1e9b3bb80386efcc7d72a37b76d1a8a47c`
- PR: `#937`
- PR head: `fb74e79b263c2f31f82951220dfa977fb3f9dc92`
- Squash merge to `main`: `d7f853da993fd50e5b40e5e8b3562b1a0068fb5e`
- Source blob read back from `main`: `71bee58be7b3577c5550099db6c333435a04859a`
- Smoke blob read back from `main`: `573db84a5a5e2399be2e8609d799e0ddd058c5de`

## Validation boundary

PR #937 is merged and closed, and both source and focused smoke were read back from current `main`. No GitHub Actions, full local .NET build, executable smoke process, or BricsCAD V25/V26 runtime was executed for this lane, so no such PASS is claimed.
