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

### Post-run harness safety regressions

Independent review found two test-harness boundary defects; neither was used to claim UI PASS. Both were reproduced by deterministic tests before correction:

- UI cleanup previously fell back to the active drawing when context binding failed. Replaying the actual cleanup method against in-memory host doubles reproduced an unauthorized `QUIT` attempt. Cleanup now requires a validated unchanged disposable context, exactly one document, an allocation-child drawing and the paused mutation boundary before closing a dialog or queuing application-wide QUIT. Null context, document drift/rename, foreign allocation, extra drawing and unpaused boundary all refuse cleanup; qualified owned cleanup is preserved. The replay loads no CAD assembly and sends no desktop input.
- The raw request-key regex did not detect a JSON-escaped duplicate coordinate. The actual decoder now validates parsed types/values and reconstructs the exact compact, ordered, eight-field ASCII object produced by the probe, rejecting any raw mismatch. Six valid forms pass; fifteen malformed/ambiguous forms, including escaped/plain duplicate keys, fail. Retained actual C# requests still decode without replaying them.
- Follow-up review reproduced culture-sensitive PowerShell equality accepting an added U+00AD in an action schema. Action and phase-marker identities now require printable ASCII and ordinal equality; regular expressions use absolute end anchors. Additional regressions reject literal/escaped soft hyphens, NUL, newline and hidden characters in check names. Canonical requests emitted by the probe remain accepted without input replay.

Both regressions run through `scripts/test-local022-ui-input.ps1`; the cleanup test lives in `tests/QS3D.LocalQualification.V25/test-ui-quit-boundary.ps1`. V25/V26 probe builds remain zero-warning/zero-error. These post-run safety checks are static/in-memory evidence only; the failed licensed UI allocation above is still bound to its original harness SHA. No unchanged-source UI rerun was performed after discovering #4586.

## Authorized Add-route successor, 2026-09-05

The owner authorized takeover/fix of existing source carrier #4586; PR #4594 is reopened, not replaced. Exact pushed product source is `0db6e659510809a6781221204a32409605c851ba`. It includes the original Grid detach correction plus deterministic real-WPF regressions/fixes for Grid before the deferred Room overlay and Family/finish control isolation. WPF tests execute extracted production routing methods with mutation endpoints doubled; they are not native placement evidence.

Matching frozen local qualification packages from that source both build with zero warnings/errors:

- V25 ZIP SHA-256 `6b6d00de4d391e772b58780be96afab9e4b31c0d8e0246dee3d7b79a8c1c5f70`.
- V26 ZIP SHA-256 `4259a2c9850e2e18dd82a8496a8b70c4c68d80abe290c51fb660e1e9d12e946d`.
- Version `0.1.0-preview.10307`, explicitly `LOCAL_PR_CANDIDATE`; the installed official `.10309` package is not replaced.

The orchestrator now pins this successor pair. Old source receipts/failed UI allocations remain unchanged. Require a fresh V25 UI allocation and successful full cleanup before a fresh V26 UI allocation. At this registration, licensed UI acceptance remains pending; MCP is paused and no user-owned BricsCAD session may be closed or driven.

Successor allocation `ui-add-route-v25-15`, harness `e6d3d49f11493782993a808fea35d65710dafd8f`, RunId `6adb65c532234af5a29e767588c60baf`, ran 05:24:52–05:28:04 UTC. Physical Add now opened the six-field dialog; Cancel passed the unchanged-project/native assertions, then the next dialog accepted all six physical numeric inputs and OK. The probe stopped at `ui_acceptcreatedialog / FAMILY_DIMENSION_MISSING`; no placement/save/reopen phase qualified. Final Add routing trace contained only `OnBlt3dRoomAwareAddClick`. Receipt proves full private/profile cleanup, zero BricsCAD processes, protected state unchanged and exact autostart restoration. This is progress past the original route defect, not UI PASS. A fresh diagnostic allocation will record only synthetic Family property keys and six dimension values before the unchanged assertion to distinguish product-state failure from probe-reader mismatch.

Diagnostic allocation `ui-add-route-v25-16`, harness `df0092129d49b3b7bf81b8accda8f812706ab138`, RunId `35cf960c1b974eeeaa1872a2b7bd8231`, ran 05:30:10–05:40:23 UTC, same source and failed assertion. The synthetic Family had L1/W1=`2`, L2/W2/H1=`true`, H2=`false`. Generic Workspace property initialization had coerced numeric 0/1 to boolean strings. Full cleanup/restoration is verified; no qualified phase.

