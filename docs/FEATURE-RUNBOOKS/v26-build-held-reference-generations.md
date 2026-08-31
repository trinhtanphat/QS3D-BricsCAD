# V26 held-reference build generation contract

## Boundary

The V26 self-hosted workflows consume `BrxMgd.dll`, `TD_Mgd.dll`, and `TD_MgdBrep.dll` from the configured licensed BricsCAD V26 host directory. Issue #4445 established fail-closed path/reparse/major-version checks plus a bounded state manifest that binds the admitted host files by canonical path, length, UTC last-write ticks and SHA-256.

A state verification immediately before `dotnet build` is necessary but is not sufficient to make the compile interval atomic: after the verifier exits, a host update can replace a managed reference before MSBuild opens it. Different references may then come from different generations. #4535 closes only that build-consumption window; it does not change licensed runtime, release, signing or publication acceptance.

## Required build sequence

`scripts/build-v26-with-stable-references.ps1` must:

1. verify the existing #4445 state manifest;
2. open all three managed reference DLLs for read using `FileShare.Read`, denying write/delete/replace while the build owns the handles;
3. verify the state again after all locks are held;
4. bind process-local `BRICSCAD_V26_DIR` to the canonical admitted directory and invoke the exact V26 `dotnet build`;
5. capture `$LASTEXITCODE` immediately after the native dotnet process and fail on a non-zero code;
6. verify the state once more while every reference lock remains held;
7. restore the previous process environment and dispose the locks in `finally`.

Both `.github/workflows/bricscad-v26.yml` and `.github/workflows/release-v26.yml` must route their V26 plugin build through this wrapper. Runtime consumers continue to use the #4445 verifier at their own boundaries.

## Deterministic source guard

Run:

```text
python scripts/preflight-v26-build-held-reference-generations.py
```

The guard rejects a naked V26 plugin `dotnet build` in either manual workflow and pins the wrapper ordering `verify -> locks -> verify -> build -> native exit capture -> verify -> dispose`. Mutation probes protect the critical verification/lock/build/exit/disposal markers.

## Evidence boundary

Hosted Shared CI can validate the source contracts, PowerShell syntax and V25 compilation used by the repository's standard protected path. It cannot execute the licensed V26 workflow, establish V26 runtime PASS, sign a production release, or publish a V26 release. Those remain separately authorized exact-SHA local/self-hosted boundaries.
