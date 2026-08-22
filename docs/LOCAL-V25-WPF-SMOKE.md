# QS3D — local V25 WPF smoke handoff

Updated: 2026-08-10 (UTC+7)

This is an **early local failure detector** for WPF resources and the two docked palettes. It is intentionally narrower than `docs/LOCAL-V25-QUALIFICATION.md` and must never be reported as full BricsCAD V25 runtime qualification.

## When to run

Run this after `QS3D.BricsCAD.V25` has compiled successfully against the locally installed BricsCAD V25 managed assemblies and before spending time on the full interactive/private-DWG matrix.

```powershell
.\scripts\run-local-v25-wpf-smoke.ps1 `
  -BricsCadDir "C:\Program Files\Bricsys\BricsCAD V25 en_US"
```

The wrapper uses the normal Release adapter output by default. `-PluginPath` may be supplied only when qualifying another exact built DLL intentionally.

## What it checks

1. `Theme.xaml` can be loaded by WPF and the shared styles used by Button/ComboBox/TextBox/GridView/DataGrid/ToolTip/Card resolve without invalid background types.
2. The built adapter assembly can resolve its BricsCAD dependencies from the supplied V25 directory.
3. `WorkspacePanel` and `RightPanel` can be instantiated and complete a 1200×800 WPF measure/arrange/update-layout pass.

## What it does **not** prove

This smoke does not launch BricsCAD and does not prove:

- `NETLOAD` or Registry DemandLoad;
- BricsCAD host-theme inheritance or ComboBox popup rendering;
- Ribbon/runtime command behavior;
- docking/floating persistence inside BricsCAD;
- 100/125/150/200% DPI behavior;
- Direct Draw/editor/jig/OSNAP/ORTHO behavior;
- native `Solid3d`/boolean behavior;
- private-DWG save/reopen/multi-document behavior;
- install/upgrade/uninstall or signing trust.

Those remain in `docs/LOCAL-V25-QUALIFICATION.md`, `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md`, and `docs/LOCAL-AGENT-OPEN-WORK-ADDENDUM-2026-08-10.md`.

## Failure handling

If this smoke fails, record the exact SHA, BricsCAD V25 build/directory, failing type/resource and sanitized exception. Fix the source, rebuild, and rerun on the fixed exact SHA. Do not work around failures by copying proprietary BricsCAD DLLs into Git or weakening Windows/BricsCAD security settings.

If it passes, report only **offline WPF smoke PASS**. Continue with the canonical exact-SHA licensed V25 qualification before using any runtime/release-qualified wording.
