# Work claim — Quantity Rule leading-whitespace provenance canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-leading-provenance-whitespace-20260813-1307`
- Registered: `2026-08-13T13:07:00+07:00`
- Baseline main SHA: `1ebfd929e05db6c441b0a62fbc8632fcfcfe1f69`
- Priority: `P0 Measurement Rules` — canonical/fail-closed persisted rule provenance

## Confirmed gap

The existing quantity-rule provenance read path rejected malformed keys such as `Rule: Ghost`, but `GetStaleManagedOutputs(...)` filtered properties with `StartsWith("Rule:")` before canonicality validation. A persisted/directly injected key such as ` Rule:Ghost` therefore bypassed the rule-provenance namespace entirely. It could survive stale cleanup or coexist with a subsequently written canonical `Rule:Ghost` shadowing the same logical provenance output.

## Reserved scope

- `src/QS3D.Core/Rules/QuantityRuleEngine.cs`
- `tests/QS3D.Core.SmokeTests/QuantityRuleProvenanceCanonicalReadSmoke.cs`
- this claim file.

## Implemented contract

- `GetStaleManagedOutputs(...)` now inspects the trimmed form of every property key only to decide whether it enters the `Rule:` namespace.
- If the trimmed key enters that namespace but differs from the raw key, evaluation fails closed with the existing malformed-provenance error before any rule output/provenance mutation.
- Canonical `Rule:<OutputName>` handling, existing post-prefix canonicality checks, active-output behavior and canonical stale cleanup remain unchanged.
- Unrelated padded non-rule properties are still ignored by this provenance guard.

## Commits

- Claim-only: `320fe55eff4583e979fb0c9ca383c0f3efd07c58` — `chore(agent): claim leading-whitespace rule provenance canonicality`.
- Production fix: `5b89db619ce783336aa944ce7cff5db012aa326e` — `fix(rules): reject padded provenance namespace keys`.
- Regression: `5b3d55b0ba1ebd7fdbfbcefa0ee0178a12547588` — `test(rules): cover padded provenance namespace prefix`.

## Validation actually executed

- Refreshed current `main` and reviewed the concurrent ACTIVE MTR-02 claim; its reserved files are only `MeasurementTrace.cs` + `MeasurementTraceContractSmoke.cs` and it explicitly excludes `QuantityRuleEngine`, so no ownership overlap exists.
- Exact production commit diff confirms only `QuantityRuleEngine.cs` changed, replacing the raw-prefix filter with trimmed-namespace recognition plus whole-key canonicality rejection.
- Exact regression commit diff confirms only `QuantityRuleProvenanceCanonicalReadSmoke.cs` changed, adding one leading-whitespace case.
- Regression proves ` Rule:Ghost` fails before quantity/provenance/freshness mutation while the pre-existing smoke cases continue to cover `Rule: Ghost`, blank provenance output, active malformed provenance and exact canonical stale cleanup.
- The provenance smoke remains automatically executed through its existing `[ModuleInitializer]`; no shared registration surface was edited.

## Unexecuted gates

- This connector environment did not execute `dotnet build`, the `QS3D.Core.SmokeTests` binary, GitHub Actions, or BricsCAD native/runtime qualification. No PASS is claimed for those unexecuted gates.

## Completion condition

Satisfied for this narrow P0 Measurement Rules provenance-integrity lane: ownership was claimed before source changes, leading-whitespace provenance namespace aliases now fail closed in the existing engine, regression is committed on the existing smoke surface, exact diffs were inspected, and the claim is released as `COMPLETED` without representing unexecuted managed/native gates as PASS.