Source #4586 corrected that numeric-key classification at exact pushed `0fc1ced48a089267246e78fe4ceeadc36cd5a2e7`; production normalization regression and prior WPF routing matrix pass. Both matching host builds are zero-warning/zero-error. Fresh frozen hashes: V25 `0d2032d4be962ab3b321abf1292bd9fd67e59ae09172cef674421bed430c2f05`; V26 `5fa8eeb26ead7719b2f27f363f22ea4ae85af91addd1c6d7500d8b8e5887c05d`. Same local-candidate version boundary applies. The preceding package pair was moved intact to the source worktree's ignored `artifacts/packages-0db6e6595`; no package evidence was overwritten. The runner now pins the corrected pair for fresh V25-first execution.

Allocation `ui-numeric-v25-17`, harness `cabd6b521a8958052217d856ef0ee751123790d9`, RunId `f69bbb35e4e64c29a9e537dc1ee39fd9`, confirmed the corrected product retains all six numeric values `2,2,1,1,1,0` after actual Add/Cancel/input/OK and passes Family cardinality, active identity and dimensions. Physical Draw and the first viewport click were sent, but the probe timed out at `ui_firstcentre / UI_TIMEOUT_FIRSTCENTRE`; no complete phase qualified. The next diagnostic harness records only synthetic placement counts/expected-versus-actual centres before the unchanged geometry assertion. Both native loops now validate an atomically published UI marker before sending further input, routing FAIL immediately to the existing owned-process cleanup instead of waiting for the overall timeout. Deterministic replay proves failure stops before input and PASS preserves the host-exit wait; no assertion is relaxed. Final receipt now verifies private/profile cleanup, protected state unchanged, zero BricsCAD processes and exact autostart restoration.

Allocations `ui-centre-diag-v25-18` and `ui-centre-diag-v25-19`, both harness `54f29086a6ec64a9ec632c81c5cc5c1a4f2a4cc6`, remain consumed `FAIL_OR_NO_RESULT`, zero verified phases. Allocation 18 (RunId `7dc85606a65c448c9a51d3eba7247ade`, 05:59:25–06:00:03 UTC) refused the second tree action outside the owned host following a layout change, before clicking. Allocation 19 (RunId `8e5b0d428cb54ded90892f673ddcd5e8`, 06:01:00–06:02:17 UTC) passed Add/Cancel/numeric Family checks and created one actual footing after physical click. Its count changed 0→1, but the runner mapped desktop pixels directly through the drawing-view conversion: expected centre `(90.4172388153421,-6.81323209341629,0)`, actual centre `(11.8147736224404,19.5094539246717,0)`. Later view changes moved the recomputed expected point while the actual centre stayed fixed. This is runner-coordinate evidence, not a product placement failure or a UI PASS. Both receipts verify full cleanup/restoration and no MCP execution.

The successor latches the independently mapped target immediately before publishing the click, after hover acknowledgement. Extracted-method regression proves capture timing, no recomputation after view drift, missing-target refusal and reset for the next placement. Geometry identity/volume/extents tolerances are unchanged. Native coordinate-frame qualification remains required before claiming physical placement PASS.

Coordinate conversion now uses `bricscadapi.dll::sds_getviewhwnd`, declared as `HWND sds_getviewhwnd(void)` in both installed V25/V26 `API/bricscad/sds_protos.h:83`. Independent binary review confirms each host's `adsw_acadDocWnd` delegates to this same API; `acedGetAcadDwgView` instead returns a CView pointer and `Document.Window` is a parent frame. The helper requires the active exact document, current-process HWND ownership, a valid client rectangle, successful ScreenToClient, inside-client coordinates and the existing pixel/world round trip. Extracted-method tests cover nonzero desktop origin, document/PID/HWND/API failures and all four out-of-view edges. These mocked API tests do not substitute for actual host placement agreement.

GitHub readback on 2026-09-05 confirms source PR #4594 was merged by `trinhtanphat` at 06:01:34 UTC, merge commit `c41837ccca9594ffffe4991999912f235a9e1cd3`, and issue #4586 is closed. Refreshed `origin/main@256452879` contains tested source `0fc1ced48a089267246e78fe4ceeadc36cd5a2e7`. This supersedes the historical ownership/source blocker above; it does not convert the pending physical UI matrix into PASS. #5718 continues only its existing standalone qualification harness/evidence carrier.

