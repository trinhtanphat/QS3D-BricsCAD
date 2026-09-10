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

Optional `-SourceProfile Default` selects an existing, manually verified native
profile as the source of the runner's disposable clone. Omission preserves the
native runner's existing host-specific test-profile default. It never attaches
to an existing host or runs against the original profile: the native runner still
creates its unique nonce, records the source and recovery snapshot in the hashed
profile recovery receipt, and restores the original current-profile pointer and
inventory. Profile names are validated before autostart/file mutation. Do not use
this option to bypass privacy/licensing prompts or to relax product assertions.

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

By default, observed mode retains a 600-second wall-clock stage deadline and
3600-second phase limit. Native mode retains its original 25/600-second limits.
The owner-approved `-PauseForOperator` option is valid only with
`OBSERVED_CLICK_V2` and freezes `operator_wait_policy=PAUSE_FOR_OPERATOR_V1`
in both allocation and final receipt (otherwise `WALL_CLOCK_V1`).

Only time between publishing an action and receiving its exact ACK is excluded
from the 600-second active stage budget. Every timer tick still checks the exact
active document/path, paused MCP boundary and latched pick-observation errors.
After the ACK, the remaining budget resumes once and all existing UI/native
assertions run; an ACK is never itself product PASS. Preparation before an action
is published is not paused. The outer UI process has a finite four-hour hard cap;
after verified UI completion its save/exit wait is capped once at the normal
phase timeout, without extending that hard cap. Cold reopen is unchanged. This
allowance covers operator scheduling, not a product performance acceptance claim.

To recover receipt I/O after an operator-session interruption, first revalidate
the sole exact owned live PID, executable, disposable drawing and fresh visible
state. Call `openObservedAllocation(root, runId, ownedPid, { resume: true })` using
the frozen helper. It validates a contiguous canonical action/ACK prefix and
skips only acknowledged actions. It refuses another policy, changed evidence,
gaps, future actions, orphan/malformed ACKs and terminal markers. Repeated
`read()` returns the same outstanding request without doing input. An
unacknowledged gesture with unknown completion must not be replayed or ACKed from
appearance alone: require the actual successful tool history and a fresh
observation, or stop that allocation as no-result. Closed hosts and consumed
allocations are never resumed. V26 must follow a cleaned V25 result using the
same driver and wait policy. Neither mode retries or relabels failed evidence.

