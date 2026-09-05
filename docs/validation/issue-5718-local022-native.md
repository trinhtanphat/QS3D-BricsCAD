# Issue #5718 — LOCAL-022 V25/V26 bounded native qualification

## Verdict

`LOCAL_PASS_BOUNDED` for the automated BricsCAD V25 and V26 portions on the same frozen product source `43130a49f49676299b865f094a9a6ded482f67ad`. Each host passed run, saved and fresh-process reopen. Aggregate LOCAL-022 remains open for interactive/UI and the additional coverage limits recorded below.

## Earlier official V25 release result

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

Cleanup also passed: zero BricsCAD/tunnel processes, the nonce profile was removed, the original profile inventory and current-profile pointer were restored, the private disposable root was removed, the repository fixture retained SHA-256 `cec1350fb2207542aeecd96a790a198a6c9cc9e99a9f875871f367554b3d967e`, and the checked DemandLoad fields (`LOADER`, loader-file hash and `LOADCTRLS`) and tunnel-autostart bytes were unchanged. The V25 snapshot did not cover every registry value in the DemandLoad key; no full-key preservation claim is made for those historical V25 allocations.

## Consumed non-result retained

The earlier `.10304` allocation remains `FAIL_OR_NO_RESULT`. Its native `run` and `saved` markers passed, but a short BricsCAD process-exit race caused the runner to throw before returning those phases; the cold-reopen phase did not run. That allocation was not reused or relabelled.

After the host naturally reached zero, recovery removed only its runner nonce and private disposable data, restored the known source profile `QS3D-V25-TEST`, preserved the sanitized markers, and reverified the unchanged repository fixture and stopped tunnel processes. The runner was then hardened with a bounded host-exit wait and durable, atomic, hash-validated profile recovery evidence before the successful fresh `.10308` allocation.

## Explicitly not qualified

This result does not close aggregate LOCAL-022:

- the earlier V25 harness does not itself qualify V26; the separate same-source V26 result is recorded below;
- visible Workspace `Móng → Móng đơn → Add`, physical mouse picks, Enter/Esc, dialog cancel and visual six-field layout are not exercised;
- Quantity UI, Unicode/DPI appearance and unrelated Foundation families are not exercised;
- no customer/private DWG is used;
- no MCP request or MCP tool test is issued. The frozen product starts its embedded loopback server as part of `NETLOAD`; the probe immediately pauses and verifies the MCP CAD-mutation and desktop-control boundaries, while both external tunnels remain stopped.

Issue #4034 and LOCAL-022 remain open for the interactive/UI cells and the additional coverage limits below; the same-source V26 automated cells now have independent evidence.

## Same-source V25/V26 successor candidate

The V26 build at official `.10308` source `988998bd26c9d0da5915670d9b5adca14b93ecca` failed with `CS0246` because the shared Update Center exposed the V25-only `UpdateDownloadProgress` type. The submodule had first been restored to its exact gitlink `fcf24893aac7fabe11017bbd5ed0072f5becd87d`; this was then a product-source compile failure, before any V26 launch.

Issue #5740 / PR #5770 carries the separate source correction at pushed candidate `43130a49f49676299b865f094a9a6ded482f67ad`. It also resolves the exposed V26 `CS0649` download-state warning and two scoped `SYSLIB0014` compatibility warnings. V25 and V26 Release builds passed with zero warnings/errors. The V26 build additionally passed the held-host-reference-generation wrapper.

The LOCAL-022 pair uses packages generated from that same candidate, with V25 first and full cleanup before V26. Their committed product version is `0.1.0-preview.10307`; they are explicitly `LOCAL_PR_CANDIDATE`, not the official release of that version. The V25 candidate ZIP hash is `4d9869e38682674772196a3e238f115624ff357a276bb0b976000b63c9a833b5`. The prior official `.10308` runtime evidence remains unchanged above. The successor results are recorded below.

