# Workspace scope empty state and Project Browser tab chrome

## Scope

This runbook covers the V25 `WorkspacePanel` presentation adapter for two host-visual defects:

1. the runtime-created `Mô hình` / `Project Browser` tabs must remain inside the QS3D dark visual system instead of inheriting BricsCAD/Windows light `TabItem` chrome;
2. empty Zone/Floor collections must communicate that there is no project data instead of rendering a blank ComboBox.

The implementation is intentionally presentation-only. `WorkspaceViewModel.Zones` and `WorkspaceViewModel.Floors` remain the authoritative collections; no sentinel text is added to Core, persistence, or CAD mutation state.

## Expected behavior

- Both runtime Project Browser tabs use QS3D brushes for idle, hover, selected, focus, and disabled states.
- Selected tab text remains `TextBrush`; selected chrome uses `Bg1Brush` plus the accent border.
- If `Zones.Count == 0`, the Zone selector visibly reads `Không có Zone` and is non-interactive/non-focusable.
- If `Floors.Count == 0`, the Floor selector visibly reads `Không có Tầng` and is non-interactive/non-focusable.
- When either bound collection receives real data, the selector returns to normal non-editable ComboBox selection behavior without a synthetic item.
- DataContext changes, collection clear/reload, unload/reload, and the workspace `Làm mới` path must not leave stale empty-state copy behind.

## Automated validation

Run:

```powershell
python scripts/preflight-workspace-scope-tabs-empty-state.py
```

The source guard fails closed if the host-independent tab template, empty-collection subscriptions, explicit no-data copy, authoritative XAML bindings, or view-model non-sentinel boundary regress.

The normal branch/PR CI remains authoritative for repository policy, source guards, package integrity, Core tests, and smoke tests.

## Licensed-host visual validation

Hosted CI cannot prove BricsCAD palette rendering. On a licensed V25 host:

1. open the QS3D workspace with a project that has no Zone/Floor values;
2. verify `Không có Zone` and `Không có Tầng` are visible and cannot be selected as project values;
3. verify `Mô hình` and `Project Browser` remain dark and legible for idle/hover/selected/focus states;
4. press `Làm mới` and confirm empty-state text remains correct;
5. load or create real Zone/Floor data, refresh, and confirm both selectors return to real options only;
6. switch between the two tabs and confirm no Windows/BricsCAD light tab chrome appears.

Until those steps are recorded on a licensed host, classify the visual host evidence as `PENDING_NATIVE`; hosted/static CI may still fully validate the source and state-boundary contract.
