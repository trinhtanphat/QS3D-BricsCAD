# V25 cloud compile-reference generation binding

Lane-Key: issue-4455

## Boundary

This repository-safe package hardens the BricsCAD V25 managed compile-reference consumption boundary used by Shared CI. It does not execute licensed BricsCAD, does not prove `NETLOAD`, and does not claim `LOCAL_PASS`. The separately dispatched `release-v25-cloud.yml` build boundary is not part of this carrier.

The V25 project still names the canonical `BrxMgd.dll`, `TD_Mgd.dll`, and `TD_MgdBrep.dll` references under `BRICSCAD_V25_DIR`; the project file is deliberately untouched because another active lane owns it. Shared CI instead routes each hosted V25 build through `scripts/build-v25-with-stable-references.ps1`.

## Fail-closed contract

`scripts/snapshot-v25-compile-references.ps1` requires ordinary non-reparse source files and an ordinary non-reparse snapshot path. For each reference it captures streaming SHA-256, byte length, and UTC last-write ticks, resolves and hashes the source a second time, copies the admitted bytes, rebinds the source after copy, and independently hashes the destination. Source drift or snapshot-byte mismatch fails before build consumption.

The helper emits bounded strict-UTF8 JSON state for exactly the three required files. `scripts/assert-v25-compile-reference-state.ps1` binds the JSON materialization itself to a stable file generation, parses only strict UTF-8, validates schema and exact entries, and independently re-resolves/re-hashes every snapshot DLL. Missing/duplicate entries, path drift, reparse transition, size/timestamp/hash drift, malformed JSON, oversized state, or invalid UTF-8 fail closed.

The build wrapper creates the verified private snapshot, then opens all three snapshot DLLs with read access and `FileShare.Read`. Compiler reads remain allowed, while writers and delete/replace operations are denied for the complete child `dotnet build`. State is independently verified after all locks are acquired and again after the child build while the locks are still held. `BRICSCAD_V25_DIR` is rebound only in the wrapper process and restored in `finally`; every held file handle is also disposed in `finally`.

The first attempted design used an MSBuild-time `Reference Update`; exact-head CI proved that this disturbed the WPF reference contract and produced `MC6000` for `WindowsBase`, `PresentationCore`, and `PresentationFramework`. That design was removed rather than weakening WPF/project semantics.

## Validation

Run the auto-discovered `scripts/preflight-v25-compile-reference-contract.py` guard. Shared branch/PR CI must pass `preflight` and `core`; `core` must exercise both the V25 plugin build and the local-qualification build through the locked-reference wrapper.

Hosted compilation proves only repository/source readiness. Any licensed BricsCAD runtime qualification remains under the canonical LOCAL_ONLY process and must be bound to its own exact source/plugin identity.