The V26 candidate ZIP hash is `7dbf9216e873f2e20c2fae5011785148e9feded944a7b43233b4710b331fd2c5`. A separate provenance file binds this archive to source `43130a49f49676299b865f094a9a6ded482f67ad`. The V26 runner checks that provenance, every extracted ZIP member, the installed .NET 8 Windows Desktop runtime, the V26 host identity, and the four-file probe output. It uses a separate V26 nonce profile and marker schema, preserves an absent or OnCommand-only registration, and restores exact profile state after both native processes.

The frozen source passed Core smoke (`ALL PASS`) and all 1611 discovered feature preflights. The V26 probe rebuild also passed with zero warnings/errors. These are source/build results, not V26 runtime qualification. PR #5770 has since merged as `3fb8cf086` and its issue is closed; the locally packaged source and hashes above remain unchanged and must not be relabelled as a newer main build.

## Same-source native execution, 2026-09-05

Harness `e32fba7b808bbe2a286c5c8b625e061de48d8ea0` ran the frozen same-source pair after the owner explicitly approved temporarily pausing OpenAI tunnel autostart. V25 ran first and cleaned up before V26 started.

- V25: `LOCAL_PASS_BOUNDED`, three verified phases, 01:03:59Z–01:05:16Z. Profile/current pointer, reference fixture and protected settings were preserved; the disposable root was removed.
- V26: `FAIL_OR_NO_RESULT`, zero verified phases, 01:05:28Z–01:06:17Z. Both native `run` and `saved` markers reported an unexpected exception; cold reopen did not run. The diagnostic token was lowercase while the validator requires uppercase, which obscured the original exception as a sanitization failure. This is not a PASS and the allocation is consumed.
- V26 cleanup passed: zero hosts, exact profile inventory/current pointer restored, nonce/private drawing removed, protected settings unchanged. The outer pair runner restored the exact original OpenAI autostart byte and timestamp; Cloudflare autostart remained unchanged.

The successor harness classifies unexpected exception types, distinguishes context binding from phase execution, and retains bounded V26 exception type/HResult/method metadata without messages, arguments or paths. No production source was changed to diagnose the native failure.

The existing geometry cells prove volume and bounds, not complete BREP topology or every taper section. Cold semantic identity is bounded to project/family/dimensions/centres and counts, not a frozen element-ID/native-handle inventory. These limitations must not be represented as full LOCAL-022 acceptance.

## V26 diagnosis and successful fresh allocation

Harness `c91fa60b2655ac9279d0348acf264ad4f2716ca4` produced a fresh consumed V26 `FAIL_OR_NO_RESULT`: context binding passed, but `RunPhase` raised `FileNotFoundException` / HResult `80070002` before executing the matrix. The sanitized stage was `run_execute`. Cleanup and exact autostart restoration passed. This failed allocation was not reused.

The V26 probe was corrected to share only the exact already-loaded, path-verified Core/product assemblies with its own .NET 8 `AssemblyLoadContext`, before phase JIT. The resolver checks the requesting context and complete assembly identity; it does not search disk, reload assemblies, copy dependencies or alter the product package. This resolved the separate-probe assembly-loading failure.

A fresh allocation on harness `628a920b5b943ea1273d3b7132eb758e6f38b709` then returned `LOCAL_PASS_BOUNDED` on licensed BricsCAD V26.2.07, 2026-09-05 01:13:58Z–01:14:42Z. All three exact-schema phase markers passed with every required Boolean check true. V25 and V26 therefore independently qualify the same bounded matrix on product source `43130a49f49676299b865f094a9a6ded482f67ad`.

| Host | Successful harness SHA | Runner SHA-256 | Probe DLL SHA-256 |
| --- | --- | --- | --- |
| V25.2.10 | `e32fba7b808bbe2a286c5c8b625e061de48d8ea0` | `e3e4b77ae6d239fb2cfbfddff4f85d5c57d4b12c13124bcc3464c3be36492d58` | `f5111a773159e4dab4f88f68d8652c3d56f9c29b6e23a858701ed2bc3a6eff01` |
| V26.2.07 | `628a920b5b943ea1273d3b7132eb758e6f38b709` | `11016af66fbc7b9ee0dd3fae2266ff6d3688cfabd9786b8842ca9ee865212adc` | `ce82d507e36ce96b6c98e59982e01d737996ee1cf5d4e271ec63eb66f0855741` |