The new protocol and Editor-event ordering must be qualified in licensed hosts.
Host-free tests/builds alone do not establish that runtime observation order.
Observed UI allocations also emit passive `render_progress` diagnostics at most
once per ten seconds through the existing controller timer, including while
waiting for an operator ACK. Raw `CMDACTIVE`, controller/Workspace dispatcher
identity and Render-priority posted/completed/aborted notification totals help
separate host command activity from WPF queue progress. Hooks are detached on
controller completion/failure. No render operation, forced layout, timer, input,
render-mode change or acceptance check is added; unavailable diagnostics cannot
change a product verdict. Notification totals are not matched queue lengths
(priorities may change) and completed operations do not prove displayed pixels.
Compare them with actual layout changes and supported screenshots before drawing
a rendering diagnosis. This instrumentation needs licensed observation; it does
not reopen accepted V25/native cells or relabel consumed V26 failures.
`scripts/test-local022-ui-input.ps1` includes actual C# serializer-to-JavaScript
consumer interoperability for all 21 stage/action/value combinations, plus the
receipt I/O and negative/replay contracts. Observed-mode cleanup never sends a
PowerShell close-window message; the runner can terminate only its owned test
process if normal scoped probe shutdown does not complete.
Reference: [Bricsys PromptedForPoint](https://developer.bricsys.com/bricscad/help/en_US/CurVer/DevRef/source/html/56081019-d553-bb3d-55f1-afa769b9fee3.htm).

## Opt-in rendering isolation (diagnosis, not qualification)

`-RenderExperiment -UiDriver OBSERVED_CLICK_V2` on the existing wrapper runs a
five-minute Default → SoftwareOnly → Default experiment in the **owned test
process only**. The original frozen product, disposable drawing, nonce profile,
paused MCP boundary, exact pushed harness and cleanup prerequisites still apply.
No registry/driver/Windows graphics setting is changed. The first120 seconds use
the existing default WPF process mode; seconds120–240 use software rendering;
seconds240–300 restore Default. Supported Computer Use observations should cover
all three stages, using the same layout after baseline preparation. The private
trace records stage, confirmed process mode and reported WPF tier; a mode value
or completed timer is not proof of pixels. A backwards clock or >15-second tick
gap aborts instead of claiming the missing observation interval.

This route never starts the authoring controller or publishes action/ACK
requests. It emits a `DIAGNOSTIC_ONLY` UI marker with no acceptance checks, which
the unchanged strict phase validator rejects. The runner's final receipt also
unconditionally says `DIAGNOSTIC_ONLY` when the explicit switch is set, even if
three synthetic markers were supplied. A nonzero runner exit is expected and
does not itself mean cleanup failed. Inspect all restoration receipts separately.
The original process mode is restored before normal completion and on a caught
failure; a terminated process cannot persist its process-local render mode.

Default/native qualification explicitly sets the inherited experiment flag to0,
then restores the previous environment value in existing cleanup. Both allocation
and receipt freeze the boolean switch. Diagnostic receipts cannot qualify a V25
predecessor, replace V26 UI acceptance, or establish a driver/product fix. After a
useful differential result, diagnose the actual source/environment boundary before
changing a production default. Preserve consumed allocations; no unchanged replay.

The behavioral test compiles the actual state machine against controlled render
property doubles and replays actual entry/verdict expressions in both runners.
It covers boundaries, completion, disposal, clock gaps, setter/restore failures,
an inadmissible baseline and the non-PASS gate. It does not render WPF or run CAD.

Reference: [Microsoft WPF render-thread diagnosis and process-local rendering](https://learn.microsoft.com/en-us/troubleshoot/developer/dotnet/framework/general/wpf-render-thread-failures).

The next diagnostic revision adds independent repaint controls: one probe-owned
floating native `PaletteSet.AddVisual(..., true)` and one opaque owned WPF Window,
each with its own explicitly styled Border/TextBlock (no QS3D visual, binding,
resource dictionary or virtualized collection). The existing one-second timer
updates a visible sequence and alternating background every two ticks. The same
sequence appears in the WPF window's native title. A private ten-tick trace names
the assigned sequence, stage, load/visibility and presentation-source type; these
are state diagnostics, never claims that pixels reached the screen.

Observe each returned window separately as needed. Aim for two distinct displayed
sequences in each Default/SoftwareOnly/restored-Default stage. Keep the control
surfaces at their startup positions and avoid floating/re-docking QS3D while
collecting that comparison; allocation58 changed mode and native parenting in
the same interval, so its visible contents were not an isolated repaint result.
Initial maximization may be needed before the first observation; record it.

If the plain WPF window repaints but the native palette witness does not, focus
on native hosting/presentation. If both independent witnesses repaint while QS3D
does not, investigate the product visual composition/lifetime. If native title
sequence advances but both WPF interiors do not, narrow to shared WPF presentation
or capture. None of these replaces direct-monitor evidence or proves a GPU defect.
The paired controls are closed/disposed before normal diagnostic completion and
on errors; partial constructor failures also clean already-created surfaces. No
new timer/dispatcher, forced layout/render, user input, production default or
qualification assertion is added. A consumed old allocation cannot be relabelled
as this new controlled experiment.

References: [Bricsys AddVisual](https://developer.bricsys.com/bricscad/help/en_US/V25/DevRef/source/html/dfb87171-68ae-c400-2399-0c6a2e0eac12.htm),
[Microsoft native owner for WPF Window](https://learn.microsoft.com/en-us/dotnet/api/system.windows.interop.windowinterophelper).

## Separate native API qualification

The wrapper's opt-in `-NativeApi` runs the existing non-interactive native
`run`/`saved`/`reopen` phases on the same frozen source and package. It cannot be
combined with an explicit `-UiDriver` or `-PauseForOperator`. Omission leaves the
UI route unchanged. Both modes still require a clean exact pushed harness,
exclusive test host, disposable DWG/nonce profile, temporary autostart consent
and verified cleanup; neither attaches to an already open BricsCAD process.

V26 native mode requires a cleaned V25 **native** result from that same source
and V25 package, explicit mode fields and all three original strict phase
validators (including generic Foundation refusal). A UI receipt or older source
cannot substitute for it. Native API evidence does not cover physical input,
visible Workspace rendering or any other missing UI/DPI/private-DWG cell.
