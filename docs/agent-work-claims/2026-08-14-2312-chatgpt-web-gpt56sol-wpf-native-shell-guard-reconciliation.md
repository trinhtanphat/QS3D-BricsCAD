# Work claim — WPF native-shell guard reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-wpf-native-shell-guard-reconciliation`
- Registered: `2026-08-14T23:12:00+07:00`
- Baseline main SHA: `be1bfe23c5d240dc3fdeafb5e51a6088cd830dc0`
- Implementation branch: `agent/chatgpt-web-gpt56sol/wpf-native-shell-guard-reconciliation-20260814`
- Integration batch: `integration/chatgpt-web-gpt56sol/wpf-native-shell-guard-reconciliation-20260814`
- Priority: P0 current V25 cloud source-gate blocker from automatic run #197 / `31817873907`

## Reserved scope

Reconcile the current WPF native-shell source guards after exact automatic V25 cloud run #197 failed only `scripts/preflight-wpf-native-shell.py` for `src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.xaml` using `WindowStyle=None`, while the scoped native-shell guard in the same aggregate run passed the repository's bounded `WindowChrome` + caption-drag contract.

This claim owns diagnosis and the smallest fail-closed source-guard/UI correction needed so the broad and scoped native-shell contracts agree. It does not preselect whether the stale side is the broad guard or the Family Manager shell; current source remains authoritative after claim visibility is verified.

## Expected surfaces

- `scripts/preflight-wpf-native-shell.py`
- `scripts/preflight-wpf-native-shell-scope.py`
- `src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.xaml`
- `src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.xaml.cs` only if the existing caption-drag/native-shell contract requires source correction
- focused static regression/guard coverage for the reconciled shell rule if needed

## Excluded scope

- No broad WPF redesign, DPI/virtualization/performance work, Ribbon feature additions, or semantic/business behavior changes.
- No changes to unrelated windows merely to silence a guard.
- No weakening of the native-shell safety rule into a blanket `WindowStyle=None` exemption.
- No LOCAL_ONLY BricsCAD runtime qualification, private-DWG evidence, installer/signing work, or manual GitHub Actions dispatch/rerun/cancel.
- The completed UI/DPI/performance claim `docs/agent-work-claims/2026-08-14-2250-chatgpt-gpt56sol-ui-dpi-performance-polish.md` remains historical evidence; this lane fixes only the exact post-integration source-gate inconsistency exposed by #197.

## Validation plan

- verify this claim-only commit is reachable from refreshed `origin/main` and re-check concurrent ACTIVE/BLOCKED claims before source work;
- inspect the two native-shell guards plus current Family Manager XAML/code-behind after reservation visibility;
- preserve fail-closed native chrome/caption-drag behavior while eliminating contradictory source-gate semantics;
- run/read back the focused guard(s), ensure the exact #197 condition is covered deterministically, and inspect the combined diff;
- keep implementation on the agent branch, integrate through the named integration branch, perform one final source landing to `main`, and let the standing automatic dispatcher handle V25 cloud CI;
- report only exact-SHA evidence actually observed.

## Coordination

Automatic V25 cloud run #197 / `31817873907` on source SHA `16666e914d8ea291466c6263a8a16433828cb3ed` passed Generic source guards and CI policy, then failed `All discovered feature source guards` with exactly one failed child: `scripts/preflight-wpf-native-shell.py`. The same aggregate run reported `scripts/preflight-wpf-native-shell-scope.py` PASS. Later current-main movement through `77c3a7be1919ffc3916e99139ecbeb43322161bd` and `be1bfe23c5d240dc3fdeafb5e51a6088cd830dc0` was documentation-only relative to that source tree, so no later source correction was observed before registration.

## Completion condition

The contradictory native-shell guard behavior is resolved without weakening the shell safety contract, deterministic focused validation is present, the implementation is integrated through the declared integration branch, the final source landing is verified on current `main`, and subsequent automatic CI evidence is recorded truthfully.
