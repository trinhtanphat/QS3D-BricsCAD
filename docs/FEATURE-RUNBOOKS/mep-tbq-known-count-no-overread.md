# MEP/TBQ known-Count no-overread

Lane-Key: `issue-4467`

`MepTbqProjectionService.BuildReport` consumes caller-controlled quantity groups under an independent 10,000-group ceiling and, when available, a deterministic Count contract from `ICollection<MepQuantityGroup>`, `IReadOnlyCollection<MepQuantityGroup>`, or non-generic `ICollection`.

A C# `foreach` reads `IEnumerator.Current` after a successful `MoveNext()` but before the loop body. Cardinality checks inside that body therefore observe one disallowed caller-controlled group before rejecting a known-Count overrun or the 10,001st pure-streaming item.

The #4467 boundary uses explicit enumeration in `MoveNext -> known Count/ceiling checks -> Current` order. Initial negative/conflicting/oversized Count admission, exact under-yield refusal, post-traversal Count stability, null-group validation, decimal conversion, deterministic sorting, CSV and TBQ bill-item semantics are preserved.

Deterministic coverage is self-registering in `tests/QS3D.Core.SmokeTests/MepTbqKnownCountNoOverreadSmoke.cs`. It proves N+1 and 10,001st `Current` are never observed and keeps stable counted, under-yield, and pure-streaming controls. The discovered source guard is `scripts/preflight-mep-tbq-known-count-no-overread.py`.

Runtime classification: `NOT_APPLICABLE`. This is deterministic Core MEP/TBQ correctness and does not require licensed BricsCAD runtime evidence; hosted CI is not a `LOCAL_PASS`.
