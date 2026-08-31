# Generated owner-handle persisted list bound

## Scope

This Core-only contract covers persisted semantic ownership properties consumed by `GeneratedHandleOwnershipPolicy`, including generated solid/rebar owner slots used by ownership lookup, health/indexing, and destructive replacement validation. It does not change the separate tolerant stale-signature canonicalizer on `ProjectElement`.

## Safety invariant

Persisted generated-owner metadata must never materialize more entries than the canonical destructive handle-set envelope: **10,000 handle tokens per owner property**. The parser must enforce that limit while scanning the delimiter stream, before materializing the next token; eager `string.Split` is prohibited because it allocates the complete token array before the bound can be checked.

Existing ownership-provenance integrity remains fail-closed: blank tokens are invalid, persisted tokens must already equal their canonical handle spelling, and duplicate canonical handles in one owner property are invalid.

## Deterministic qualification

`GeneratedHandleOwnerListBoundSmoke` proves the exact 10,000-token boundary remains accepted, 10,001 tokens fail closed, and blank/noncanonical/duplicate tokens retain their historical rejection behavior. `preflight-generated-handle-owner-list-bound.py` pins delimiter scanning, the shared 10,000-entry envelope, canonicalization/duplicate checks, and the absence of eager `Split` in `SplitHandles`.

No licensed BricsCAD runtime is required for this Core ownership/provenance contract. Hosted CI must not be described as a native `LOCAL_PASS`.
