# Grid naming bounded-enumeration plan — 2026-08-12

## Goal

Make the existing `GridNamingService.Renumber()` capacity contract bound source enumeration/allocation as well as accepted cardinality, without changing Grid naming semantics.

## Confirmed baseline defect

Post-claim source was re-fetched from `main` at `c324c7e8447cbb4d66ef823db5c27a624cc8c9b3`.

The method declared `MaxGridBatch = 2000` but executed:

```text
orderedGridElementIds.Select(...).ToList()
```

before checking whether the materialized list exceeded 2,000 entries. A large or non-terminating lazy enumerable could therefore be consumed indefinitely before the declared guard was reached.

This lane is independent from active Grid intersection/spatial bounded-enumeration work because it reserves only `GridNamingService.Renumber()` semantic naming input materialization.

## Invariants preserved

- Public capacity remains exactly 2,000 Grid ids.
- Empty input still fails with the existing empty-input message.
- Accepted ids are still trimmed/validated through indexed `Required(...)` calls.
- Duplicate ids remain case-insensitive failures.
- Prefix/suffix, numeric/alphabetic sequence, padding and range rules are unchanged.
- Project semantic-id validation, Grid-category validation, label collision checks and ordering are unchanged.
- Real renumber mutations still Touch once before writes; unchanged plans remain no-op.
- No Grid annotation/native geometry behavior is changed.

## Implementation

### 1. Bounded one-pass materialization

Replace LINQ full materialization with a single `foreach` over the source:

- before adding each yielded value, check whether 2,000 accepted ids are already buffered;
- if yes, the current yielded item is the 2,001st and the method throws the existing capacity error immediately;
- otherwise normalize the value using the existing index-based `Required(...)` call and append it.

This means an oversize enumerable is requested only through item 2,001. Item 2,002 is never requested after oversize cardinality is known.

### 2. Regression model

`GridNamingBoundedEnumerationSmoke` uses an intentionally unbounded lazy source that:

- increments a visible yield counter;
- yields valid Grid-like ids through count 2,001;
- throws a sentinel exception if count 2,002 is ever requested.

The expected contract is the normal `InvalidOperationException` capacity error after exactly 2,001 yielded values. The smoke also checks `ProjectState.ChangeVersion` is unchanged, proving the rejection occurs before semantic mutation.

The smoke is registered via an isolated module initializer so concurrent agents do not need to modify shared test registration.

### 3. Static regression gate

`preflight-grid-naming-bounded-enumeration.py`:

- requires manual one-pass enumeration and the in-loop `ids.Count == MaxGridBatch` check;
- requires the capacity check to occur before normalization/add and before project resolution;
- rejects the legacy `.Select(...).ToList()` materialization path and post-materialization `ids.Count > MaxGridBatch` check;
- requires the adversarial 2,001/2,002 smoke contract and module registration.

## Moving-main integration

- Claim commit: `c324c7e8447cbb4d66ef823db5c27a624cc8c9b3`.
- Work branch: `agent/grid-naming-bounded-enumeration-20260812`.
- Re-fetch moving `main` before PR and before merge.
- Compare changes since the branch point specifically for `GridNamingService.cs` and this lane's new files.
- If a concurrent winner changes `GridNamingService.cs`, do not overwrite it; re-read/reconcile only if non-overlapping.
- Otherwise open a focused PR and squash-merge with expected head SHA.
- Close the claim with exact evidence.

## Validation policy

No GitHub Actions are dispatched because repository policy is manual-only and this request did not separately authorize workflow execution. The browser/container environment already failed GitHub checkout DNS earlier in this audit, so executable smoke/preflight PASS is not claimed unless an actual run becomes available. The committed regression/static gate plus source/diff review are the remote evidence for this pure Core lane; no BricsCAD V25 runtime PASS is claimed.
