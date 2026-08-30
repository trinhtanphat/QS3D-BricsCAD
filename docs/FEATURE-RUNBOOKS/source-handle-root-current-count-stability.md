# SourceHandleResolver root Current-induced known-Count stability

## Scope

This runbook covers deterministic Core validation for `SourceHandleResolver.Resolve(...)` while materializing Locate root semantic element ids. It does not require licensed BricsCAD runtime execution.

## Failure model

A supported counted enumerable can execute caller-controlled logic inside both `MoveNext()` and `Current`. If Locate snapshots Count before traversal and only revalidates after `MoveNext()` or after traversal, a hostile enumerator can let `Current` corrupt Count and then restore the original value from the next `MoveNext()`. Final cardinality checks can then accept semantic input whose admitted Count evidence was transiently invalid.

## Required traversal invariant

For root element-id materialization:

1. Snapshot and validate all supported known Count surfaces before traversal.
2. Revalidate the exact admitted Count/source set immediately before every `MoveNext()`.
3. After every successful `MoveNext()`, revalidate Count again before the admitted-count overrun gate, the 10,000-entry hard cap, and before `Current`.
4. Read `Current` exactly once for the accepted traversal step.
5. Revalidate Count immediately after that `Current` read and before accepting/materializing the semantic root id.
6. A Current-induced growth, shrink, negative Count, or disagreement between supported Count interfaces must reject after that one `Current` read and before another caller `MoveNext()`.
7. Preserve final observed-count parity and post-traversal Count/source revalidation, including under-yield rejection.
8. Preserve canonical semantic-id validation, project freshness/ownership checks, stable counted behavior, pure-streaming behavior, and the existing hard cap.

## Deterministic regression

`SourceHandleRootCurrentCountStabilitySmoke` uses hostile counted enumerables whose first `Current` mutates the exposed Count evidence. The hardened implementation must reject with `MoveNextCalls == 1` and `CurrentReads == 1` for:

- Current-induced Count growth;
- Current-induced Count shrink;
- Current-induced negative Count;
- Current-induced disagreement between generic and read-only Count surfaces.

The smoke also validates that stable counted input still resolves successfully. Existing root known-Count and transient-`MoveNext()` smokes remain part of the acceptance boundary.

## Source guards

Run:

```text
python scripts/preflight-source-handle-root-known-count-integrity.py
python scripts/preflight-source-handle-root-transient-known-count-stability.py
```

The guards require explicit loop control and pin the ordering `Count rebound -> MoveNext -> Count rebound -> admitted/hard-cap gates -> Current -> Count rebound`, while preserving final Count revalidation. They must not be weakened to accommodate a failing regression.

## Repository validation

Run the normal deterministic Core validation used by CI:

```text
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Then require Shared CI and protected PR `preflight + core` on the exact current candidate. If protected `main` advances, collision-scan all reserved task paths and reconcile the same canonical branch without force before relying on prior candidate evidence.

## Acceptance boundary

This work is `REMOTE_SAFE` deterministic Core semantic integrity. CI build/smoke evidence is sufficient for this source contract. Do not claim licensed BricsCAD or private-DWG `LOCAL_PASS` from this runbook.