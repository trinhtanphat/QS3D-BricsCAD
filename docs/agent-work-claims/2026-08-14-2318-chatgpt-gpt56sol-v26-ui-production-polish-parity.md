# Work claim — V26 UI production-polish parity

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260814-v26-ui-production-polish-parity`
- Registered: `2026-08-14T23:18:00+07:00`
- Baseline main SHA: `3a25e068804ef37587f32ff5f41a241278d0763c`
- Implementation branch: `agent/chatgpt-gpt56sol/v26-ui-production-polish-parity-20260814`
- Implementation commit: `699098371c2d11c9066f4cfbf0e4249cba48c055`
- Reconciled agent head: `35cfa54d8e37cd27bf5d539e394fc025d593952e`
- Reviewed integration candidate: `402cf76a326a051a4a4d296c9abcc3d9bb5762e0`
- Combined main landing: `92fac8cfd7132c30575d4e90f7b619f271d93e00`
- Superseded PR: `#1380` (closed unmerged after the source was included by the combined integration landing)
- Priority: source parity follow-up after the completed V25 UI/DPI/performance production-polish lane.

## Completed scope

BricsCAD V26 now activates the same QS3D-scoped WPF production-polish contract already used by V25. `src/QS3D.BricsCAD.V26/PluginEntry.cs` imports the shared UI namespace and invokes `ProductionUiPolish.EnsureRegistered()` after runtime binary identity capture and before `PaletteCoordinator.EnsureCreated()`. The shared `scripts/preflight-ui-production-polish.py` requires the registration in both V25 and V26 entry points and fails if V26 palette creation precedes the registration.

`QS3D.BricsCAD.V26.csproj` continues to link the V25 UI/code source, so this lane adds no duplicate V26 UI implementation and preserves the existing shared-source architecture.

## Changed surfaces

- `src/QS3D.BricsCAD.V26/PluginEntry.cs`
- `scripts/preflight-ui-production-polish.py`
- this claim file for coordination/closeout only

## Integration evidence

The implementation was developed off `main` on the declared agent branch, reconciled non-destructively, and published as the reviewed integration candidate above. An authorized integration coordinator later included that already-reviewed V26 production-polish parity patch in combined main landing `92fac8cfd7132c30575d4e90f7b619f271d93e00`; the landing commit message explicitly records that inclusion. PR #1380 was therefore closed unmerged to prevent a duplicate source landing.

No force push was used by this lane and no implementation source/test/script commit was written directly to `main` by this coding session.

## Exact-SHA CI boundary

The repository automatic dispatcher created V25 cloud run #203 (`31822511453`) for exact combined landing SHA `92fac8cfd7132c30575d4e90f7b619f271d93e00`. That run advanced through the earlier gates and then failed `All discovered feature source guards` specifically in `scripts/preflight-ui-production-polish.py` because the implicit shared `ListBox`, `ListView`, and `TreeView` styles used the equivalent WPF attached-property owner spelling `VirtualizingStackPanel.*`, while the deterministic guard reads the canonical `VirtualizingPanel.*` spelling.

That newly exposed shared-theme owner-spelling issue is separately reserved by ACTIVE claim `2026-08-15-0554-chatgpt-gpt56sol-ui-virtualization-owner-parity`, which owns only `src/QS3D.BricsCAD.V25/UI/Theme.xaml` and explicitly excludes this completed lane's preflight and V26 `PluginEntry` files. Therefore this claim records source integration completion but does **not** claim aggregate CI PASS. Fresh exact-SHA acceptance belongs to the owner-parity follow-up after its one-file fix lands.

## LOCAL_ONLY boundary

Licensed BricsCAD V26 WPF/DPI/runtime qualification remains `LOCAL_ONLY`; no native runtime PASS is claimed. `docs/LOCAL-V26-QUALIFICATION.md` already requires WPF theme/resources, dialogs, DPI scaling, modeless behavior and shutdown/reopen checks on a real V26 host. The canonical live local queue remains `docs/LOCAL-AGENT-INBOX.md`; its existing `LOCAL-010` performance/UI/HiDPI item should be extended with the exact integrated V26 candidate/SHA during the next safe inbox closeout rather than creating a duplicate LOCAL_ONLY queue.

## Coordination

The WPF native-shell reconciliation claim owns Family Manager/native-shell guard surfaces and was not modified by this lane. The UI virtualization owner-parity follow-up owns only the shared Theme.xaml owner-spelling correction exposed by run #203. Geometry, persistence, packaging/signing, updater behavior, business semantics and licensed-runtime qualification remain outside this completed source-parity scope.

## Completion result

Completed for the declared remote-safe V26 source-parity scope: implementation is present in the combined `main` landing, the duplicate PR is closed, exact-SHA CI evidence and the separately owned follow-up blocker are recorded, and runtime-only V26 DPI/UI acceptance remains truthfully `LOCAL_ONLY`.