# Work claim — UI virtualization attached-property owner parity

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260815-ui-virtualization-owner-parity`
- Registered: `2026-08-15T05:54:00+07:00`
- Baseline main SHA: `92fac8cfd7132c30575d4e90f7b619f271d93e00`
- Implementation branch: `agent/chatgpt-gpt56sol/ui-virtualization-owner-parity-20260815`
- Integration batch: `integration/chatgpt-gpt56sol/ui-virtualization-owner-parity-20260815`
- Priority: unblock fresh V25 aggregate source guards without weakening or overlapping the active V26 UI-preflight owner lane

## Trigger

Fresh V25 cloud run #203 (`31822511453`) on exact main SHA `92fac8cfd7132c30575d4e90f7b619f271d93e00` advances through the earlier reconciled feature guards but fails only `scripts/preflight-ui-production-polish.py`. The failure reports that the implicit `ListBox`, `ListView`, and `TreeView` styles have no `VirtualizingPanel.IsVirtualizing` / `VirtualizingPanel.VirtualizationMode` setters. Direct source read-back shows all three styles already carry the equivalent virtualization/recycling contract under the inherited `VirtualizingStackPanel.*` owner spelling.

## Reserved scope

Normalize only the shared production theme's virtualization attached-property owner spelling so the current deterministic guard reads the same dependency-property contract that WPF applies at runtime.

## Expected surface

- `src/QS3D.BricsCAD.V25/UI/Theme.xaml`
- this claim file for coordination/closeout evidence only

## Explicit exclusions / coordination

- Do **not** edit `scripts/preflight-ui-production-polish.py` or `src/QS3D.BricsCAD.V26/PluginEntry.cs`; those remain reserved by ACTIVE claim `2026-08-14-2318-chatgpt-gpt56sol-v26-ui-production-polish-parity.md` / PR #1380.
- Do **not** edit any file reserved by ACTIVE `2026-08-14-2349-chatgpt-web-gpt56sol-v25-source-guard-reconciliation.md`; that claim explicitly excludes this UI-production-polish guard surface.
- The earlier V25 UI/DPI/performance production-polish claim is `COMPLETED`; this lane does not reopen its broader scope.
- No visual redesign, layout change, new styles, plugin-entry change, packaging/signing change, geometry/persistence work, workflow edit, or licensed-runtime claim.
- No source/test/script commit directly to `main`; implementation stays on the agent branch, then a refreshed integration branch, then one policy-compliant final landing.

## Implementation intent

For the implicit `ListBox`, `ListView`, and `TreeView` styles only, replace the `VirtualizingStackPanel.IsVirtualizing` and `VirtualizingStackPanel.VirtualizationMode` owner spelling with `VirtualizingPanel.IsVirtualizing` and `VirtualizingPanel.VirtualizationMode`. Preserve the existing values (`True`, `Recycling`) and `ScrollViewer.CanContentScroll=True`; make no behavioral or visual change.

## Validation plan

- verify this claim-only commit is reachable from refreshed `main` before source writes;
- create the declared agent branch from the post-claim main SHA;
- inspect the exact three implicit styles before and after the patch;
- verify the current `preflight-ui-production-polish.py` parser now sees the required setters without editing/weakening the guard;
- refresh `main`, assemble a one-file integration candidate, verify exact diff, then land once through PR/integration policy;
- use the repository's automatic dispatcher and fresh exact-SHA V25 cloud run as remote acceptance; if another deterministic gate is exposed, diagnose it on that exact SHA rather than reusing stale CI.

## Completion condition

The exact three implicit styles preserve logical scrolling, virtualization and recycling under the guard-recognized WPF owner spelling; the one-file patch is landed through the declared branch/integration path; and a fresh exact-SHA V25 run advances past `All discovered feature source guards`. Licensed BricsCAD UI/DPI behavior remains `LOCAL_ONLY` and is not represented as remotely qualified.
