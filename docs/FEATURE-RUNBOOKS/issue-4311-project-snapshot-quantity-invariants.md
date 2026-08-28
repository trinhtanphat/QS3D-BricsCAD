# Issue #4311 — Project snapshot quantity invariants

Status: `SOURCE_FIX_ACTIVE`

Lane-Key: `issue-4311`

Canonical owner: independent QS3D schedule worker `C02`

Runtime: `NOT_APPLICABLE` — deterministic Core state/rollback integrity.

## Problem

`ProjectElement.SetQuantity(...)` is the canonical quantity mutation boundary: it trims names, rejects blank/control/malformed-XML names, rejects negative/non-finite values and normalizes zero. The public `Quantities` dictionary remains mutable for compatibility, so callers can bypass those invariants.

Before this lane, `ProjectStateSnapshot` bounded quantity dictionary cardinality but copied entries with raw dictionary assignment. Snapshot capture/detached copy could retain state the canonical domain API would reject, and names such as `AreaM2` plus ` AreaM2 ` could silently collapse if later routed through canonical mutation.

## Hardened contract

- Snapshot validation checks every element quantity before clone/rollback materialization.
- Blank, control-character or malformed XML/UTF-16 names fail closed.
- Names are evaluated by the same trim/case-insensitive canonical identity used by `SetQuantity`; two raw names that collapse to one identity fail closed rather than lose data.
- Negative, NaN and Infinity values fail closed.
- Quantity cloning/restoration routes through `ProjectElement.SetQuantity(...)`, replacing raw dictionary assignment.
- A single padded name is canonicalized by the domain setter, preserving existing setter semantics rather than imposing a stricter snapshot-only rule.
- Directly injected IEEE-754 negative zero is normalized to canonical positive zero.
- Dirty flags and `UpdatedUtc` are restored after canonical quantity materialization, so cloning does not invent persisted mutation state.

## Regression

`ProjectStateSnapshotElementIdentitySmoke` covers direct mutable negative/NaN/Infinity values, control/malformed UTF-16 names, canonical-name collision, padded-name canonicalization, negative-zero normalization, rejection side-effect freedom, and the existing identity-preserving rollback/detached-copy/revision-ceiling matrix.

`preflight-project-snapshot-quantity-invariants.py` is auto-discovered and locks canonical identity validation, XML validation, collision rejection, canonical setter copying, absence of raw dictionary copying, and focused regression controls.

## Landing

Use canonical branch `agent/c02/issue-4311-project-snapshot-quantity-invariants`. Required endpoint is automatic exact-head branch CI, latest-main reconciliation when needed, one PR with `Lane-Key: issue-4311`, protected current-candidate `preflight` + `core` SUCCESS, expected-head merge and exact resulting `main` verification.

No licensed BricsCAD host, private DWG or `LOCAL_PASS` evidence applies.
