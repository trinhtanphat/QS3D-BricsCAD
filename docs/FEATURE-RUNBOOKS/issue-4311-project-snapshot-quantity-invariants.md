# Issue #4311 — Project snapshot quantity invariants

Status: `SOURCE_FIX_ACTIVE`

Lane-Key: `issue-4311`

Canonical owner: independent QS3D schedule worker `C02`

Runtime: `NOT_APPLICABLE` — deterministic Core state/rollback integrity.

## Problem

`ProjectElement.SetQuantity(...)` is the canonical quantity mutation boundary: it requires a canonical non-control name, rejects negative and non-finite values, and normalizes zero. The public `Quantities` dictionary remains mutable for compatibility, so hostile or stale callers can bypass that setter.

Before this lane, `ProjectStateSnapshot` bounded the quantity dictionary cardinality but copied entries with raw dictionary assignment. Snapshot capture/detached copy could therefore retain quantity state that the canonical domain mutation API would reject, and transactional rollback could later restore that non-canonical state.

## Hardened contract

- Snapshot validation checks every element quantity before clone/rollback materialization.
- Empty, padded or control-character quantity names fail closed.
- Negative, NaN and Infinity values fail closed.
- Quantity cloning/restoration routes through `ProjectElement.SetQuantity(...)` rather than raw dictionary assignment.
- Directly injected IEEE-754 negative zero remains a valid zero magnitude but is normalized to canonical positive zero by the same domain setter.
- Dirty flags and `UpdatedUtc` are restored after canonical quantity materialization, so validation/cloning does not invent persisted mutation state.

## Regression

`ProjectStateSnapshotElementIdentitySmoke` now covers direct mutable corruption for negative/NaN/Infinity values and non-canonical names, proves rejection is source-state side-effect free, verifies negative-zero canonicalization, and retains the existing identity-preserving rollback, detached-copy and revision-ceiling matrix.

`preflight-project-snapshot-quantity-invariants.py` is auto-discovered and locks the validation call, canonical setter copy path, absence of the former raw dictionary assignment and the focused regression controls.

## Landing

Use the canonical branch `agent/c02/issue-4311-project-snapshot-quantity-invariants`. Required endpoint is automatic exact-head branch CI, latest-main reconciliation if needed, one PR with `Lane-Key: issue-4311`, protected current-candidate `preflight` + `core` SUCCESS, expected-head merge and exact resulting `main` verification.

No licensed BricsCAD host, private DWG or `LOCAL_PASS` evidence applies.