Allocation `ui-client-centre-v25-20`, harness `5a57aa456bbcb0478c89bd18e4beca1ea74bef8b`, on the same source now independently matched both physical centres exactly: `(11.8147736224404,19.5094539246717,0)` and `(30.2162809776779,19.5094539246717,0)`. Both native solid checks passed and Enter ended the draw command. The next stage failed `ui_edith2 / UI_PROPERTY_EDITOR_IDENTITY`: the visible panel was showing the last-created element's Instance properties, not the specialized H2/mm Family form. Receipt remains `FAIL_OR_NO_RESULT`, zero complete phases; full cleanup and exact autostart restoration passed.

The next UI flow explicitly clicks the Family/Type scope and reselects Foundation→Móng đơn in the real tree to enter the specialized Family form, then scrolls the exact editable H2/mm row into view before physical input. No selector, model setter or production handler is invoked to manufacture selection/edit. This qualifies only the explicit tree-route Family edit; it does **not** establish that switching the generic scope combo alone preserves specialized dimension editors/regeneration. That separate source-path concern remains under investigation. Per-row name/unit/editability and all prior state/geometry assertions remain required.

Allocation `ui-family-edit-v25-21`, harness `60a531f80`, RunId `5d2d0ff143ab4e409b9a462830db0b30`, ran 06:26:34–06:28:14 UTC and repeated both verified centres/Enter. The actual scope ComboBox opened; then the physical-hover consumer unnecessarily reactivated the already-foreground host, dismissing its WPF popup before the click could be published. It failed closed at `ui_selectfamilyscope / UI_ELEMENT_NOT_CLICKABLE`, zero complete phases. Cleanup/restoration passed. The input consumer now preserves existing same-process foreground and only activates when needed; it still requires foreground and point ownership at every input boundary. Actual activation-branch replay confirms zero reactivation for owned focus, guarded activation for foreign focus and refusal when activation fails. No popup/selection is changed programmatically.

The separately identified generic Family-scope source gap is registered as #5829, source-only carrier `agent/local022-01a06ce3-20260905/issue-5829-footing-scope-regeneration`. #5718 retains only test/evidence ownership. A later scope-only UI path must qualify the exact fixed source before broad scope-switching completion is claimed.

Allocation `ui-family-focus-v25-22`, harness `2453bd7bfd3945f7b2234c55a23c0ee2c860d4ac`, RunId `00d24ff8e8dc46409362a530faeceb6a`, ran 06:31:06–06:32:20 UTC. Popup-preserving input now physically selected Family/Type. The following parent-tree workaround selector failed with `ui_selectfoundationparent / UNEXPECTED_INVALIDOPERATIONEXCEPTION`; complete phase count remains zero, cleanup/restoration passed. A later owned frame still showed Instance scope, so stable scope is not yet established. The successor removes the workaround stages entirely: it requires consecutive observed Family scope, exact active Family, direct H2/mm editor and native regeneration after only the scope-combo choice. Scope/row diagnostics are private synthetic data. This stronger direct-path acceptance must fail on missing/reverted specialized state, not silently re-enter another route.

Allocation `ui-scope-direct-v25-23`, harness `de9009870c32fb38f9b2bec7e38d13fa43605649`, RunId `3118efc5e766487cba9148e129d9fc1b`, ran 06:35:28–06:36:19 UTC. A deferred workspace layout moved the initial tree label from screen Y=605 to Y=822 after hover; the unchanged physical-input boundary refused the outside-host click. No scope-path evidence was produced; cleanup/restoration passed. The probe now detects an out-of-workspace tree label before publishing a click, finishes any pending hover acknowledgement, and repeats only reveal/hover preparation within the existing stage deadline. It never clicks a stale out-of-view row or changes tree selection directly.

Allocation `ui-scope-direct-v25-24`, harness `3bbe00fbdca926367955e1b247c7b4cea6c68cc4`, RunId `39f0b257efe94b708b772ef0c116e821`, started 06:38:48 UTC and failed `ui_selecttree / UI_TIMEOUT_SELECTTREE`, zero qualified phases. Its owned-window frame showed a restored small host after the launcher requested Maximized; tree hit/layout had changed. Cleanup and autostart restoration passed. The successor explicitly prepares the exact owned main HWND once before sequence 1, verifies maximized state and a settling interval, and checks WPF nearest-row hit identity before publishing tree clicks. It never resizes after input begins. Extracted-method regressions cover one-shot sequencing, HWND/PID identity, settling, and clipped/overlapping/wrong tree-row hits.

