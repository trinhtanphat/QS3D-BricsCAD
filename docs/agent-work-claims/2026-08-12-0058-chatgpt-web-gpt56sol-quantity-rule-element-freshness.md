# Work claim — quantity-rule element freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-quantity-rule-element-freshness`
- Registered: `2026-08-12T00:58:00+07:00`
- Baseline main SHA: `d440c0d46499326c320aa907a24f00dec34256e1`
- Priority: deterministic Core mutation freshness defect found during owner-requested continue-all audit

## Confirmed defect

`QuantityRuleEngine` persists rule ownership in `ProjectElement.Properties` under `Rule:<output>` and removes stale managed quantities/provenance directly from the element dictionaries. Those writes/removals bypass `ProjectElement` freshness handling.

This is observable when a rule id/version changes but evaluates to the exact same quantity: `SetQuantity` is correctly a no-op for the unchanged value, then provenance changes directly while `ProjectElement.UpdatedUtc` remains unchanged. Likewise, applying a ruleset with no active rule but stale managed output removes persisted quantity/provenance without advancing element freshness.

Preview-level project mutation tracking (`68edffe2afd5bd363f85cc6eff659e104b1994aa`) updates `ProjectState.ChangeVersion` for reviewed apply, but it does not repair the element-level persisted timestamp and does not cover direct public `QuantityRuleEngine.Apply/ApplyMatching` callers.

## Reserved scope

- Add one assembly-internal `ProjectElement` freshness primitive that only advances `UpdatedUtc` and does not change `Dirty` or generated-stale state.
- Route quantity-rule provenance assignment and stale managed-output cleanup through small helpers that call that primitive only when direct dictionary mutation actually changes persisted element state.
- Preserve exact same-value/no-op behavior: unchanged quantity + unchanged provenance must remain timestamp-stable.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectElement.cs` (internal freshness primitive only)
- `src/QS3D.Core/Rules/QuantityRuleEngine.cs`
- `tests/QS3D.Core.SmokeTests/QuantityRuleElementFreshnessSmoke.cs`
- module-initializer registration in the new smoke file
- this claim file

## Excluded scope

- No ProjectState ChangeVersion, preview identity/freshness, formula evaluation, rule ordering/dependency, Dirty flags, generated geometry stale, persistence schema or UI/native changes.
- No use of `SetProperty` for `Rule:` metadata because provenance bookkeeping must not acquire general semantic-property dirty/stale side effects.
- No GitHub Actions dispatch or V25/V26 runtime work.

## Validation plan

- First direct rule apply writes quantity + provenance and advances element timestamp.
- Reapplying identical rule/result/provenance is timestamp-stable.
- Changing only rule version while result stays identical updates provenance and advances element timestamp without changing Dirty.
- Removing a stale managed output through `ApplyMatching` advances timestamp and removes both quantity + provenance while preserving Dirty.
- Calling `ApplyMatching` with no stale outputs/no active rules remains timestamp-stable.
- Exact implementation diffs are inspected and final source/test are read back from moving `main` before close-out.

## Coordination

Recent quantity-rule work on canonical provenance reads, reviewed preview apply Project ChangeVersion and rule-create UI freshness is complete and operates at different boundaries. The eleven commits that landed after the first claim attempt touch V26/signing/family activation/wall footprint/browser references and unrelated claims, not `ProjectElement` or `QuantityRuleEngine`. Recent commit search found no active claim for element-level quantity-rule provenance/cleanup freshness.

## Completion condition

Current `main` records element freshness for actual direct quantity-rule provenance/cleanup mutations, preserves no-op stability and existing dirty/stale semantics, includes focused deterministic smoke coverage, and this claim is closed `COMPLETED`.
