# IFC round-trip known-Count no-overread

Lane-Key: issue-4475

## Contract

IFC round-trip bounded collection materializers must reject cardinality overrun without observing the disallowed caller-controlled `IEnumerator.Current` value. For counted input the required order is `MoveNext -> admitted Count overrun check -> streaming ceiling -> Current -> semantic validation/retention`. Pure streaming input retains the independent 10,000-item ceiling and must reject item 10,001 before reading its `Current` value.

Affected boundaries are projection dimensions, projection provenance, projection-set creation, quantity-evidence creation, and exchange-result-set creation.

Existing contracts remain authoritative: initial negative/conflicting/oversized Count refusal, exact under-yield rejection, post-traversal Count rebinding/stability, null/token validation, deterministic sorting/grouping, duplicate handling, immutable publication, and existing IFC identity semantics.

## Deterministic validation

Run the Core smoke project and `scripts/preflight-ifc-roundtrip-known-count-no-overread.py`. The adversarial collection reports Count=1 while yielding two elements and separately records `MoveNext` and `Current` observations. Each affected boundary must perform the second `MoveNext` needed to prove overrun while reading `Current` exactly once.

This is repository-safe Core/export integrity work. Licensed BricsCAD, private DWGs, Excel UI, signing, and `LOCAL_PASS` evidence are not required and must not be inferred from hosted CI.
