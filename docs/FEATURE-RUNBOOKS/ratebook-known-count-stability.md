# RateBook deterministic Count stability

Status: `SOURCE_FIX_ACTIVE`

Lane-Key: `issue-4351`

Reservation-Protocol: `v2`

Canonical owner/session: `account:longnguyentuan2107-maker|session:c02-20260828-0946-ratebook-count-stability`

Canonical carrier: `agent/longnguyentuan2107-maker-c02-20260828-0946z/issue-4351-ratebook-known-count-stability`

Ownership-Key: `core.cost.ratebook-known-count-stability`

Runtime: `NOT_APPLICABLE` — deterministic Core cost/rate integrity.

## Problem

`RateBook` already samples deterministic Count metadata before traversal and rejects early over-yield or final observed-cardinality mismatch. Before this package, it did not sample those same Count interfaces after caller-controlled enumeration. A source could advertise Count=N, yield exactly N valid rate items, then change its reported Count as traversal completed and still be accepted.

That permits the immutable RateBook snapshot to be constructed from a collection whose deterministic shape changed while materialization was in progress.

## Hardened contract

- Pre-traversal Count admission remains authoritative for negative, conflicting and over-limit metadata.
- The first yielded item beyond a known Count still fails before item semantic validation.
- Under-yield still fails after traversal.
- When deterministic Count evidence existed at admission, every supported Count surface is sampled again after traversal.
- Post-traversal negative/conflicting Count evidence fails closed through the same shared Count reader.
- A stable but changed final Count fails with the dedicated `known count changed during traversal` error before sorting/publication.
- Pure streaming `IEnumerable<RateItem>` inputs remain governed by the independent 10,000-item traversal bound and do not acquire a synthetic Count requirement.
- Stable honest multi-interface collections remain accepted.
- Rate identity, duplicate rate-item IDs, scope/effective-time ambiguity checks, deterministic sort order and resolution semantics are unchanged.

## Deterministic evidence

`RateBookKnownCountTraversalSmoke` covers:

- existing first-overrun rejection;
- existing under-yield rejection;
- exact stable counted input;
- exact-cardinality traversal followed by Count drift;
- post-traversal conflict between generic/read-only and non-generic Count surfaces;
- honest stable multi-interface Count evidence;
- pure streaming input.

`scripts/preflight-ratebook-known-count-stability.py` pins the two-phase binding order and these regression controls.

## Landing

Use only the canonical carrier above. Required endpoint is exact-head branch CI, latest-main reconciliation if needed, canonical protected PR with current `preflight` + `core` terminal SUCCESS, expected-head merge, and exact resulting `main` verification.

No licensed BricsCAD host, private DWG, package/signing evidence or `LOCAL_PASS` claim applies.
