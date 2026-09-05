# LOCAL-022 observed UI driver

`OBSERVED_CLICK_V2` is an explicit alternative input protocol for environments
whose supported Computer Use API has physical clicks/keys/text but no hover-only
operation. It does not acknowledge or claim a hover. `NATIVE_V1` retains the
original hover/remeasure protocol and its original receipt schema.

The owner-authorized runner still prepares one frozen package, disposable DWG,
nonce profile and paused MCP boundary, and owns cleanup. Select the new mode with
`-UiDriver OBSERVED_CLICK_V2` on `scripts/run-local022-ui-qualification.ps1`.
Its PowerShell loop does not activate windows, dismiss dialogs or send physical
input in this mode. The external operator uses only the supported Computer Use
JavaScript APIs, with a new observation before each action and a refresh after it.
The probe does not maximize or scroll WPF controls in this mode; the operator
prepares the visible window/tree/editor through those APIs.

## Per-allocation procedure

1. Freeze/commit/push the harness, and run its host-free guards and both probe
   builds. Use a fresh allocation name. Do not replay a consumed allocation.
2. Start V25 first. Read the allocation's exact RunId, product hash, driver hash
   and owned process PID. Select exactly one returned BricsCAD window from
   `sky.list_windows()` with the correct host executable and disposable drawing;
   independently verify that the runner owns the sole host process. `Window.id`
   is opaque: never treat it as a HWND or PID.
3. Initialize `@oai/sky` in `node_repl` per its current skill. Import
   `local022-observed-input.mjs` in that same session, then call
   `openObservedAllocation(root, runId, ownedPid)`. This module performs receipt
   I/O only; it does not automate any UI or call an MCP endpoint.
4. Call `read()` for the next bounded request. Inspect a fresh screenshot/tree.
   Request coordinates are proposed screen points, not permission to bypass
   observed screenshot coordinates, DPI mapping or target identity. For a CAD
   centre pick, independently confirm the requested point is in the current
   drawing viewport; do not choose a different centre merely to pass.
5. For click requests, perform the actual requested click and refresh. For text,
   click the observed exact editor, refresh and verify focus, issue Ctrl+A,
   refresh/verify focus, type the exact numeric value, and refresh/verify it.
   For key requests, verify the active draw context (or the exact Cancel dialog),
   send the specified Enter or Escape, and refresh. Scroll preparation also uses real observed UI.
6. Only after actual successful actions and refresh, call `acknowledge(request,
   proof)` with `completed/refreshed=true`, the returned `windowApp/windowId`,
   latest screenshot `observationId`, and exact operations `['click']`,
   `['click','selectAll','typeText']`, or `['pressKey']`. This is an operator
   attestation, not self-proving evidence. Never fabricate it after tool failure,
   unknown focus, interrupted input or an unperformed action. The module refuses
   V1/move requests, noncanonical JSON, wrong stage/value/nonce/PID/sequence,
   changed allocation/driver, replay and terminal markers.
7. The probe independently requires actual UI state and native geometry. Before
   each requested CAD click it freezes the screen-to-world target and arms a
   point witness. `Editor.PromptedForPoint` must report the same finite point,
   exact request sequence, unchanged document/World UCS/draw context, matching
   physical cursor and the pre-placement semantic count. Missing/duplicate/moved
   or already-created evidence fails. Callback failures are latched, not thrown
   through the product's `GetPoint` handler. The generated solid never defines
   its own expected centre. Existing geometry, ownership, regeneration, erased
   handles, cardinality and saved/cold identity checks remain mandatory.
8. Stop input on a failure/terminal marker. Re-observe the exact live runner
   handle; let its owned cleanup restore profiles and autostart. Inspect all
   final receipts before any PASS claim. V26 follows only a cleaned V25 pass on
   the same frozen product; qualify the new driver on V25 before using it on V26.

The asynchronous operator mode uses a bounded 180-second stage deadline and
3600-second phase limit. These are distinct from the original 25/600-second
native-driver limits; neither mode retries or relabels failed evidence.

The new protocol and Editor-event ordering must be qualified in licensed hosts.
Host-free tests/builds alone do not establish that runtime observation order.
`scripts/test-local022-ui-input.ps1` includes actual C# serializer-to-JavaScript
consumer interoperability for all 21 stage/action/value combinations, plus the
receipt I/O and negative/replay contracts. Observed-mode cleanup never sends a
PowerShell close-window message; the runner can terminate only its owned test
process if normal scoped probe shutdown does not complete.
Reference: [Bricsys PromptedForPoint](https://developer.bricsys.com/bricscad/help/en_US/CurVer/DevRef/source/html/56081019-d553-bb3d-55f1-afa769b9fee3.htm).