### Frozen scope-regeneration successor

Source #5829 / PR #5834 is pushed at `f0146aacef0b398bc71e8e278e6f7675432c1f17`. The Family presenter now retains six validated mm editors/native regeneration when switching scope and after ViewModel replacement. Its executable regression was RED on the preceding generic route and GREEN after correction, including malformed/stale/duplicate/suppressed-context refusal. Both exact-source host builds completed with zero warnings/errors; this is not licensed PASS.

Fresh matching packages: V25 ZIP SHA-256 `ee1f820baaa93d1b5e636432e317ca76c632b856480450467449af249c2636c0`; V26 `4410e6b00caf15c99f7a974254debaffa255987e566f4ca54d1717e5355291a8`. ProductVersion remains committed `0.1.0-preview.10307`, explicitly LOCAL_PR_CANDIDATE, not official release. Prior frozen packages remain intact in their prior worktrees. The runner pins this new pair for direct-scope UI V25-first qualification, then V26 only after the same-source V25 UI result and cleanup pass. Installed `.10309` and the MCP pause remain unchanged.

First successor allocation `ui-scope-fixed-v25-25`, harness `29721efc5`, RunId `d844597c8ff7455c9074e05cd9bb8caf`, selected the actual Móng đơn tree row in the maximized owned host, but Add did not open the dialog (`ui_opencanceldialog / UI_TIMEOUT_OPENCANCELDIALOG`). Workspace Zone/Floor controls were blank, although the probe obtained the disposable fixture project; only the Room-aware Add handler was observed. Zero complete phases; final private/profile/protected-state cleanup and exact autostart restoration passed. The next diagnostic records synthetic Workspace status/loading/subtype and whether the actual handler target is the observed panel. This failure is not yet attributed to the source fix, and no new source change is made without its cause.

Allocation `ui-scope-status-v25-26`, harness `ca60bea94454547f52750845f0f2b75c29346020`, RunId `3e8ebd56ed9b40ad8cb557f757c3985a`, ran 07:05:48–07:07:34 UTC on the same `f0146aace` packages. Add/Cancel, six numeric fields, both independently verified physical centres/native solids and Enter passed again. Actual Family/Type selection remained selected through consecutive observations and displayed the specialized six mm rows directly, without tree reselection. It then failed `ui_edith2 / UI_TIMEOUT_EDITH2`: the requested H2 editor centre at screen `(377,666)` was below the visible property pane. The owned screenshot showed H1 above the clipped lower edge; H2 input did not reach the intended editor. Zero complete phases; all receipt cleanup/protected-state checks and exact autostart restoration passed. The same unchanged source opening both dialogs in this allocation disproves a deterministic Add-route regression inferred only from allocation 25; its intermittent initial state remains unqualified.

The successor scrolls the property list to its final rows after locating H2, then requires WPF hit ancestry to identify that exact TextBox before publishing physical hover/text input. No model state, selection, production handler or geometry assertion is changed. A RED→GREEN extracted-method regression rejects clipped/missing/different editor hits and accepts only the exact editor; existing input/cleanup/native-boundary regressions pass. This is runner safety evidence, not licensed H2 edit PASS.

GitHub readback confirms #5834 was merged externally by `longnguyentuan2107-maker` at 07:13:39 UTC, merge `39d8167a4531e0b0ec704cfca2b413c3676acf4a`, and #5829 is closed. Protected preflight/core succeeded on merged PR head `94ec7f7bba49131fd961d2d15232230c13009ec5`. The frozen local runtime candidate remains exact `f0146aacef0b398bc71e8e278e6f7675432c1f17`; this merge does not establish remaining edit/save/cold-reopen UI acceptance.

Allocation `ui-h2-visible-v25-27`, harness `77ff2b240`, RunId `f32389a020ab43e59a4cdb5709a8f7d0`, ran 07:16:33–07:18:10 UTC. Earlier UI/geometry/scope assertions repeated successfully, but H2 remained non-hit-testable after ScrollToBottom. The new guard withheld all editor input (last sequence 32), then the unchanged stage deadline produced `UI_TIMEOUT_EDITH2`. Zero complete phases; receipt and autostart restoration verify full cleanup. The next diagnostic records read-only editor/ancestor bounds and scroll viewport/extent/offset on changes, distinguishing clipped layout from failed scroll alignment before further behavior changes.