Final cleanup verification found zero BricsCAD/tunnel processes, both successful private drawing roots and profile recovery files removed, exact per-host profile inventories/current pointers restored, and unchanged reference fixture/product payloads. V25 DemandLoad preservation is limited to the checked fields listed above; V26 additionally fingerprints all value names, kinds and values in its registration key. OpenAI autostart returned to its original byte `1` and original timestamp after each allocation; Cloudflare remained `0`. No MCP request or tunnel functional test was performed. The installed user plugin was not replaced by these local candidate packages.

## Owner-approved interactive extension

The owner approved real UI expansion on 2026-09-05. The existing host runners now have an opt-in `-InteractiveUi` path, separate from the already consumed native-API allocations. The intended matrix is the visible Móng đơn tree/Add route, Cancel/no mutation, six numeric dialog fields, physical viewport centre picks, Enter/Esc termination, UI Family edit/regeneration, save and fresh-process reopen. The probe must assert product state after each action; sending an input event is not a PASS.

`scripts/local022-ui-input.ps1` accepts only nonce/sequence/PID-bound hover/move, click, numeric text and Enter/Esc requests. It checks the exact owned executable, foreground PID and point ownership before native input. Screenshots use only the owned HWND via `PrintWindow`, never the desktop. The capture helper has its own terminating error policy and rechecks captured HWND ownership, independent of the caller's error preference. Action/ack files, images and raw diagnostics are private ignored artifacts, not public evidence.

`scripts/run-local022-ui-qualification.ps1` is the reusable orchestration entry point for the frozen `43130a49...` package pair described above. It requires a clean, pushed `-HarnessSha`, a fresh `-AllocationName`, the local `-PackageRoot`, `-HostMajor 25` or `26`, and explicit `-ConfirmTemporaryAutostartPause`. V26 additionally requires `-V26ProvenancePath` and `-PrecedingV25Receipt` from a same-source UI PASS with verified cleanup/restoration. Each invocation restores the original OpenAI autostart byte/timestamp; it does not test either tunnel.

Guard regression command: `pwsh -File scripts/test-local022-ui-input.ps1`. These deterministic guard tests are not licensed qualification. Interactive runtime results must be recorded separately against the exact successful harness/product identities; none is inferred from adding or compiling this extension. Historical native PASS and consumed failures above remain unchanged.

First UI allocation on harness `b5947224c` (V25, 02:41:55Z–02:42:32Z) is consumed `FAIL_OR_NO_RESULT`, zero verified phases. The physical-input boundary refused its first point as outside/occluded from the owned HWND, before acknowledging input. Code review found the consumer restored the measured window immediately before input, potentially invalidating coordinates from a maximized window; the successor removes that resize and retains exact-HWND diagnostics on rejection. This is a harness-coordinate non-result, not a product UI PASS or feature failure. Host/profile/private-drawing cleanup and exact autostart restoration passed.

## Physical UI result: existing source defect #4586 reproduced

The owner-approved V25 UI extension has **not passed**. Final diagnostic allocation `ui-window-owner-v25-14`, harness `91b7810168d65bcf57ad29cf8248deac4e5d84dc`, ran on licensed BricsCAD `25.2.10` from `2026-09-05T03:10:16.2672506Z` to `2026-09-05T03:11:25.0833995Z`. Product source remains the frozen `43130a49f49676299b865f094a9a6ded482f67ad` local package pair and hashes above; the installed user release was not replaced.

Observed production behavior after acknowledged physical input:

