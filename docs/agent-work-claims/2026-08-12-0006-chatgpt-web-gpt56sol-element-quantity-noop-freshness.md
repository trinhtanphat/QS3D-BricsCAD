# Work claim — ProjectElement quantity no-op freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-element-quantity-noop-freshness`
- Registered: `2026-08-12T00:06:00+07:00`
- Baseline main SHA: `9f4f28d5ed79d3b898c70078eeaeeb345b4fd9ea`
- Priority: concrete CAD-independent semantic freshness defect found during owner-requested continue-all audit

## Confirmed defect

`ProjectElement.SetQuantity(name, value)` validates finite values but always writes the dictionary entry and advances `UpdatedUtc`, even when the same canonical quantity key already contains the exact same finite value. `SetProperty` already treats an exact same-value assignment as a no-op. Repeating an unchanged derived quantity therefore creates a false element-freshness change without changing semantic data, dirty flags, generated ownership, or project ChangeVersion.

## Reserved scope

Make exact same-value quantity assignment idempotent: after canonicalizing the key and validating the finite value, return without mutation when the stored value is equal. A genuinely changed value must retain the current write + `UpdatedUtc` behavior.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectElement.cs` (`SetQuantity` only)
- `tests/QS3D.Core.SmokeTests/ProjectElementQuantityNoOpSmoke.cs`
- module-initializer registration in that new smoke file
- this claim file

## Excluded scope

- No `ProjectElement.Category`, `MarkDirty`, `MarkClean`, generated stale, relation or property mutation policy changes.
- No quantity tolerance/rounding policy; equality is the stored `double` value contract, not approximate engineering equality.
- No regeneration algorithm, rule-engine, ProjectState ChangeVersion, persistence schema, V25/native or UI changes.
- No GitHub Actions dispatch.

## Validation plan

- Setting a new finite quantity writes the value and advances element `UpdatedUtc`.
- Setting the same canonical key to the same finite value leaves `UpdatedUtc` unchanged.
- Case-insensitive key aliases remain the same dictionary identity and same-value alias writes are no-ops.
- A changed finite value still updates the stored value and timestamp.
- NaN/Infinity rejection remains fail-closed and non-mutating.
- Re-fetch current target after claim publication, inspect exact source diff, and read back final source/test from `main`; never force-push.

## Coordination

Recent active claims observed on Grid naming bounds, quantity diagnostic export atomicity and unrelated V25/UI/source-enumeration surfaces. Search of recent commits found no active/recent claim for `ProjectElement.SetQuantity` same-value timestamp semantics. The earlier element-category hardening explicitly deferred category-change side-effect policy; this lane does not touch it.

## Completion condition

Current `main` makes exact same-value quantity assignments side-effect free while preserving all changed-value/invalid-value behavior, focused deterministic smoke coverage is present, and this claim is closed `COMPLETED` with exact commits and validation actually performed.