Allocation `ui-h2-layout-v25-28`, harness `c6805259b`, RunId `ecb6f288a139471b843959bc586e228c`, ran 07:49:29–07:51:38 UTC on unchanged `f0146aace`. It confirmed the product's embedded list is 120px high while its parent DockPanel is179px and top controls consume100px, leaving79px. Its internal scroll viewport is118px, extending beyond the allocated visible remainder. The attempted runner ScrollToBottom also overshoots H2 above that viewport (editor top514, scroll top571, offset/max478). The exact-editor guard withheld input, preserving FAIL/zero complete phases; cleanup and exact autostart restoration passed. Source correction is registered as #5840, distinct from merged scope-presenter #5829. The runner removes ScrollToBottom and retains normal H2 ScrollIntoView plus exact hit identity for the corrected UI. No property-search workaround or weakened geometry assertion is introduced.

### Frozen embedded-viewport successor

Source #5840 / PR #5841 is pushed at `87aff7fec452f9a8dd9f641ef84d143edc73514d`. The final embedded layout releases only the inner PropertyList minimum; the whole pane120px minimum,56*/44* proportions and host clipping remain. A real STA WPF fixture executing the full extracted production layout was RED at list120/available79.6, then GREEN for short/tall/resize/repair/dedicated restoration and H2/final/first editor bounds. Focused source guards and preceding Family-scope regression pass. Both locked-reference host builds before and after commit completed with zero warnings/errors.

Matching frozen local packages: V25 ZIP SHA-256 `6da38fcb3bc5fdb1989e9397fad45da712bf4af6b7690298a2fe83657bcb10ac`; V26 `59498948341f36d408f8bf838e177170c19d99ffc64acaa2070b0524e6b99a81`. Version remains `0.1.0-preview.10307`, explicitly unsigned LOCAL_PR_CANDIDATE, not official release. Prior packages/receipts remain intact; installed `.10309` is unchanged. The runner pins this successor for fresh V25-first physical qualification and requires successful V25 UI/cleanup before V26. Licensed H2/edit/save/cold acceptance is not yet claimed.

### First complete V25 physical UI pass

Allocation `ui-property-fit-v25-29`, harness `6976ea8f599d5a3147123ac874b873116a0de8f0`, RunId `53fafd79a2b5456fb9226a43405aa47b`, ran 08:08:58–08:10:55 UTC on exact source `87aff7fec452f9a8dd9f641ef84d143edc73514d`, BricsCAD25.2.10. Final receipt is `LOCAL_PASS_BOUNDED`, all three `ui`/`uisaved`/`uireopen` phases verified. Physical Add/Cancel/six inputs/two independently mapped centres/Enter/direct Family scope/H2=1000mm/native regeneration/former generated handles erased/third physical centre/Esc passed. QS3DSAVE/QSAVE and fresh-process reopen preserve exact Family/element/native-handle identities, dimensions, volume/extents, ownership, cardinality and artifact digest. Actual list79px/viewport77px fit the remaining pane, and H2's physical hit was the exact editor. All39 physical requests have matching receipts. Full private/profile/protected-state cleanup, zero hosts and exact autostart restoration passed. This does not cover the broader topology/DPI/Quantity/private-DWG matrix.

Matching V26 allocation `ui-property-fit-v26-30`, same source/harness, RunId `3b1c2bd4098e4684ab12f0b03091868c`, ran08:11:40–08:13:03 UTC and passed physical Add/Cancel/six dimension input/Family checks. It failed before placement at `ui_acceptcreatedialog / UI_VIEWPORT_MAPPING_UNAVAILABLE`, zero complete phases. The owned V26 screenshot shows native Tips and Properties panels consuming the available drawing area, unlike V25. Full cleanup/protected-state/autostart restoration passed. The successor uses Bricsys-documented `-TOOLPANEL` Tips Hide and `PROPERTIESCLOSE` only in the disposable V26 profile before the UI probe; QS3D panes and viewport ownership/roundtrip checks remain unchanged. Actual startup-array replay was RED before adding the commands and GREEN afterward; it launches no CAD. Reference: https://help.bricsys.com/en-us/document/command-reference/t/-toolpanel-command?version=V26 . A fresh V26 allocation must still independently qualify native centres/edit/save/cold-reopen.

