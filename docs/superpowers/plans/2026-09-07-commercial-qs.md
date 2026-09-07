# Commercial QS completion plan — issue #6038

## Goal

Complete the remaining first-class Commercial QS lifecycle without duplicating existing QS3D authorities. The bounded source/product scope is:

`Quantity/BQ/DGKL -> Rate build-up/Estimate -> Tender -> Progress claim -> Variation register -> IPC -> Final account`

The first four stages already exist on `main`; this issue adds only the missing settlement contracts and connects the full lifecycle in documentation/tests.

## Existing authorities that must be reused

- Quantity/BQ/DGKL/XLSX/reverse trace: existing Quantity and workbook workflows.
- Unit rates/rate build-up/cost analysis: `QS3D.Core.Cost`.
- Estimating and revision provenance: `QS3D.Core.Commercial`.
- Tender requirements/bids/evaluation: `QS3D.Core.Cost`.
- Progress valuation and retention: `ProgressClaimService` / `ProgressClaimResult`.
- Scheduling/progress allocations: existing Scheduling/Planning contracts.

No second quantity engine, rate book, tender store, progress engine, project store, or CAD-specific commercial arithmetic is allowed.

## New bounded contracts

### Variation / Change Order

Add immutable, provenance-bound variation records with explicit lifecycle state. Only `Approved` variations contribute to the approved contract change. Pending/rejected/superseded records must not leak into certified totals. Duplicate IDs and mixed currencies fail closed.

### Interim Payment Certificate (IPC)

Aggregate the existing `ProgressClaimResult` with bounded approved-variation certification lines and explicit commercial adjustments. The IPC must:

- preserve the existing base-work progress/retention result rather than recalculate it independently;
- reject certification against non-approved or unknown variations;
- reject previous/current variation certification that exceeds the signed approved amount;
- preserve omissions as signed negative approved changes;
- keep deductions, advance recovery, variation retention, and retention release explicit rather than inventing jurisdiction-specific policy;
- reject an impossible negative cumulative certified balance;
- use exact decimal arithmetic and immutable snapshots.

### Final Account

Reconcile:

`original contract + approved variation net + explicit final adjustment - prior gross certification + retention release - final deductions`

The result must expose final contract value, gross balance, remaining retention, amount due and recovery due. It must reject impossible negative final contract value and retention release beyond retained balance.

## TDD sequence

1. Add `CommercialQsSettlementSmoke.cs` referencing the intended API before production implementation.
2. Push and observe hosted CI RED for the missing settlement types.
3. Add `src/QS3D.Core/Commercial/CommercialQsSettlement.cs` with the minimal contracts required by the regression.
4. Register the smoke in `SmokeTestEntryPoint`.
5. Add `docs/COMMERCIAL-QS-WORKFLOW.md` and update the command/product documentation only where it accurately reflects existing user-facing surfaces.
6. Run fresh exact-head CI. Fix only evidenced failures without weakening guards.
7. Refresh protected `main`, reconcile non-force, rerun exact-head required CI, open/refresh the canonical PR, and merge only when green/current/mergeable/collision-clean.
8. Verify post-merge `main`.

## Regression cases

- Approved +120 and approved omission -20 with rejected +90 => approved net +100.
- Duplicate variation ID => fail closed.
- Mixed currency => fail closed.
- Existing progress claim computes the base-work value/retention.
- Variation certification +50 and omission certification -20 => signed +30 current variation certification.
- Certification beyond approved signed amount => fail closed.
- Certification of rejected/pending variation => fail closed.
- IPC exact totals preserve explicit retention/recovery/deductions/release.
- Final account: original 1000 + approved variations 100 - final adjustment 25 = 1075 final contract value; prior gross 1000, retention release 50 and deductions 5 => 120 amount due.
- Retention release above retained balance => fail closed.

## Runtime boundary

This is REMOTE_SAFE source/Core/product-contract work. Hosted CI can prove source shape, deterministic arithmetic and compatibility; it cannot manufacture licensed BricsCAD V25/V26 native `LOCAL_PASS`. Existing native qualification parents remain authoritative.