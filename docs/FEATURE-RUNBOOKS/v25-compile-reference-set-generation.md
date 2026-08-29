# V25 compile-reference set-generation integrity

## Purpose

The V25 plugin build consumes three managed BricsCAD references together: `BrxMgd.dll`, `TD_Mgd.dll`, and `TD_MgdBrep.dll`. A valid build snapshot must represent one simultaneously admitted source-reference generation, not three independently stable files captured at different times.

This is a repository-safe build-integrity contract. It does not execute licensed BricsCAD and must not be reported as `LOCAL_PASS`.

## Failure model

A source reference directory can be updated while CI or a local source-ready build is preparing its isolated snapshot. Per-file hash-before/copy/hash-after checks are insufficient when each source file is released before the next file is admitted: an updater can replace an already-captured DLL between iterations, creating a mixed-generation snapshot whose individual members all look stable.

The snapshot helper therefore treats the required references as one set:

1. validate source/snapshot path and reparse safety before destructive snapshot cleanup;
2. open every required source DLL for read with `FileShare.Read`, which permits readers but denies write/delete/replace;
3. bind canonical path, length, last-write timestamp, and SHA-256 through each held stream;
4. acquire all three locks before copying the first member;
5. copy from those held streams, then recheck each held source and destination bytes;
6. rebind the entire source set while every source lock remains held;
7. publish `reference-state.json` only after the whole-set check succeeds;
8. if set capture fails, remove the state manifest before releasing the locks.

Downstream `build-v25-with-stable-references.ps1` continues to lock and verify the isolated snapshot throughout `dotnet build`. This runbook complements, rather than replaces, the pinned-MSI acquisition-generation contract.

## Deterministic guard

Run:

```text
python scripts/preflight-v25-compile-reference-set-generation.py
```

The guard verifies that all-source admission precedes the first copy, copying uses the held streams, whole-set rebinding and state publication happen before lock disposal, failed capture removes the manifest, and the former sequential unlocked source-copy pattern is absent. The guard includes destructive mutation probes so protected markers cannot silently stop being enforced.

## Acceptance

For a source change to this boundary:

- focused source guard passes;
- tracked PowerShell syntax validation passes;
- automatic exact-head Shared CI reaches `preflight=SUCCESS` and `core=SUCCESS`;
- after any latest-main reconciliation, fresh exact-head evidence is required;
- the protected PR current candidate must pass `preflight` and `core` before merge.

No hosted/static/managed-reference result establishes licensed BricsCAD runtime acceptance.
