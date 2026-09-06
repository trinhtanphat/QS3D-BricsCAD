# Project persistence stamp bounded semantic snapshot

Issue: #5984
Lane: C01 Core / Persistence / Data Integrity
Runtime: REMOTE_SAFE deterministic managed Core

## Defect

`ProjectPersistenceStamp` bounds top-level and nested collection cardinality, but historically materialized semantic persistence content into an unbounded `StringBuilder` twice for stability. One oversized in-memory string, or many individually valid strings whose framed content grows without bound, could therefore force uncontrolled duplicate allocation during `ProjectPersistenceStamp` construction, `RequiresSave`, or `MarkSaved` before QSDB file-size admission is reached.

## Contract

- Semantic snapshot framing has an explicit 64 Mi-character materialization budget.
- Every framing append (`AppendSequenceCount`, `AppendInt32`, `AppendDouble`, `AppendString`) must admit the complete pending append against the remaining budget before mutating the builder.
- Budget overflow fails deterministically with `InvalidOperationException` and does not weaken collection-count, ordering, nested-stability, semantic-equality, or project-affinity checks.
- The runtime smoke exercises the production budget primitive without allocating a near-limit string; the focused preflight proves all append entry points remain wired through that primitive.

## Remote validation

Run:

```text
python scripts/preflight-project-persistence-stamp-bounded-snapshot.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Then require protected Shared CI `preflight` and `core` SUCCESS on the exact current PR candidate before merge.

No licensed BricsCAD runtime evidence is required or claimed for this managed persistence invariant.
