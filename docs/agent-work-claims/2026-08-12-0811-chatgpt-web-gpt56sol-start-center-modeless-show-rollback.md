# Work claim — Start Center modeless show rollback

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-start-center-modeless-show-rollback`
- Registered: `2026-08-12T08:11:00+07:00`
- Baseline main SHA: `b9ccef654400f813705442b10c472dff5fff35ac`
- Completed source commit: `8d8f02919a2ed38e21e1b452fab6ceb9cc9c168d`
- Regression commit: `a4b2ebb4fc6fd6abbf272eed8acbed432d683504`
- Readback main SHA before close-out: `fd6d25c1ee5c6f1d9feec6aa42b7d1887d66fb56`
- Priority: P1 deterministic modeless lifecycle hardening found during owner-requested BLT3D clean-room Start Center `continue all` audit.

## Confirmed defect

`StartCenterCommands.ShowStartCenter()` created the singleton `StartCenterWindow`, attached `Closed`, and subscribed the static `DocumentActivated` handler before `Application.ShowModelessWindow(...)`. If the BricsCAD host rejected or threw while showing that newly-created window, the outer catch only wrote a diagnostic. The failed window remained in `_window` until another command attempt replaced it, and the document-level activation subscription remained owned indefinitely even though no Start Center was successfully shown.

The normal-close path correctly unsubscribed, but it could not run when the initial host show failed before a usable modeless window was established. The prior Start Center preflight guarded normal close and activation fail-soft behavior but did not guard failed-open rollback.

## Reserved scope

- `src/QS3D.BricsCAD.V25/StartCenterCommands.cs`
- `scripts/preflight-start-center.py`
- `docs/LOCAL-AGENT-INBOX.md` only for LOCAL_ONLY follow-up/evidence wording if the current repo convention requires it
- this claim file for close-out

## Implemented contract

1. `ShowStartCenter()` now tracks the exact `StartCenterWindow` created by the current invocation.
2. A first-open failure releases only that exact instance through `ReleaseStartCenterWindow(...)` before the existing `QS3DSTART error` diagnostic is written.
3. Release detaches the failed/closed window's `Closed` handler, unsubscribes `DocumentActivated`, and clears `_window`, but only while the released instance still owns the singleton. This prevents stale-window cleanup from tearing down a newer Start Center owner.
4. Normal close uses the same exact-instance release path, keeping close/failure cleanup consistent.
5. Existing successful-open, already-visible activation, active-DWG refresh, Recent DWG, launcher/favorites, dashboard, shortcut, Ribbon and updater behavior remains unchanged.
6. `scripts/preflight-start-center.py` now guards created-instance tracking, failed-open rollback before diagnostics, exact-instance ownership, handler detach, activation unsubscribe, singleton clear, shared normal-close cleanup, and keeps the activation-refresh exception boundary scoped to its own handler.

## Validation evidence

- Source commit: `8d8f02919a2ed38e21e1b452fab6ceb9cc9c168d` — `fix(start-center): roll back failed modeless show`.
- Regression commit: `a4b2ebb4fc6fd6abbf272eed8acbed432d683504` — `test(start-center): guard failed modeless show rollback`.
- Post-concurrency readback at `fd6d25c1ee5c6f1d9feec6aa42b7d1887d66fb56` confirmed both the source rollback and focused regression assertions remained present on `main`.
- The edited Python regression content was syntax-compiled during remote preparation, and the exact committed diff was read back. The full repository preflight suite, adapter compile, and GitHub Actions were not run from this remote connector session.
- `LOCAL-001` already owns the licensed BricsCAD V25 modeless/runtime and full interactive matrix in `PENDING_LOCAL`; no duplicate LOCAL item was created. Native injection of `Application.ShowModelessWindow` failure, successful retry, active-DWG switching and close/reopen remain part of that existing LOCAL_ONLY qualification truth.

## Product boundary / exclusions

- No BLT code/assets were copied; BLT3D remains clean-room UX reference only.
- No duplicate Start Center/Ribbon/Recent DWG implementation was added because those surfaces already existed on `main`.
- No `DocumentBoundWindowLifetime`, Core/project mutation semantics, installer/signing/release behavior, or unrelated UI/XAML was changed.
- No GitHub Actions were dispatched and no BricsCAD V25 runtime PASS is claimed from remote.

## Completion result

`COMPLETED`: the failed-open Start Center command lifecycle is retry-safe at source level, its regression contract is committed on `main`, concurrent agent changes survived subsequent HEAD movement, and native V25 behavior remains explicitly LOCAL_ONLY rather than being promoted from static evidence.