- The real tree selected `Móng đơn`, tag `Foundation.SingleFooting`; `_categoryFilter` was `Foundation` before and after the test baseline bind.
- The actual Family toolbar Add button had two Click handlers, in order: `OnGridAwareFamilyAddModeClick`, then `OnBlt3dRoomAwareAddClick`.
- Mouse-down and routed Click reached that button. The Click was handled, a visible WPF popup contained `Tham số` / `Solid3D`, no `SingleFootingDimensionsDialog` appeared, and the family inventory was unchanged.
- The phase timed out with `UI_TIMEOUT_OPENCANCELDIALOG`; receipt remains `FAIL_OR_NO_RESULT`, **0 verified phases**. This is positive evidence of wrong production Add routing, not a dialog/Cancel/placement PASS.
- Profile inventory/current pointer/nonce/private-drawing cleanup, protected installed-loader/settings checks and exact original OpenAI autostart byte/timestamp restoration passed. No owned BricsCAD process or tunnel remained; no MCP request was issued.

Source inspection and independent read-only review identify the stale Grid handler as the cause. Both final BLT/Room rewires omit removing it, allowing its generic chooser to consume the Click before the SingleFooting route. Those routing files are unchanged between the tested source and refreshed `origin/main@8dc5cc21769385509cf565b907095b9c93971303`.

This is the exact defect already reserved by open issue **#4586**, canonical branch `agent/trinhtanphat-01a046ab/issue-4586-single-footing-add-routing`, another session's active ownership. Its PR #4594 is closed without merge; that does not release the open reservation. No source fix, replacement carrier, PR reopening or takeover was performed by #5718. The next source action requires that carrier's owner or explicit reassignment. Merely deleting the Grid handler is insufficient: the final dispatcher must retain direct Grid creation and Room routing.

Do not run the remaining UI matrix again on this unchanged defective product source. After an authorized exact source correction, prepare a new frozen matching package pair and run a fresh V25 allocation first, then V26 only after V25 UI PASS and full cleanup. V26 physical UI remains **NOT RUN**. Existing V25/V26 native-API bounded PASS receipts are preserved, not promoted or invalidated by this distinct UI routing failure.

### Consumed UI allocations

All allocations below used the same frozen product source, ended `FAIL_OR_NO_RESULT` with zero verified phases, and completed receipt-verified host/profile/private-drawing cleanup. None may be reused or presented as UI PASS.

| Allocation suffix / harness | Observed stop |
| --- | --- |
| `01 / b5947224c` | Input point rejected; superseded capture/resize implementation |
| `02 / 50637424b` | Input point rejected; target outside displayed window |
| `03 / a79ee4a09` | Tree selection timeout |
| `04 / 99e8bd21b` | Foreground ownership rejected |
| `05 / ce17e234a` | Tree selection timeout |
| `06 / bcd60a9b5` | Tree selection timeout; private WPF hit diagnostics added |
| `07 / 2fe9e9c08` | Tree selection timeout; measured target shifted 15 pixels |
| `08 / 5a258d954` | Tree selected; Add selector ambiguous |
| `09 / 01f32e66d` | Add clicked; hosted WPF Application.Current absent |
| `10 / 8784a05d8` | Add dialog timeout |
| `11 / 14f5487b6` | Add dialog timeout; actual button Click observed |
| `12 / 347bbf335` | Tree target moved repeatedly; wrong row hit; no Add-route result |
| `13 / 347bbf335` | Fresh allocation after unstable UI; Add dialog timeout; category preserved |
| `14 / 91b781016` | Exact duplicate-handler / generic-popup source failure above |

The runner corrections preserve physical input and post-action product assertions: maximize before measuring; bring the tree into view; hover then remeasure; select visible row text; bind Add to the Family toolbar; locate hosted WPF window ownership without manufacturing Application.Current. Private diagnostic observers do not invoke the production Add handler or alter its routing. A separate late diagnostic capture from allocation 13 was excluded and deleted without inspection after its foreground check failed under a permissive caller; the successor helper now fails closed regardless of caller error policy, covered by a deterministic refusal test.

Aggregate LOCAL-022 / #4034 remains `IN_PROGRESS / SOURCE_FIX_4586_REQUIRED`. Six-field input, Cancel, physical placement/edit/save/reopen, DPI/Quantity and extended topology acceptance remain unproved by these UI attempts.
