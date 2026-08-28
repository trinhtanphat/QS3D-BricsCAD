# V25 cloud compile-reference generation binding

Lane-Key: issue-4455

## Boundary

This repository-safe package hardens the BricsCAD V25 managed compile-reference consumption boundary. It does not execute licensed BricsCAD, does not prove `NETLOAD`, and does not claim `LOCAL_PASS`.

The V25 project still names the canonical `BrxMgd.dll`, `TD_Mgd.dll`, and `TD_MgdBrep.dll` references under `BRICSCAD_V25_DIR`. Immediately before MSBuild resolves those references, the repository build target re-admits the three source files, copies one stable generation of each into a per-build intermediate snapshot, independently verifies the published snapshot state, then rewrites only the in-memory V25 `Reference` HintPaths to the snapshot.

## Fail-closed contract

`scripts/snapshot-v25-compile-references.ps1` requires ordinary non-reparse source files and an ordinary non-reparse snapshot path. For each reference it captures streaming SHA-256, byte length, and UTC last-write ticks, resolves and hashes the source a second time, copies the admitted bytes, rebinds the source after copy, and independently hashes the destination. Source drift or snapshot-byte mismatch fails before reference resolution.

The helper emits a bounded strict-UTF8 JSON state for exactly the three required files. `scripts/assert-v25-compile-reference-state.ps1` reparses that bounded state and independently re-resolves/re-hashes every snapshot file before MSBuild consumes it. Missing/duplicate entries, path drift, reparse transition, size/timestamp/hash drift, malformed JSON, oversized state, or invalid UTF-8 fail closed.

`Directory.Build.targets` scopes this behavior to `QS3D.BricsCAD.V25`, excludes design-time builds, executes before `ResolveAssemblyReferences`, and leaves other projects unchanged. The existing project file is deliberately untouched because another active lane owns that path.

## Validation

Run the auto-discovered `scripts/preflight-v25-compile-reference-contract.py` guard. Shared branch/PR CI must pass `preflight` and `core`, including the ordinary V25 managed-reference build, before this lane can merge.

Hosted compilation proves only repository/source readiness. Any licensed BricsCAD runtime qualification remains under the canonical LOCAL_ONLY process and must be bound to its own exact source/plugin identity.
