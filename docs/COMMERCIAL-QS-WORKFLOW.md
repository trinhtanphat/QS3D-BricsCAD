# Commercial QS workflow

## Purpose

QS3D uses one connected commercial lifecycle rather than separate calculation islands:

`Quantity / BQ / DGKL -> Rate build-up / Estimate -> Tender -> Progress claim -> Variation -> IPC -> Final account`

Issue #6038 completes the settlement end of that lifecycle in `QS3D.Core`. It does **not** create another quantity engine, rate book, tender engine, progress engine, project store, or CAD-specific money calculator.

## Existing authorities reused

The following capabilities already existed before #6038 and remain authoritative:

- Quantity/BQ/DGKL/XLSX and reverse-trace workflows for measured quantities and workbook provenance.
- `QS3D.Core.Cost` rate book, rate build-up/cost analysis, historical benchmarks and tender evaluation contracts.
- `QS3D.Core.Commercial` estimating/revision/audit contracts and exact decimal arithmetic.
- `ProgressClaimService` / `ProgressClaimResult` for base-work progress valuation and retention.
- Existing Scheduling/Planning contracts for programme/progress allocations.

The settlement contracts added by #6038 consume those results. They do not recompute or fork them.

## Variation / Change Order

`CommercialVariation` records an explicit lifecycle state (`Pending`, `Approved`, `Rejected`, or `Superseded`), currency, proposed amount, approved amount and a `CommercialRevisionRef`.

`CommercialVariationRegister` snapshots a bounded generation of variation rows and exposes `ApprovedNetChange`.

Rules:

- Only `Approved` rows contribute to the approved contract change.
- Approved additions are positive and approved omissions may be negative.
- Pending/rejected/superseded rows must carry zero approved value and cannot leak into payment totals.
- IDs are unique case-insensitively.
- Register currency is canonical and every row must match it.
- Revision provenance is mandatory and must identify the same variation.
- The input generation is snapshotted/revalidated before publication.

## Interim Payment Certificate (IPC)

`InterimPaymentCertificateService.Create(...)` takes the existing `ProgressClaimResult` as the base-work authority, then combines only approved variation certification and explicit payment adjustments.

The current-period formulas are:

```text
variation certified = sum(signed approved variation certification this period)

gross certified = progress gross certified + variation certified
retention = progress retention + explicit variation retention

net certified = gross certified
              - retention
              + retention release
              - advance recovery
              - other deductions

cumulative net certified = previous net certified + net certified
```

Guards:

- unknown, pending, rejected or superseded variations cannot be certified;
- additions cannot be certified with negative values;
- omissions cannot be certified with positive values;
- previous + current certification cannot pass the signed approved amount;
- duplicate variation certification IDs fail closed;
- variation-retention/release/recovery/deduction/prior-net inputs that are defined as balances are non-negative;
- cumulative net certification cannot become negative;
- the variation certification input is bounded and generation-stable.

No jurisdiction-specific recovery, tax, bond, escalation or retention policy is silently invented. Those values must arrive as explicit governed inputs when applicable.

## Final Account

`FinalAccountService.Reconcile(...)` closes the settlement using the same approved variation register:

```text
final contract value = original contract
                     + approved variation net change
                     + explicit final adjustment

gross balance = final contract value - previous gross certified
unreleased retention = retention held - retention release

settlement = gross balance
           + retention release
           - final deductions
```

A positive settlement is exposed as `AmountDue`; a negative settlement is exposed as `RecoveryDue` without losing the signed reconciliation evidence.

Guards include canonical currency, non-negative balance inputs, no retention release above retention held, exact decimal arithmetic and rejection of a negative final contract value.

## Worked deterministic fixture

The regression contract uses:

- original contract: `1000`
- approved addition: `+120`
- approved omission: `-20`
- rejected proposal: excluded
- final adjustment: `-25`

Approved variation net is therefore `+100`, and final contract value is `1075`.

With previous gross certification `1000`, retention held/released `50/50` and final deductions `5`, the gross balance is `75`, unreleased retention is `0`, and final amount due is `120`.

The IPC fixture reuses an existing progress claim with gross `300`, retention `30`, net `270`; adds signed variation certification `+30`, explicit variation retention `3`, retention release `5`, advance recovery `10`, other deductions `2`, and prior net certified `500`. The resulting current net is `290` and cumulative net is `790`.

## BricsCAD product boundary

#6038 adds Core commercial settlement contracts and deterministic validation. It does **not** add a new `[CommandMethod]` or claim a new BricsCAD command name. Existing quantity/BQ commands and future/host UI surfaces should call these Core authorities rather than reproduce settlement arithmetic in the V25/V26 adapter.

This distinction is intentional: Core arithmetic/provenance can be validated in hosted CI, while any later modeless UI, command routing or licensed BricsCAD interaction must pass its own exact-runtime qualification.

## Validation boundary

The #6038 regression is registered in `QS3D.Core.SmokeTests` and covers approved/rejected variation state, signed omissions, duplicate/mixed-currency rejection, Progress Claim reuse, over-certification rejection, IPC retention/recovery/deduction reconciliation and Final Account retention/final settlement.

This package is `REMOTE_SAFE`. A hosted source/Core/build result is not a substitute for licensed BricsCAD V25/V26 `LOCAL_PASS`, and #6038 does not manufacture or claim native runtime evidence.