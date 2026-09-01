# TBQ workspace base-total precision

## Purpose

`TbqProjectWorkspaceState.BaseTotal` is project-level commercial truth. It must preserve every representable bill-item contribution without rejecting a project merely because an intermediate pairwise decimal sum cannot carry a contribution that the complete exact aggregate can represent.

## Contract

The workspace snapshots and canonicalizes bill items before `BaseTotal` is read. Base-total evaluation first materializes each already-validated `TbqBillItem.TotalCost`, then evaluates the complete exact aggregate with the shared non-negative decimal accumulator. Conversion back to `decimal` happens only after the whole contribution set is known.

For the canonical high-dynamic-range case

- `10000000000000000000000000000m`
- `0.5m`
- `0.5m`

the representable final total is `10000000000000000000000000001m`. The result must be identical regardless of caller input order because `TbqProjectWorkspaceState` preserves canonical bill-item ordering.

If the final aggregate is not representable as `decimal`, `BaseTotal` fails closed through its existing overflow boundary. The correction does not round, drop, or silently absorb non-zero costs.

## Preserved boundaries

This change does not weaken per-item multiplication precision/overflow checks in `TbqBillItem.TotalCost`. Collection maximums, duplicate-id validation, Count stability, build-up/reference/library snapshots, adjustment ratios, trade analysis, persistence, and canonical ordering remain unchanged.

## Deterministic validation

`TbqWorkspaceBaseTotalPrecisionSmoke` covers the recoverable half-unit aggregate, caller-order independence after canonicalization, an ordinary total plus zero-ratio preview, and a complete aggregate that is genuinely unrepresentable and must fail closed.

`scripts/preflight-tbq-workspace-base-total-precision.py` rejects a return to pairwise BaseTotal addition and pins the exact whole-set aggregation contract.

This is Core/commercial arithmetic only and requires no licensed BricsCAD runtime or private DWG evidence.
