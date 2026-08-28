# Regeneration work profile known-Count stability

Carrier: `issue-4456`
Reservation protocol: `v2`
Canonical branch: `agent/longnguyentuan2107-maker-c01-20260829/issue-4456-regeneration-profile-count-stability`

## Defect

`RegenerationWorkProfile.MaterializeBounded<T>` admitted collection `Count` evidence before traversal but used `foreach`. C# `foreach` reads `IEnumerator.Current` after a successful `MoveNext()` and before entering the loop body, so the previous in-body overrun guard could observe one `Current` beyond the admitted known Count. The same ordering also affected the uncounted project-element ceiling.

## Required invariant

Traversal must be explicit and ordered as follows:

1. validate all available known Count surfaces before traversal;
2. call `MoveNext()`;
3. reject known-Count overrun before reading `Current`;
4. reject the streaming/project-element ceiling before reading `Current`;
5. read and validate `Current`, then retain it;
6. after traversal, reject known-Count under-yield;
7. re-read known Count surfaces and reject negative/conflicting/drifted evidence.

Targets, work items and category collections all pass through the same materializer and therefore share this contract.

## Deterministic validation

Run from repository root:

```text
python scripts/preflight-regeneration-work-profile-known-count-stability.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
```

The adversarial smoke counts `MoveNext` and `Current` reads. A Count=1 source that yields a second item must reach the second `MoveNext` but must still report only one `Current` read. Equivalent coverage is required for targets, work items and categories. The smoke also preserves under-yield, post-traversal drift, streaming ceiling and honest counted-input controls.

This is a Core-only deterministic acceptance package. No licensed BricsCAD runtime claim is part of this carrier.
