#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
INSTALLER = ROOT / "scripts" / "install-v25-autoload.ps1"
RUNBOOK = ROOT / "docs" / "V25-RUNTIME-TROUBLESHOOTING.md"
RUNTIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "RuntimeDiagnosticsCommands.cs"


def require(text: str, needle: str, label: str, errors: list[str]) -> None:
    if needle not in text:
        errors.append(f"missing {label}: {needle!r}")


def main() -> int:
    errors: list[str] = []
    installer = INSTALLER.read_text(encoding="utf-8")
    runbook = RUNBOOK.read_text(encoding="utf-8")
    runtime = RUNTIME.read_text(encoding="utf-8")

    require(installer, "Unblock-File -LiteralPath $destination -ErrorAction Stop", "payload unblock", errors)
    require(installer, "Get-RunningBricsCADProcessDetails", "running-process diagnostics", errors)
    require(installer, "PID=", "running-process PID evidence", errors)
    require(installer, "Path=", "running-process path evidence", errors)
    require(installer, "BricsCAD V25 Pro or higher", "supported-host warning", errors)
    require(installer, "Assert-DemandLoadRegistration", "DemandLoad readback gate", errors)
    require(installer, "DemandLoad Loader mismatch", "Loader verification", errors)
    require(installer, "DemandLoad LoadCtrls mismatch", "LoadCtrls verification", errors)
    require(installer, "DemandLoad command registration is missing", "command verification", errors)

    require(runbook, 'Substituting font "vntimeh.shx" by font "simplex.shx"', "SHX substitution classification", errors)
    require(runbook, 'Substituting font "VNI-Times" by font "simplex.shx"', "VNI substitution classification", errors)
    require(runbook, "does **not** prove that `QS3D.BricsCAD.V25.dll` failed to load", "font/plugin separation", errors)
    require(runbook, "BricsCAD V25 on Windows with a Pro-or-higher license level", "host compatibility guidance", errors)
    require(runbook, '`Unable to recognize command "QS3D"`', "secondary-command symptom guidance", errors)
    require(runbook, "LoadCtrls`: `4` for `OnCommand`, or `2` for `OnStartup`", "DemandLoad mode contract", errors)
    require(runbook, "must not bundle, download, or silently redistribute", "font redistribution boundary", errors)
    require(runbook, "assembly version `0.0.0.0`", "V25 BREP assembly-version guidance", errors)
    require(runbook, "file/product version", "V25 BREP file-identity guidance", errors)

    require(runtime, "NativeAssemblyMajorMatches(", "native dependency major helper", errors)
    require(runtime, "allowFileVersionFallback: true", "V25 BREP fallback opt-in", errors)
    require(runtime, "if (assemblyMajor > 0)", "assembly-major precedence", errors)
    require(runtime, "return assemblyMajor == expectedMajor;", "nonzero assembly-major strictness", errors)
    require(runtime, "ReadNativeFileIdentity(assembly)", "loaded-file identity read", errors)
    require(runtime, "FileVersionInfo.GetVersionInfo(path)", "loaded-file version inspection", errors)
    require(runtime, "TryPositiveMajor(identity.FileVersion", "file-version major validation", errors)
    require(runtime, "TryPositiveMajor(identity.ProductVersion", "product-version major validation", errors)
    require(runtime, '"; file " + EmptyAsUnknown(identity.FileVersion)', "BREP file identity display", errors)
    require(runtime, '"; product " + EmptyAsUnknown(identity.ProductVersion)', "BREP product identity display", errors)

    stale_brep_predicate = "return Major(typeof(Brep).Assembly) == ExpectedRuntimeMajor;"
    if stale_brep_predicate in runtime:
        errors.append("V25 BREP diagnostics regressed to assembly-version-only identity")

    if errors:
        print("V25 runtime diagnostics preflight FAILED")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: V25 runtime diagnostics contract is present.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