Exact source `87aff7fec` passed all1652 discovered feature preflights locally. Protected branch33954182384 and PR33954225983 both completed preflight/core SUCCESS. These source gates do not convert V26's consumed failure into PASS.

### V26 native-panel allocation and source merge readback

Allocation `ui-native-panels-v26-31`, harness `8242ccbd307393c38abecd585bea411fa0b8737a`, RunId `ea4d2ef85fae4f9abe7cee19c1b1d3f0`, ran 08:16:39–08:18:14 UTC on the unchanged frozen `87aff7fec` V26 package and BricsCAD26.2.07. The owned frame confirms the native Tips/Properties panels were hidden and drawing space became visible. The initial requested tree-label centre was `(94,471)`, but the actual mouse event identified the nearby hint text, not a TreeViewItem. Its workspace-relative event position `(89,237)` differs from the label's reported `(94,263.5)`. This establishes a physical targeting mismatch, not its root cause; self-consistent WPF PointToScreen/PointFromScreen hit checks did not establish the real input hit. No guessed fixed coordinate offset or new product defect is inferred from it.

The unchanged stage deadline ended `ui_selecttree / UI_TIMEOUT_SELECTTREE`, final receipt `FAIL_OR_NO_RESULT`, zero complete phases and only two action/ACK pairs. Full private cleanup, protected-state equality, current profile and complete profile inventory restoration, nonce removal, zero BricsCAD processes and exact original autostart restoration passed. V26 physical UI remains unqualified. MCP execution and runner-issued MCP requests remain false. The V25 allocation29 PASS is unchanged and is not promoted to V26 evidence.

GitHub readback verifies #5841 merged at 08:25:16 UTC, exact PR head `1002e18b6f25e348aa8c097eeed4ed4869aecaab`, merge `34e2609a0fd125fd1bf020c1d4e4787dd75134a7`. Fresh PR run `33954888075` and push run `33954886020` both passed protected preflight/core on that head; the reservation/collision gate passed and #5840 closed. Then-current main `8f86ff3676c0c6cff2697795f444d88836918d5d` contains the merge. The licensed product tested above remains exact `87aff7fec452f9a8dd9f641ef84d143edc73514d`, not this newer source integration. Frozen archives and private raw evidence were not changed or uploaded.

The current Windows automation environment requires the supported Computer Use JavaScript API instead of direct PowerShell UI input. No allocation32 was launched after allocation31; a subsequent UI execution must use a compliant input path while retaining independent product/native assertions and truthful physical receipts. The existing runner's hover requests cannot be acknowledged as performed by a driver that did not actually perform them. This is an execution-path limitation, not a licensed PASS or a request to weaken the harness.

### Observed-click protocol successor, pending licensed qualification

The runner now explicitly selects `NATIVE_V1` or `OBSERVED_CLICK_V2`. V1 retains real hover and V1 receipts. V2 has no hover request/ACK, publishes stage-bound V2 requests and requires explicit supported Computer Use observation/action/refresh by the operator. Its shell loop cannot enter the existing PowerShell physical-input or proxy-dialog paths; the input mode and exact JavaScript receipt-helper hash are frozen in the allocation and mode is recorded in the final receipt. The original native stage deadline is retained; the asynchronous V2 operator has a separate bounded180-second stage/3600-second phase deadline. V2 does not call programmatic maximize/tree/editor scrolling from the probe.

Both modes additionally arm a host-independent `PhysicalPickWitness` with the independently mapped world target before publishing each CAD click. The real Editor `PromptedForPoint` callback must observe that same finite point, exact sequence/document/World UCS/draw state, matching cursor and unchanged pre-placement semantic count. Generated geometry is never used as the expected input. Missing/stale/duplicate/mismatched/post-placement observations fail; event errors are latched outside the production command. Event registration is removed at completion/failure. SDK signatures compile against both actual installed V25/V26 reference sets with zero warnings/errors. Actual runtime callback ordering remains to be qualified, not assumed from compilation.

New host-free tests were RED for missing witness/protocol/driver, then GREEN for actual witness correlation, explicit V1/V2 policy, shell input isolation, strict canonical JSON/stage/value binding and actual filesystem receipt refusal/exclusive publication. Existing cleanup/input/geometry-target guard replays still pass. Temporary receipt fixtures are explicitly nonlicensed and removed by the test. No existing receipt, frozen package or installed plugin was changed. See `tests/QS3D.LocalQualification.V25/OBSERVED-UI-DRIVER.md` for the operator procedure. A new V25-first allocation is required before V26; this implementation is not a new licensed PASS.

