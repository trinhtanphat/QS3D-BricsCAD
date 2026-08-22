# Work claim — Start Center modeless show rollback

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-start-center-modeless-show-rollback`
- Registered: `2026-08-12T08:11:00+07:00`
- Baseline main SHA: `b9ccef654400f813705442b10c472dff5fff35ac`
- Priority: P1 deterministic modeless lifecycle hardening found during owner-requested BLT3D clean-room Start Center `continue all` audit.

## Confirmed defect

`StartCenterCommands.ShowStartCenter()` creates the singleton `StartCenterWindow`, attaches `Closed`, and subscribes the static `DocumentActivated` handler before `Application.ShowModelessWindow(...)`. If the BricsCAD host rejects or throws while showing that newly-created window, the outer catch only writes a diagnostic. The failed window remains in `_window` until another command attempt replaces it, and the document-level activation subscription remains owned indefinitely even though no Start Center was successfully shown.

The normal-close path correctly unsubscribes, but it cannot run when the initial host show fails before a usable modeless window is established. The existing Start Center preflight guards normal close and activation fail-soft behavior but does not guard failed-open rollback.

## Reserved scope

- `src/QS3D.BricsCAD.V25/StartCenterCommands.cs`
- `scripts/preflight-start-center.py`
- `docs/LOCAL-AGENT-INBOX.md` only for LOCAL_ONLY follow-up/evidence wording if the current repo convention requires it
- this claim file for close-out

## Detailed implementation plan

1. Re-fetch moving `main`, the exact Start Center command source, regression gate, and claim ownership immediately before each write.
2. Track whether the current invocation created the Start Center singleton, without changing the behavior of an already-loaded/visible window.
3. If first-time `ShowModelessWindow(...)` fails, detach the failed window's `Closed` handler, release the `DocumentActivated` subscription owned by that failed instance, clear `_window` only when it still refers to that exact failed instance, and keep the existing user-facing `QS3DSTART error` diagnostic behavior.
4. Keep normal close idempotent and keep document-activation refresh fail-soft. Do not make Start Center create QS3D project state or change command dispatch, favorites, Recent DWG, dashboard, Ribbon, updater, or XAML behavior.
5. Extend `scripts/preflight-start-center.py` so source regression explicitly requires failure rollback of the newly-created modeless window and forbids reverting to a show call with no failed-open cleanup ownership.
6. Re-fetch and inspect the exact post-write files/diff on `main`. Do not dispatch GitHub Actions. Do not claim compile/runtime PASS remotely.
7. Record only the remaining BricsCAD V25 native failure-injection scenario as LOCAL_ONLY if required, then mark this claim `COMPLETED` with exact merged SHA evidence.

## Intended contract

- A newly-created Start Center either becomes a successfully host-owned modeless window, or its command-level lifecycle ownership is rolled back before the invocation returns.
- Failed first show does not leave `_window` pointing at an unusable instance and does not leave `DocumentActivated` subscribed solely because that failed instance was attempted.
- Re-running `QS3DSTART` after a failed show starts from clean singleton/subscription state.
- Existing successful-open, already-visible activation, normal-close cleanup, Recent DWG, launcher/favorites, dashboard and shortcut behavior remains unchanged.

## Excluded scope

- No BLT code/assets copying; BLT3D remains clean-room UX reference only.
- No duplicate Start Center/Ribbon/Recent DWG implementation that already exists on `main`.
- No `DocumentBoundWindowLifetime` edits; its completed attach-atomicity lane explicitly excluded modeless call-site edits.
- No Core/project mutation semantics, installer/signing/release behavior, or unrelated UI refactors.
- No GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim from remote.

## Validation plan

Use source readback plus the auto-discovered Start Center static preflight contract as remote evidence. The native BricsCAD failure path (`Application.ShowModelessWindow` throwing during first open), successful retry, drawing activation, and close/reopen behavior remain LOCAL_ONLY on a licensed BricsCAD V25 workstation.

## Completion condition

The failed-open Start Center lifecycle is retry-safe at source level, the focused static regression guard is merged on moving `main`, concurrent agent changes are preserved, and this claim is closed with verified main ancestry/SHA without force-push.