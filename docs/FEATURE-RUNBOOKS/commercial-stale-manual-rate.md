# Commercial stale manual-rate immutability

Issue: #5390
Lane-Key: `issue-5390`
Runtime: `NOT_APPLICABLE` — deterministic QS3D.Core behavior.

## Invariant

Once an estimating line is marked stale because its quantity-source revision is no longer current, its last valid commercial rate/amount is historical evidence. While `IsStale == true`, manual rate override creation and removal must fail closed. Neither operation may construct a replacement line/portfolio or append a commercial audit record.

The existing bulk-rate workflow already treats stale lines as blocking. This package applies the same provenance boundary to the independent manual override create/remove APIs without changing fresh-line behavior.

## Deterministic acceptance

Run:

```text
python scripts/preflight-commercial-stale-manual-rate.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The commercial estimating smoke proves both directions:

- A referenced/base-rate line is marked stale, then manual override creation is rejected; referenced/effective rate, amount, stale state and audit count remain unchanged.
- A manual override is created while a line is fresh, the line is then marked stale, and override removal is rejected; override/effective rate, amount, stale state and audit count remain unchanged.
- Existing fresh manual override create/remove behavior remains covered by the same canonical smoke.

## Protected merge acceptance

The exact reconciled candidate must pass the repository-required protected `preflight` and `core` jobs. Do not infer runtime or licensed BricsCAD acceptance from these deterministic Core checks.