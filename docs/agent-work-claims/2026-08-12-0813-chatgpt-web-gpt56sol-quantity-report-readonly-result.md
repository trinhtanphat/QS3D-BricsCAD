# Work claim — Quantity report structural read-only result

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-report-readonly-result-20260812-0813`
- Registered: `2026-08-12T08:13:00+07:00`
- Completed: `2026-08-12T08:14:00+07:00`
- Baseline main SHA: `9cc952dc2457c558dca2d81ffbc366a202b365e7`
- Claim commit: `dc159e599696483cae4e730609c92393c3c0e163`
- Source commit: `b3000e17411399e472639d869ae94f15d3f30ee1`
- Regression commit: `f29c53b8de34dac8234c55fb3fdfbc6af5c1c2a5`
- Priority: evidence-driven public Reporting result ownership during owner-requested `continue all`

## Confirmed defect fixed

`QuantityReportBuilder.Group(IEnumerable<ElementInstance>)` declares `IReadOnlyList<QuantityReportRow>` but previously returned its mutable backing `List<QuantityReportRow>` directly. A caller could cast the result to a mutable collection and structurally add, remove or clear grouped rows after aggregation had completed.

## Completed change

- The completed grouped result is now returned through `result.AsReadOnly()`.
- Grouping order/key semantics, row objects, source-handle provenance, count arithmetic, quantity accumulation, duplicate-element rejection and existing exception behavior are unchanged.
- No deep-immutability redesign of `QuantityReportRow` was made.

## Regression evidence

`QuantityReportReadOnlyResultSmoke` builds two ordinary report groups from three elements, verifies first-seen group order, counts and GrossConcreteM3 aggregation, requires `ICollection<QuantityReportRow>.IsReadOnly`, and proves structural `Add` throws `NotSupportedException`.

## Read-back validation

Current `main` source was re-fetched after the source/regression writes and still contains `return result.AsReadOnly();`. The focused smoke was also re-fetched from `main` and retains the intended grouping and structural-mutation checks.

## Excluded scope respected

No ProjectQuantityReportBuilder selection logic, deduction rules, XLSX/export UI, CAD/native behavior, persistence, release/update or quantity arithmetic redesign was changed.

## Validation boundary

Remote source/smoke read-back only. No GitHub Actions were dispatched; no executable Core build/smoke PASS and no BricsCAD V25/V26 runtime qualification are claimed.
