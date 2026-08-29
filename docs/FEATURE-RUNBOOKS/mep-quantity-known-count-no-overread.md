# MEP quantity known-Count no-overread

Lane-Key: `issue-4464`

`MepQuantityService.Aggregate` consumes caller-supplied MEP elements under a deterministic 10,000-element ceiling and, when available, a collection `Count` contract exposed through `ICollection<MepElement>`, `IReadOnlyCollection<MepElement>`, or non-generic `ICollection`.

A C# `foreach` reads `IEnumerator.Current` after `MoveNext()` but before the loop body. Checking known-count overrun or the streaming ceiling only inside that body therefore observes one disallowed caller-controlled element before rejecting it. A malicious or stateful enumerator can make that extra `Current` access mutate state or throw a different exception, outranking the admitted cardinality contract.

#4464 changes the materialization boundary to explicit enumeration: after each successful `MoveNext()`, the known-count and streaming-ceiling checks run before `Current` is accessed. Existing negative/conflicting/oversized Count admission, exact under-yield refusal, post-traversal Count rebinding, duplicate/null validation, deterministic grouping/order, compensated finite aggregation, and stable/pure-streaming behavior remain unchanged.

Deterministic adversarial coverage is self-registering in `tests/QS3D.Core.SmokeTests/MepQuantityKnownCountNoOverreadSmoke.cs`. It proves that a Count=N overrun performs N `Current` reads only, while a pure-streaming 10,001st element is rejected after `MoveNext()` without reading its `Current`. The auto-discovered guard is `scripts/preflight-mep-quantity-known-count-no-overread.py`.

Runtime classification: `NOT_APPLICABLE`. This is deterministic Core MEP quantity correctness and does not require licensed BricsCAD runtime evidence; hosted compilation is not `LOCAL_PASS`.