### Observed V25 allocation32 — consumed protocol failure, not PASS

Exact pushed harness `c9f3a81e848e6a70d9f1fff78ecfb2ebdd01dd29`, unchanged product `87aff7fec452f9a8dd9f641ef84d143edc73514d`, RunId `23811dd678564bd1b9095d7c5010211f`, host25.2.10, allocation `ui-observed-v25-32` started11:14:41UTC. Supported Computer Use performed real maximize/tree scroll/selection, Add, Cancel/Esc and Add again, with four matching V2 action/ACK pairs. Request5 correctly emitted InputL1=`2000`mm, but the JavaScript allowlist incorrectly expected semantic metres (`2`). The decoder refused; no input or ACK for request5, no complete UI phase, no new native PASS. The earlier manually enumerated serializer matrix also copied the wrong units and was insufficient evidence.

The operator stopped only the exact owned host. A second runner defect then left the exited/no-child/no-marker loop waiting for its full phase deadline; interrupting the shell terminated it without completing finally. Verified recovery used the allocation-bound profile receipt (hash checked) and the existing sandbox restore helper: original pointer/inventory restored, exact owned nonce removed, zero hosts. Original OpenAI autostart bytes/hash and timestamp were restored from the hash-verified backup; Cloudflare stayed paused. Private test fixtures and raw evidence are retained, and a separate operator recovery record explicitly says `ABORTED_NO_RESULT`; the original incomplete runner restoration receipt is not relabelled. No full private cleanup or new licensed PASS is claimed for this interrupted allocation.

The successor decoder now expects actual dialog millimetres `2000,2000,1000,1000,1000,0`. Regression test extracts the actual `FieldOrder`/`FieldText` definitions and verifies their use by InputField before sending actual C# serialized requests through the JS decoder (RED→GREEN); semantic-unit substitutions are rejected. Both host runners now throw into owned cleanup when the owned host has exited with no marker and no exact child, with actual branch replay coverage. Fresh allocation required; do not resume/relabel allocation32.

### Observed V25 allocation33 — operator timeout, cleaned

Exact harness `870a6e9b06dc6bf4e9020b22d36dcb45065d915a`, frozen product `87aff7fec452f9a8dd9f641ef84d143edc73514d`, RunId `f9e2d4f677da488bb1bb89c0a0e19bfe`, host25.2.10, allocation `ui-observed-mm-v25-33`,11:26:51.9515484–11:33:33.4605037UTC: `FAIL_OR_NO_RESULT / UI_TIMEOUT_INPUTW1`, zero complete phases. Actual Select/Add/Esc/Add and L1=2000mm completed with five V2 ACKs; W1 was clicked but not typed/ACKed before the180-second operator deadline. No licensed geometry PASS. Automatic private/profile/protected cleanup and original autostart hash/timestamp restoration all verified, zero hosts. Fresh CI for870a6e9b0: push33963339784 and PR33963342538/33963390866 preflight/coreSUCCESS.

Owner subsequently explicitly approved600-second observed-operator stages after this interrupted multi-call input scenario. The new deadline test executes the actual selected timeout and expiration branch at181/599/600/601seconds (RED at180, then GREEN at600); legacy native25/26-second behavior stays unchanged. Total observed phase remains3600seconds. This changes operator scheduling allowance only, not product performance acceptance, native assertions, input protocol or prior failed receipts. New allocation34 is required before any new PASS claim or V26 run.

### Observed allocations34/35 — incomplete, cleanly restored

Both used exact pushed harness `9f6024674d8fc26b7f8a347fc6a08410e62d2f85`, unchanged frozen product `87aff7fec452f9a8dd9f641ef84d143edc73514d`, V25.2.10 and the approved600-second stage deadline. Allocation34 `ui-observed-tenmin-v25-34`, RunId `685fa29a61f748f1a6fb375e663b8d3c`,12:15:16.7417606–12:37:52.0220457UTC, failed `UI_TIMEOUT_INPUTH2`: nine acknowledged physical steps; H2 was typed but the operator continuation was interrupted before its ACK. The decoder refused further ACKs after the terminal marker. Zero complete phases; no placement evidence for34.

