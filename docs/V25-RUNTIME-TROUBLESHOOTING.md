# BricsCAD V25 runtime troubleshooting

This runbook separates three different failure classes that can appear close together in the BricsCAD command line. Treating them as the same error leads to misleading fixes.

## 1. DWG font substitution is not a QS3D load failure

Messages such as:

```text
Substituting font "vntimeh.shx" by font "simplex.shx"
Substituting font "VNI-Times" by font "simplex.shx"
```

mean that the opened drawing references fonts that BricsCAD cannot currently resolve. BricsCAD is substituting `simplex.shx` so the drawing can still be displayed. This can change Vietnamese text appearance, spacing, glyph coverage, or plotted text metrics, but it does **not** prove that `QS3D.BricsCAD.V25.dll` failed to load.

Safe remediation:

1. Obtain the exact required font from the drawing owner, project CAD standard, or another source for which the user has a valid redistribution/use right.
2. Install/configure that font through the normal Windows/BricsCAD font search path, or intentionally replace/map the drawing text style after reviewing the drawing standard.
3. Reopen the drawing and verify text visually before saving or plotting.

QS3D must not bundle, download, or silently redistribute third-party/proprietary VNI/SHX fonts, and must not silently rewrite customer drawing text styles just to hide this warning.

## 2. `Could not load file or assembly ... QS3D.BricsCAD.V25.dll`

This is a separate plugin/host loading failure. The important part is the complete exception text after `or one of its dependencies`; do not infer the cause from the first line alone.

Check in this order:

1. Use **BricsCAD V25 on Windows with a Pro-or-higher license level**. Bricsys documents the BRX/.NET API as Pro-or-higher. BricsCAD Shape/Lite is not a supported QS3D managed-plugin host.
2. Confirm the installed plugin exists at `%LOCALAPPDATA%\QS3D\BricsCAD-V25\QS3D.BricsCAD.V25.dll` together with `QS3D.Core.dll`.
3. Re-run `install-v25-autoload.ps1 -Force` only after every BricsCAD process is closed. The installer copies and calls `Unblock-File` on packaged payloads; therefore Windows download blocking must not be assumed to be the root cause without the complete host exception.
4. In a supported V25 Pro-or-higher host, use `NETLOAD` only as a diagnostic when DemandLoad fails, select the installed `QS3D.BricsCAD.V25.dll`, and capture the **complete** error text.
5. If manual `NETLOAD` succeeds, test `QS3DRUNTIMECHECK` and then `QS3D`. If DemandLoad still fails, inspect the registration described below.

`Unable to recognize command "QS3D"` immediately after a DemandLoad exception is normally a secondary symptom: BricsCAD could not load the assembly that defines the command, so the command wrapper never became usable.

Do not copy BricsCAD SDK/runtime assemblies such as `BrxMgd.dll`, `TD_Mgd.dll`, or `TD_MgdBrep.dll` into the QS3D package as a generic workaround. The V25 adapter intentionally resolves those host assemblies from the installed BricsCAD runtime.

## 3. Installer says BricsCAD is still running

The installer intentionally refuses to replace/register the plugin while a `bricscad.exe` process is alive. Close all BricsCAD windows and wait for the processes to exit before installing/upgrading.

The hardened installer reports the detected process PID and executable path when available so a hidden/stale BricsCAD process is easier to identify. Do not terminate unrelated processes by force unless the user has confirmed they are safe to close.

## DemandLoad registration expected by QS3D

QS3D registers under the matching V25 user key:

```text
HKCU\Software\Bricsys\BricsCAD\<V25-version>\<language>\Applications\QS3D
```

Expected values after installation:

- `Loader`: exact installed `QS3D.BricsCAD.V25.dll` path.
- `LoadCtrls`: `4` for `OnCommand`, or `2` for `OnStartup`.
- `Description`: `QS3D for BricsCAD V25`.
- `Commands`: contains every packaged command from `COMMANDS.txt`, including `QS3D`.

The installer now reads these values back and fails/rolls back if the registration it just wrote does not match the requested mode and installed loader.

Bricsys V25 registry DemandLoad reference: `https://developer.bricsys.com/bricscad/help/en_US/V25/DevRef/source/RegistryDemandLoad.htm`.

Bricsys V25 BRX/.NET product requirement: `https://developer.bricsys.com/bricscad/help/en_US/V25/DevRef/source/BRX.htm`.

## Minimal local verification after an installer/runtime change

On a Windows machine with licensed BricsCAD V25 Pro or higher:

1. Close every BricsCAD process.
2. Install the exact candidate package with the normal signed-production command, using `-Force` only for an intentional upgrade.
3. Start BricsCAD V25 Pro or higher and open a disposable/non-private DWG.
4. Run `QS3DRUNTIMECHECK` and then `QS3D` using DemandLoad.
5. If DemandLoad fails, run `NETLOAD` against the installed DLL once and retain the complete exception in sanitized local evidence.
6. Confirm an unrelated missing-font warning (for example `vntimeh.shx`/`VNI-Times` falling back to `simplex.shx`) does not get reported as a QS3D load failure.

Native BricsCAD execution is LOCAL_ONLY. Source review, static preflight, or a remote build cannot manufacture a runtime PASS.
