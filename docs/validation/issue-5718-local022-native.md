# Issue #5718 — LOCAL-022 V25 bounded native qualification

## Verdict

`LOCAL_PASS_BOUNDED` for the automated BricsCAD V25 portion only.

- Product: official `v0.1.0-preview.10308`
- Product source: `988998bd26c9d0da5915670d9b5adca14b93ecca`
- Published V25 ZIP SHA-256: `8618feb76d523337d9a9ff5900520683a5807050dcd158e27f9b8b3c4bef3771`
- Qualification harness: `6484b2ad7b6cba5d2c58517637f6bb5228b3beaf`
- Runner SHA-256: `b9bc068edf61eaad3145cfe81402a821bc4d74388494bccd0f4673150fdd29c3`
- Probe DLL SHA-256: `80f21352f7a62a216099b147673a1e66ab31e888cd30a3c912f013eb751e6b99`
- Host: BricsCAD V25.2.10, Windows x64, licensed interactive runtime
- Runtime interval: 2026-09-04 18:09:05Z through 18:09:50Z

The runner verified all 17 files named by the package manifest against the immutable published ZIP before launch. It rebuilt the net48 probe with .NET SDK 8.0.424 and reported zero warnings and zero errors.

## Qualified cells

The fresh allocation produced three independent PASS markers:

1. `run`
   - exact disposable drawing and exact published product location;
   - V25 host and metre drawing units;
   - `H2=0` box placement;
   - `H2>0` tapered placement at repeated centres;
   - live `Solid3d` mass properties, volume and extents;
   - source footprint and QS3D ownership marker;
   - Family edit/regeneration from the original two instances to the expected replacement cardinality;
   - every former generated handle erased;
   - generic `Foundation` control rejected before mutation;
   - exact semantic/native cardinality.
2. `saved`
   - production `QS3DSAVE` sidecar persistence;
   - native `QSAVE` with the database still open;
   - exact saved semantic/native state and cardinality.
3. `reopen`
   - a separate fresh BricsCAD process;
   - cold canonical project binding;
   - semantic identity continuity;
   - live reopened generated solids;
   - exact dimensions, volumes, extents and cardinality.

Cleanup also passed: zero BricsCAD/tunnel processes, the nonce profile was removed, the original profile inventory and current-profile pointer were restored, the private disposable root was removed, the repository fixture retained SHA-256 `cec1350fb2207542aeecd96a790a198a6c9cc9e99a9f875871f367554b3d967e`, and protected DemandLoad/tunnel state was unchanged.

## Consumed non-result retained

The earlier `.10304` allocation remains `FAIL_OR_NO_RESULT`. Its native `run` and `saved` markers passed, but a short BricsCAD process-exit race caused the runner to throw before returning those phases; the cold-reopen phase did not run. That allocation was not reused or relabelled.

After the host naturally reached zero, recovery removed only its runner nonce and private disposable data, restored the known source profile `QS3D-V25-TEST`, preserved the sanitized markers, and reverified the unchanged repository fixture and stopped tunnel processes. The runner was then hardened with a bounded host-exit wait and durable, atomic, hash-validated profile recovery evidence before the successful fresh `.10308` allocation.

## Explicitly not qualified

This result does not close aggregate LOCAL-022:

- BricsCAD V26 on the same product source is not run by this V25 harness;
- visible Workspace `Móng → Móng đơn → Add`, physical mouse picks, Enter/Esc, dialog cancel and visual six-field layout are not exercised;
- Quantity UI, Unicode/DPI appearance and unrelated Foundation families are not exercised;
- no customer/private DWG is used;
- no MCP request or MCP tool test is issued. The frozen product starts its embedded loopback server as part of `NETLOAD`; the probe immediately pauses and verifies the MCP CAD-mutation and desktop-control boundaries, while both external tunnels remain stopped.

Accordingly, issue #4034 and LOCAL-022 remain open for the V26 and interactive/UI cells.