Fresh allocation35 `ui-observed-continuous-v25-35`, RunId `69f6cf214d284c87a72e14cdd09f6196`,12:49:27.3317932–13:05:02.3999192UTC, completed15 physical action/ACK pairs through Enter. Both actual `PromptedForPoint` callbacks matched the independently frozen world targets, cursor positions, request sequences and pre-placement semantic counts; native placement checks advanced. This is the first licensed evidence of the new event witness ordering, not a complete UI PASS. At `OpenFamilyScope` the host exited without a phase marker or exact child. The owner subsequently confirmed closing the test window. No timeout/crash/product defect is inferred from this interrupted allocation, and no ACK16 exists.

Both final receipts remain `FAIL_OR_NO_RESULT`, zero complete phases, with full private/profile/protected-state cleanup, nonce removal, zero hosts and exact original autostart restoration verified. MCP execution remains false. No consumed allocation is replayed or relabelled. Protected exact-head push33965498982 and PR33965500955 succeeded on9f6024674; CI does not qualify remaining runtime acceptance. The successor preserves native exit code and phase in private diagnostics before owned cleanup; an extracted-branch regression was RED without these fields, then GREEN for normal and abnormal exit codes on both hosts. Deadlines, assertions, product packages and installed `.10309` are unchanged. A fresh V25 pass/cleanup is still required before V26.

### Observed allocation36 — creation observed, operator ACK expired

Allocation `ui-observed-owned-v25-36`, RunId `9fff12215de642bd8e69fca65c156912`, exact harness `7134270ddbdfec3aa6c5deac9bfae0012d22f230`, unchanged frozen87aff7fec product, V25.2.10,13:20:23.4039983–13:41:18.8268449UTC: `FAIL_OR_NO_RESULT / UI_TIMEOUT_ACCEPTCREATEDIALOG`, zero complete phases. Ten physical action/ACK pairs covered selection, Add/Cancel and all six numeric inputs. The real OK click closed the dialog and displayed the new Foundation Family at13:31:18UTC, but the operator continuation did not publish ACK11 before the600-second stage deadline. Terminal-marker refusal prevented a late ACK. This does not establish a product creation failure, nor any placement/save/cold PASS for36.

The final receipt and restoration record independently verify all private/protected/profile cleanup, nonce removal, zero hosts and original autostart restoration; cleanup_failure is null and MCP execution is false. Exact7134270dd push33968563514 and PR33968566293 passed preflight/core. Unchanged observed allocations were paused after repeated inter-turn operator scheduling failures. The owner subsequently approved the explicit pause/resume contract below. Existing native/product assertions and consumed receipts remain unchanged; no host was left running while approval was pending.

### Approved operator-pause successor — host-free verification

The explicit `-PauseForOperator` switch is restricted to observed interactive UI and frozen as `PAUSE_FOR_OPERATOR_V1` in allocation/receipt. Actual UI tick replays prove that an unacknowledged action can wait beyond 3600 seconds without advancing the product stage; every tick still validates the drawing/MCP boundary and latched point-witness errors. Exact ACK resumes the remaining 600-second active budget once. Invalid ACK, changed context, witness failure and backward pause clock fail closed. Unpublished preparation and non-paused/native deadlines remain bounded as before. Both outer runners cap operator UI at four hours, shorten save/exit to the original phase timeout once UI PASS is verified, and preserve cold-reopen timing.

Receipt-I/O resume is explicit and requires that frozen policy. Actual filesystem tests reject gaps, future requests, orphan/malformed ACKs, wrong nonce/PID/helper hash, changed acknowledged history and terminal markers. It skips only the exact acknowledged prefix, never injects input or creates an ACK on resume. Unknown completion cannot be inferred from a screenshot. The full procedure is in `tests/QS3D.LocalQualification.V25/OBSERVED-UI-DRIVER.md`.

The new clock/runner/resume regressions were observed RED before implementation and GREEN afterward. `scripts/test-local022-ui-input.ps1` passes, including all 21 actual C# serializer/JS decoder combinations and the existing physical/native guards. V25/net48 and V26/net8.0-windows probe builds both pass with zero warnings/errors against the same frozen product packages. These are host-free results, not a new licensed PASS. A fresh V25 allocation and full cleanup must qualify the new driver/policy before V26. Exact b5a928d0f CI push33972752843 and PR33972756191 preflight/core succeeded before this successor; they do not qualify its new head.
