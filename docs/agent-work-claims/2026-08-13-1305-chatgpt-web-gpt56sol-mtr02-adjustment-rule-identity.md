# Work claim — MTR-02A adjustment rule identity provenance

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-mtr02-adjustment-rule-identity-20260813-1305`
- Workstream: `MeasurementTrace / MTR-02 / P0` — deduction/addition rule identity sub-lane only
- Claimed UTC: `2026-08-13T06:05:00Z`
- Last updated UTC: `2026-08-13T06:05:00Z`
- Baseline main SHA: `76a1e760c78f1146fa528dcf11e906fecaa532e0`

## Confirmed gap

The current canonical `MeasurementTrace` already carries optional trace-level `RuleId` / `RuleVersion`, but `MeasurementTraceAdjustment` records only kind, amount, unit, reason and source identity. Therefore a deduction/addition can explain which source was adjusted without identifying which versioned rule/policy authorized that adjustment. This is the remaining deterministic deduction-identity part of MTR-02; it is distinct from profile-definition work and from rule evaluation itself.

Current-source readback on the baseline also shows canonical serialization is explicitly versioned as `MTR1`, so any new adjustment metadata must preserve byte-for-byte `MTR1` serialization for legacy/no-adjustment-rule traces rather than silently changing the existing canonical representation.

## Reserved files

- `src/QS3D.Core/Measurement/MeasurementTrace.cs`
- `tests/QS3D.Core.SmokeTests/MeasurementTraceContractSmoke.cs`
- this claim file

## Scope

- Extend `MeasurementTraceAdjustment` with an optional `ruleId` + `ruleVersion` pair using the existing canonical token validation semantics.
- Require the pair together; missing one side fails closed.
- Include adjustment rule identity in equality/hash only when present, preserving legacy hash behavior for adjustments with no rule metadata.
- Preserve existing `MTR1` canonical text exactly when all adjustments have no rule metadata.
- When any adjustment carries rule identity, emit a deterministic new canonical schema (`MTR2`) that serializes a rule-id/version pair for every adjustment after canonical adjustment sorting.
- Add deterministic smoke coverage for pair validation, equality/hash distinction and `MTR1` legacy versus `MTR2` rule-aware serialization.

## Explicit exclusions

- No Measurement Profile type/engine/schema in this lane; repository search/source inspection did not reveal a canonical profile source-of-truth, so profile design remains a separate owner decision/sub-lane rather than speculative invention.
- No changes to `QuantityRule`, `QuantityRuleEngine`, expression evaluation, output provenance properties, stale cleanup or rule dependency ordering. The currently ACTIVE quantity-rule variable-key collision claim reserves `QuantityRuleEngine.cs` and its own smoke only and explicitly excludes MTR files.
- No Takeoff/MTR-03, report/UI, persistence, BricsCAD adapter/native, LOCAL-003, Curtain, installer or CI/GitHub Actions changes.

## Initial overlap check

- Previous MTR nullable-integrity claim is `COMPLETED`; its implementation is current and these files are free for a new narrowly scoped claim.
- MTR-03 is completed and did not reserve current MTR contract files after its Takeoff projection lane.
- `chatgpt-web-gpt56sol-quantity-rule-variable-key-collision-20260813-1257` is `ACTIVE` only on `QuantityRuleEngine.cs` + `QuantityRuleVariableKeyCanonicalizationSmoke.cs` and explicitly excludes MeasurementTrace/MTR files.
- `LOCAL-003` is `ACTIVE` again on licensed native Level-Z qualification; it remains fully outside this pure Core provenance-contract scope.
- Curtain/native and palette UI work observed on current main are outside these two reserved files.

## Validation plan

- Re-fetch `main` after this claim-only commit and compare all intervening commits for reserved-file overlap before source work.
- Source diff must remain limited to the adjustment provenance contract/canonical encoding described above; no quantity calculation logic changes.
- Focused smoke must prove legacy `MTR1` output remains unchanged for no-rule adjustments and rule-aware traces use deterministic `MTR2` output.
- Re-fetch implementation diff and exact final files from current `main` after push.
- No GitHub Actions. This container has no `dotnet`, `csc`, `mcs` or `msbuild`; unless that changes, no executable .NET/native PASS will be claimed.

## Completion

Pending implementation.
