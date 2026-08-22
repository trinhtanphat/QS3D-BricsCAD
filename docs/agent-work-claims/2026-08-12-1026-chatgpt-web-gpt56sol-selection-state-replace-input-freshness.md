# Work claim — SelectionState replacement input freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-selection-state-replace-input-freshness`
- Registered: `2026-08-12T10:26:00+07:00`
- Completed: `2026-08-12T10:26:00+07:00`
- Baseline main SHA: `7c43babfd7063b9d84dd0c097f72af4c8a2dd49f`
- Priority: P1 — fail-closed semantic selection replacement at a caller-controlled reentrant enumeration boundary.

## Confirmed defect

`SelectionState.Replace(IEnumerable<string>)` materialized caller-controlled lazy input before applying it. During enumeration, the producer could reentrantly call `Clear()` or `Replace()` on the same `SelectionState`. The outer `Replace()` did not detect that effective selection mutation and could overwrite the newer inner state using stale materialized input.

## Implemented contract

- Added a private monotonic `_changeVersion` for effective selection mutations.
- `Replace()` captures the revision immediately before enumerating caller input and rejects revision drift immediately after materialization, before `SetEquals` or replacement mutation.
- Effective `Replace()` and `Clear()` operations prepare the next checked revision before mutating `_ids`, apply that revision before `Changed`, and leave no-op mutations revision-stable.
- Preserved the 10,000 input cap including collection fast checks, blank/null skipping, trimming, case-insensitive de-duplication, deterministic `ElementIds` ordering, stable lazy input, and no-op event suppression.

## Regression coverage

Focused Core smoke source covers:

- stable lazy normalization/de-duplication and no-op event behavior;
- effective reentrant `Replace()` preserving the newer inner selection while the outer call fails closed;
- effective reentrant `Clear()` followed by empty outer enumeration, proving freshness rejection precedes no-op comparison;
- reentrant no-op replacement, proving only effective mutation invalidates the outer operation.

A `ModuleInitializer` registration and static preflight lock the source ordering, atomic revision advancement, smoke cases, and registration.

## Evidence

- Claim registration: `5424315d1b23146e959d619f46b5c1c70325a316`
- Plan: `2e386950d238f96263a2e29bc6a0d761295eb3b6`
- Source fix: `509e9ea0bc214fdb50a8fb087fe13661c4d4343d`
- Smoke regression: `efb7d712c796157ab1c445a72502dac5f1398a52`
- Smoke registration: `f3342a8990ae9396457100ef4cba8c3d09ff9ada`
- Static preflight: `f8a949cba3363611fb75a3c1f08d550c80025f9e`
- Latest-main readback confirmed source, smoke, and preflight content after concurrent repository changes.

## Validation limitations

The smoke executable and Python preflight were not executed in this connector-only environment. No GitHub Actions/build/release dispatch or licensed BricsCAD V25/V26 runtime PASS is claimed.

## Excluded scope

- Full thread-safety or cross-thread synchronization guarantees.
- BricsCAD implied-selection bridge/UI behavior.
- Selection Inspector and unrelated selection diagnostics.

## Completion

`COMPLETED`: an effective selection mutation that occurs while caller-controlled replacement IDs are being enumerated can no longer be overwritten by the stale outer `Replace()` operation.
