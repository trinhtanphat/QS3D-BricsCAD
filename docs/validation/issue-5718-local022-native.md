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

## Same-source V25/V26 successor candidate

The V26 build at official `.10308` source `988998bd26c9d0da5915670d9b5adca14b93ecca` failed with `CS0246` because the shared Update Center exposed the V25-only `UpdateDownloadProgress` type. The submodule had first been restored to its exact gitlink `fcf24893aac7fabe11017bbd5ed0072f5becd87d`; this was then a product-source compile failure, before any V26 launch.

Issue #5740 / PR #5770 carries the separate source correction at pushed candidate `43130a49f49676299b865f094a9a6ded482f67ad`. It also resolves the exposed V26 `CS0649` download-state warning and two scoped `SYSLIB0014` compatibility warnings. V25 and V26 Release builds passed with zero warnings/errors. The V26 build additionally passed the held-host-reference-generation wrapper.

The next LOCAL-022 pair uses packages generated from that same candidate, with V25 first and full cleanup before V26. Their committed product version is `0.1.0-preview.10307`; they are explicitly `LOCAL_PR_CANDIDATE`, not the official release of that version. The V25 candidate ZIP hash is `4d9869e38682674772196a3e238f115624ff357a276bb0b976000b63c9a833b5`. The prior official `.10308` runtime evidence remains unchanged above. Successor runtime results are pending until fresh allocation receipts exist.

The V26 candidate ZIP hash is `7dbf9216e873f2e20c2fae5011785148e9feded944a7b43233b4710b331fd2c5`. A separate provenance file binds this archive to source `43130a49f49676299b865f094a9a6ded482f67ad`. The V26 runner checks that provenance, every extracted ZIP member, the installed .NET 8 Windows Desktop runtime, the V26 host identity, and the four-file probe output. It uses a separate V26 nonce profile and marker schema, preserves an absent or OnCommand-only registration, and restores exact profile state after both native processes.

The frozen source passed Core smoke (`ALL PASS`) and all 1611 discovered feature preflights. The V26 probe rebuild also passed with zero warnings/errors. These are source/build results, not V26 runtime qualification. PR #5770 has since merged as `3fb8cf086` and its issue is closed; the locally packaged source and hashes above remain unchanged and must not be relabelled as a newer main build.

## Same-source native execution, 2026-09-05

Harness `e32fba7b808bbe2a286c5c8b625e061de48d8ea0` ran the frozen same-source pair after the owner explicitly approved temporarily pausing OpenAI tunnel autostart. V25 ran first and cleaned up before V26 started.

- V25: `LOCAL_PASS_BOUNDED`, three verified phases, 01:03:59Z–01:05:16Z. Profile/current pointer, reference fixture and protected settings were preserved; the disposable root was removed.
- V26: `FAIL_OR_NO_RESULT`, zero verified phases, 01:05:28Z–01:06:17Z. Both native `run` and `saved` markers reported an unexpected exception; cold reopen did not run. The diagnostic token was lowercase while the validator requires uppercase, which obscured the original exception as a sanitization failure. This is not a PASS and the allocation is consumed.
- V26 cleanup passed: zero hosts, exact profile inventory/current pointer restored, nonce/private drawing removed, protected settings unchanged. The outer pair runner restored the exact original OpenAI autostart byte and timestamp; Cloudflare autostart remained unchanged.

The successor harness classifies unexpected exception types, distinguishes context binding from phase execution, and retains bounded V26 exception type/HResult/method metadata without messages, arguments or paths. No production source was changed to diagnose the native failure.

The existing geometry cells prove volume and bounds, not complete BREP topology or every taper section. Cold semantic identity is bounded to project/family/dimensions/centres and counts, not a frozen element-ID/native-handle inventory. These limitations must not be represented as full LOCAL-022 acceptance.
