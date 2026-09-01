# Generated-handle owner token integrity

## Scope

This `REMOTE_SAFE` Core contract hardens persisted generated-handle ownership provenance consumed by `GeneratedHandleOwnershipPolicy`. Runtime status is `NOT_APPLICABLE`; no licensed BricsCAD `LOCAL_PASS` claim is established by this lane.

This work is a follow-up to #4778. That carrier bounded caller-controlled destructive handle enumeration and stabilized Count evidence. This carrier addresses the persisted semantic owner-token boundary used by ownership lookup and destructive authorization.

## Fail-closed persisted token contract

Generated owner properties may contain multiple handles separated by `;`. Parsing must preserve delimiters with `StringSplitOptions.None` so empty positions are observable rather than silently removed.

Every persisted token must already be a canonical generated-handle identity. Empty or whitespace-only tokens, leading/trailing/double delimiters, padded values, alternate spellings that normalize to a different canonical identity, and duplicate tokens within one property are malformed provenance and must fail closed with an explicit integrity error.

The parser must not use `RemoveEmptyEntries`, filter normalized blanks away, or collapse duplicates with `Distinct`. Those patterns repair corrupted provenance into a smaller valid-looking set and can allow ownership reasoning to proceed on incomplete evidence.

Logical handle equality outside the persisted representation remains case-insensitive. For example, a caller may look up canonical persisted handle `A` using logical handle `a`; the persisted owner property itself must still use the exact canonical spelling `A`.

## Public-surface propagation

`EnumerateOwnerHandles`, `EnumerateLogicalOwnerHandles`, `CollectOwnerHandles`, `TryFindOwner`, and `ValidateAllBeforeErase` all flow through the same fail-closed persisted token parser. No public ownership path may bypass malformed-token rejection.

For destructive validation, complete caller-input admission from #4778 remains intact. If persisted owner provenance is malformed, `ValidateAllBeforeErase` must fail before the affected `nativeOwnershipValidator` invocation. Deterministic regression pins zero native callback observation for this failure mode.

## Deterministic regression

`GeneratedHandleOwnerTokenIntegritySmoke` covers leading, trailing, and double delimiters; whitespace-only tokens; duplicate tokens; padded and other non-canonical spellings; propagation through all ownership public surfaces; zero native callback on malformed persisted provenance; and a valid multi-handle control preserving case-insensitive logical lookup and deterministic destructive validation.

`scripts/preflight-generated-handle-owner-token-integrity.py` is auto-discovered and pins `StringSplitOptions.None`, canonical exact-token comparison, duplicate detection, absence of silent-repair patterns, smoke coverage, and this runbook contract.

## Validation

Use normal Shared Branch and Integration CI. Obtain exact-head branch `preflight` and `core` success, reconcile latest protected `main` on the same canonical branch without force-push, obtain fresh evidence when the head changes, then require protected PR `preflight` + `core` before an expected-head merge. Verify exact protected main contains the merge.
