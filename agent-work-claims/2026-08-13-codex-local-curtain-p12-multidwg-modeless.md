# Work claim — LOCAL-002 Curtain P12 multi-DWG modeless affinity

- Status: `COMPLETED`
- Agent: `codex-local-root-20260813`
- Registered: `2026-08-13T14:28:03+07:00`
- Baseline main SHA: `0413a7951f1946b54211f04ea3de2b901b0576a0`
- Priority: `LOCAL-002 / P0 / P12` — the remaining Curtain modeless/multi-DWG cell requires the installed licensed BricsCAD V25 document manager, WPF dispatcher and real document-destroy lifecycle.

## Reserved scope

Prepare and execute one bounded exact-SHA local qualification for `LOCAL-002 / P12`: two disposable drawings open in one BricsCAD V25 process with a real modeless `CurtainWallWindow` bound to drawing A. Prove Refresh and command dispatch refuse while drawing B is active, resume only after A is reactivated, never mutate B, and the A-bound window closes when A is destroyed while B remains usable.

This lane may add only automation-only probe commands, a guarded local PowerShell runner, a focused static preflight, and the exact P12 documentation/evidence closeout. Any product defect reproduced by the probe will be reported with sanitized evidence for a non-local source-fix lane; this local claim will not implement an ordinary production UI/Curtain fix.

## Expected surfaces

- new `src/QS3D.BricsCAD.V25/CurtainPanelMultiDwgRuntimeProbeCommands.cs`
- new `scripts/test-bricscad-v25-curtain-panel-multidwg.ps1`
- new `scripts/preflight-curtain-panel-multidwg-runtime-probe.py`
- `docs/CURTAIN-NATIVE-PANELS.md` only for the guarded P12 handoff and exact result
- `docs/LOCAL-AGENT-INBOX.md` only for the bounded P12 result when evidence exists
- this claim file

## Excluded scope

- No edits to `CurtainWallWindow.xaml(.cs)`, `CurtainWallHubCommands.cs`, `DocumentBoundWindowLifetime.cs`, Curtain builders/planners/health, Core domain code, Level placement, shared modeless UI, or other production behavior.
- No P10 generated-panel Workspace/Family resolution work (`#982`) and no P11 Undo semantic/native coherence fix (`#987`).
- No takeover of the active LOCAL-003 Level Z-chain, BQ/modeless, Start Center, responsive UI PR #975, or other active claims.
- No customer/private DWG, GitHub Actions, release, installer, signing, V26 qualification, or broad H.1 all-window matrix.

## Validation plan

- Re-fetch current `origin/main` and recheck claims/open PRs before every implementation commit.
- Require a clean exact-SHA x64 Release V25 adapter/Core pair whose ProductVersion identifies the tested SHA.
- Use two ordinary repository-sample copies under one fresh outside-repository artifact root; require no pre-existing BricsCAD process, sidecar, backup or drawing lock.
- Launch one hidden BricsCAD V25 process, show the real Curtain Hub for A, open B, exercise the real bound-window refresh/command handler on the WPF dispatcher, reactivate A, close A and verify the document-destroy close path.
- Compare sanitized project/native aggregate state for A and B before/after; verify B never changes and command dispatch targets A only.
- Accept PASS/allowlisted FAIL only after launched-process exit, private-script deletion, sidecar/backup/drawing-lock cleanup and byte-identical restoration of both disposable drawings.
- Run the focused gate, relevant modeless/Curtain gates, generic preflight, installed-reference V25 Release build and the licensed P12 runner. Do not dispatch Actions.

## Coordination

Current active/blocked claim and open-PR scans at the baseline found no reservation for `LOCAL-002 / P12`, `CurtainWallWindow` multi-DWG runtime, or a Curtain P12 runner. The active LOCAL-003 claim explicitly excludes Curtain P01-P12 implementation. PR #975 owns unrelated responsive Workspace/Quantity UI surfaces and does not touch this lane.

## Completion condition

The claim is visible on current `origin/main`; the guarded probe/runner/gate are merged; one clean exact-SHA licensed V25 P12 result is recorded with sanitized evidence; and this claim is marked `COMPLETED`. A product failure leaves P12 `PENDING_LOCAL`/source-blocked with a remote handoff instead of being misreported as PASS.

## Completion evidence

- Source-preparation PR `#992` merged as `30fb30a7d91ec95f3a188ddd5cbc6cd792daeb5d`; BricsCAD-hosted WPF discovery hardening PR `#993` merged as `47b64868111522628fe90f1f00c409ea455160b6`; native close-boundary hardening PR `#994` merged as `1ddba400c2d781ee0f5ac6e2a507d26d33b37477`; exact private-state cleanup correction PR `#995` merged as `67426427d67490b246eb035af1bc19c930789686`.
- Clean exact `main` SHA `67426427d67490b246eb035af1bc19c930789686` built the V25 x64 Release adapter with `0 warnings / 0 errors`; adapter SHA-256 was `85D36541D52A119DCCD031D0555EAB3EE097CB0E9D409A24B5F4A3B81A48DC51` and BricsCAD was `25.2.10`.
- The real A-bound Curtain Hub refused routed Refresh and Frame Health while B was active without changing either project; A refresh succeeded after reactivation; destroying A closed the bound window and raised Closed; B remained active and unchanged.
- Both ordinary repository-sample copies remained byte-identical to SHA-256 `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`. Process and private-script cleanup passed, and an independent post-run scan found zero `.qsdb`, backup, lock, drawing-lock or script files.
- The result promotes only P12 to bounded `LOCAL_PASS`. It does not qualify broad H.1, P10/P11, Level placement or overall LOCAL-002.
