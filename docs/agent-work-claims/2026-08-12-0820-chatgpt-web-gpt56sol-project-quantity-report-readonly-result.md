# Work claim — Project quantity report structural read-only result

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-project-quantity-report-readonly-result-20260812-0820`
- Registered: `2026-08-12T08:20:00+07:00`
- Completed: `2026-08-12T08:21:00+07:00`
- Baseline main SHA: `1d99fe7b9b8b8a753054591b11439256ed7c3ad9`
- Claim commit: `77e2b8cc94340a8d2b51d95b3afc01060b28526b`
- Source commit: `987e5d369d889154fa0596b16beb7e6733406fc8`
- Regression commit: `d322aa4f35b552f7e09c91e688a03d9a8bb2e986`
- Priority: evidence-driven public Reporting result ownership during owner-requested `continue all`

## Confirmed defect fixed

All public `ProjectQuantityReportBuilder.Group(...)` and `Detail(...)` paths share private `Build(...)`, whose return type is `IReadOnlyList<QuantityReportRow>` but previously returned `order.Select(...).ToList()` directly. Callers could cast the result to a mutable collection and structurally add, remove or clear rows after aggregation had completed.

## Completed change

The shared Build return now uses `order.Select(x => rows[x]).ToList().AsReadOnly()`. Group/detail keys, selection canonicality, ordering, quantities, mass/density handling, source-handle resolution, identity validation and existing exception behavior are unchanged. No deep-immutability redesign of `QuantityReportRow` was made.

## Regression evidence

`ProjectQuantityReportReadOnlyResultSmoke` builds two Beam elements in one family. Group still returns one row with Count=2 and LengthM=5; Detail still returns two rows with Count=1 each and aggregate LengthM=5. Both returned values must expose read-only `ICollection<QuantityReportRow>` boundaries and structural `Add` must throw `NotSupportedException`.

## Coordination respected

The recent duplicate-selection canonicality source/regression remains unchanged. This lane did not edit `ResolveSelection(...)`, selection enumeration, source-handle traversal, BLT quantity preset/settings or any local V25/Core-gate reserved file.

## Read-back validation

Current `main` source was re-fetched after publication and still contains the `ToList().AsReadOnly()` return. The focused smoke was also re-fetched from `main` with both Group and Detail checks intact.

## Validation boundary

Remote source/smoke read-back only. No GitHub Actions were dispatched; no executable Core build/smoke PASS and no BricsCAD V25/V26 runtime qualification are claimed.
