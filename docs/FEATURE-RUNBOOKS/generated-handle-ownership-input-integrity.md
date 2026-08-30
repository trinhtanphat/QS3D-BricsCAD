# Generated-handle destructive input integrity

## Scope

This `REMOTE_SAFE` Core contract covers caller-controlled `IEnumerable<string>` input to `GeneratedHandleOwnershipPolicy.ValidateAllBeforeErase`. Runtime status is `NOT_APPLICABLE`; this lane does not establish licensed BricsCAD `LOCAL_PASS` evidence.

The method gates destructive replacement, so malformed or unbounded input must fail before native ownership validation begins.

## Admission contract

Destructive handle input is bounded to **10,000** entries. When the source exposes a known cardinality through `ICollection<string>`, `IReadOnlyCollection<string>`, or non-generic `ICollection`, all available Count surfaces must agree, remain non-negative, stay within the hard cap, and remain stable throughout traversal.

Caller-controlled traversal is explicit and ordered as:

`Count rebound -> MoveNext -> Count rebound -> known-count/hard-cap admission -> Current -> Count rebound -> normalize`

This ordering rejects N+1 input and MoveNext-induced transient Count growth, shrink, negative Count, or cross-interface conflicts before `Current` is read for the affected entry. A post-`Current` Count rebound also prevents a Current getter from changing cardinality metadata and then having that entry normalized under stale Count evidence.

After enumeration, Count is rebound again and exact observed cardinality is required, preserving under-yield detection. Pure streaming sources without a supported Count surface remain accepted up to the 10,000-entry hard cap, and the 10,001st `Current` is never read.

## Destructive atomicity

Blank or duplicate normalized handle identities remain rejected. Accepted handles remain sorted with `StringComparer.OrdinalIgnoreCase` before semantic/native ownership checks.

Most importantly, the complete source is admitted, normalized, bounded, Count-validated, and sorted before the first `nativeOwnershipValidator` call. Every source-admission failure therefore has **zero native callback** invocations.

Existing semantic-owner checks remain unchanged: each handle must resolve to the expected semantic element and logical owner slot before its native validator runs.

## Deterministic regression

`tests/QS3D.Core.SmokeTests/GeneratedHandleOwnershipInputIntegritySmoke.cs` covers:

- known Count N+1 rejection without an extra `Current` read;
- MoveNext-induced Count growth and shrink before `Current`;
- transient negative Count and cross-interface Count conflict before `Current`;
- known Count under-yield with zero native callbacks;
- pure streaming 10,001-entry rejection after exactly 10,000 `Current` reads and zero callbacks;
- stable multi-interface counted input with deterministic sorted callback order;
- pure streaming compatibility with deterministic sorted callback order.

`scripts/preflight-generated-handle-ownership-input-integrity.py` pins the production ordering, supported Count surfaces, hard cap, no-foreach rule, exact-one `Current` read, callback ordering, hostile regression names, and runbook contract.

## Validation

Use normal Shared Branch and Integration CI. Exact branch-head `preflight` + `core` evidence is required before opening the protected PR. If protected `main` advances, reconcile on the same canonical branch without force-push, obtain fresh branch evidence, then require current protected PR `preflight` + `core` before expected-head merge.
