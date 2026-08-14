# Work claim — V26 UI production-polish parity

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260814-v26-ui-production-polish-parity`
- Registered: `2026-08-14T23:18:00+07:00`
- Baseline main SHA: `3a25e068804ef37587f32ff5f41a241278d0763c`
- Implementation branch: `agent/chatgpt-gpt56sol/v26-ui-production-polish-parity-20260814`
- Implementation commit: `699098371c2d11c9066f4cfbf0e4249cba48c055`
- Reconciled agent head: `35cfa54d8e37cd27bf5d539e394fc025d593952e`
- Integration batch: `integration/chatgpt-gpt56sol/v26-ui-production-polish-parity-20260814`
- Priority: source parity follow-up after the completed V25 UI/DPI/performance production-polish lane.

## Reserved scope

Restore host-major parity for the existing QS3D-scoped WPF production-polish registration. `QS3D.BricsCAD.V26.csproj` links the V25 UI/code source, including `ProductionUiPolish.cs`, but the V26-specific `PluginEntry.cs` currently does not register `ProductionUiPolish` during startup. This lane adds only the missing V26 startup registration and deterministic source guard coverage proving both host-major entry points keep the same production-polish contract.

## Expected surfaces

- `src/QS3D.BricsCAD.V26/PluginEntry.cs`
- `scripts/preflight-ui-production-polish.py`
- V26 project/source-link contract only if a narrow deterministic guard is needed; no broader V26 startup refactor.

## Excluded scope

- `scripts/preflight-wpf-native-shell.py`, `scripts/preflight-wpf-native-shell-scope.py`, `FamilyManagerWindow.xaml` and `FamilyManagerWindow.xaml.cs`, which are reserved by the ACTIVE WPF native-shell guard reconciliation claim.
- No broad WPF redesign, new controls, Ribbon behavior, geometry, semantic/business behavior, persistence, packaging/signing, updater lifecycle or V26 startup cleanup refactor.
- No licensed V25/V26 runtime qualification, native UI screenshots, real DPI transition measurement or manual GitHub Actions operations.
- No implementation source/test/script commit directly to `main`.

## Validation plan

- verify this claim-only commit is reachable from refreshed `origin/main` and re-check concurrent claims before source writes;
- add `ProductionUiPolish.EnsureRegistered()` to the V26 entry point before host UI creation;
- extend the existing UI production-polish preflight so both V25 and V26 entry points must register the shared guard;
- inspect branch diff against refreshed `main` and keep the implementation branch isolated;
- publish the implementation branch and integration candidate without merging source to `main` unless the owner separately authorizes this session as integration coordinator / final main landing;
- leave real V26 BricsCAD UI/DPI runtime evidence `LOCAL_ONLY`.

## Implementation checkpoint

The implementation is committed on the declared agent branch. `src/QS3D.BricsCAD.V26/PluginEntry.cs` now imports the shared UI namespace and calls `ProductionUiPolish.EnsureRegistered()` immediately after runtime binary identity capture and before `PaletteCoordinator.EnsureCreated()`. `scripts/preflight-ui-production-polish.py` now requires the registration in both V25 and V26 entry points and additionally fails if V26 palette creation precedes registration. The branch was reconciled non-destructively with refreshed `main` through merge commit `35cfa54d8e37cd27bf5d539e394fc025d593952e`; no force push was used.

## Coordination

The ACTIVE `2026-08-14-2312-chatgpt-web-gpt56sol-wpf-native-shell-guard-reconciliation` claim owns only Family Manager/native-shell guard reconciliation and explicitly excludes DPI/virtualization/performance work. This lane does not touch any of its expected surfaces. Current V26 project source-linking proves `ProductionUiPolish.cs` is compiled into V26, while the pre-fix V26-specific `PluginEntry.cs` lacked the registration call.

## Completion condition

The V26 implementation branch registers the shared production-polish guard before V26 host UI creation, deterministic preflight coverage requires registration in both host-major entry points, the branch is published and reviewed against refreshed `main`, and runtime-only V26 DPI/UI acceptance remains truthfully classified `LOCAL_ONLY` until executed on a licensed host. Keep this claim `ACTIVE` until an authorized integration coordinator lands the implementation into the final combined `main` result.