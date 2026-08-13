# Work claim — Quantity Rule leading-whitespace provenance canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-leading-provenance-whitespace-20260813-1307`
- Registered: `2026-08-13T13:07:00+07:00`
- Baseline main SHA: `1ebfd929e05db6c441b0a62fbc8632fcfcfe1f69`
- Priority: `P0 Measurement Rules` — canonical/fail-closed persisted rule provenance

## Confirmed gap

The existing quantity-rule provenance read path rejects malformed keys such as `Rule: Ghost`, but `GetStaleManagedOutputs(...)` filters properties with `StartsWith("Rule:")` before canonicality validation. A persisted/directly injected key such as ` Rule:Ghost` therefore bypasses the rule-provenance namespace entirely. If `Ghost` is an active rule output, the engine can then write canonical `Rule:Ghost` while leaving the padded shadow key in place; if it is stale, cleanup never sees it. The existing `QuantityRuleProvenanceCanonicalReadSmoke` covers whitespace after the prefix and blank outputs, but not whitespace before the `Rule:` namespace.

## Reserved scope

- `src/QS3D.Core/Rules/QuantityRuleEngine.cs` — recognize a trimmed key that enters the `Rule:` namespace, then reject any whole-key whitespace non-canonicality before output/provenance mutation.
- `tests/QS3D.Core.SmokeTests/QuantityRuleProvenanceCanonicalReadSmoke.cs` — add a focused leading-whitespace provenance regression while preserving existing canonical stale-cleanup behavior.
- this claim file.

## Contract boundary

- Canonical `Rule:<OutputName>` behavior is unchanged.
- Existing malformed `Rule: Ghost`, blank output, stale cleanup, dependency ordering, variable projection and provenance write semantics remain authoritative.
- Only properties whose trimmed key enters the `Rule:` namespace are subject to this new whole-key canonicality guard; unrelated padded non-rule properties remain outside this lane.

## Excluded scope

- No MeasurementTrace/MTR files; the concurrent MTR-02 adjustment-rule-identity claim owns only `MeasurementTrace.cs` + `MeasurementTraceContractSmoke.cs` and explicitly excludes `QuantityRuleEngine`.
- No QuantityRule Preview source, persistence/schema, reports/BOQ/estimate, palette, Curtain/native, LOCAL-003, installer, CI or GitHub Actions changes.
- No new rule/provenance engine and no public rule model changes.

## Validation plan

- Regression injects ` Rule:Ghost` and proves `ApplyMatching(...)` fails before quantity/provenance/freshness mutation.
- Existing cases continue to prove `Rule: Ghost` and `Rule:   ` fail closed and canonical `Rule:Ghost` stale provenance is removed exactly.
- Re-fetch `main` immediately after claim and abort/source-skip on any overlapping Rules reservation.
- Inspect exact production/test commit diffs and final current-main readback.
- Connector-only validation is not an executable .NET build/smoke run; no managed/native PASS will be claimed unless actually executed.
