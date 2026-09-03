# MEP/TBQ transient known-Count stability

Lane-Key: `issue-4721`
Reservation-Protocol: `v2`
Runtime: `NOT_APPLICABLE` — deterministic Core MEP/TBQ quantity-commercial integrity.

## Contract

`MepTbqProjectionService.BuildReport(...)` must treat supported collection Count metadata as admitted semantic evidence, not as an advisory optimization. When a known Count is available, every traversal step must rebind that Count immediately before `MoveNext()` and again after a successful `MoveNext()` before capacity checks and before semantic `Current` is read.

Transient Count growth, shrink, negative Count, or disagreement between supported Count surfaces must fail closed before semantic `Current`. A hostile source must not be able to expose a quantity group and then restore the admitted Count before post-traversal validation.

The fix must preserve the existing 10,000-group streaming ceiling, known-Count N+1 no-overread behavior, under-yield rejection, stable counted and pure-streaming acceptance, deterministic report ordering, and TBQ bill projection semantics.

## Deterministic proof

`MepTbqTransientKnownCountSmoke` instruments Count, `MoveNext`, and `Current` independently. Hostile transient cases require `MoveNextCalls == 1` and `CurrentReads == 0`. Stable counted and streaming controls must continue to build one report row successfully.

No licensed BricsCAD or private DWG evidence is required for this Core-only contract, and no LOCAL_PASS may be inferred from hosted CI.
