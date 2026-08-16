#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
STORE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "UserUiLayoutStore.cs"
V26_PROJECT = ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj"


def fail(message):
    print("ERROR:", message)
    return 1


def main():
    store = STORE.read_text(encoding="utf-8")
    v26_project = V26_PROJECT.read_text(encoding="utf-8")

    required_store = {
        "V26 compile-time branch": "#if BRICSCAD_V26",
        "V26 settings folder": 'private const string HostSettingsFolder = "BricsCAD-V26";',
        "V25 settings folder": 'private const string HostSettingsFolder = "BricsCAD-V25";',
        "shared host-major path": 'Path.Combine(root, "QS3D", HostSettingsFolder, "ui-layout-v1.txt")',
        "per-user local root": "Environment.SpecialFolder.LocalApplicationData",
    }
    missing = [label for label, token in required_store.items() if token not in store]
    if missing:
        return fail("UI layout host-major contract is missing: " + ", ".join(missing) + ".")

    forbidden = [
        'Path.Combine(root, "QS3D", "BricsCAD-V25", "ui-layout-v1.txt")',
        'Path.Combine(root, "QS3D", "BricsCAD-V26", "ui-layout-v1.txt")',
    ]
    if any(token in store for token in forbidden):
        return fail("SettingsPath must route through HostSettingsFolder rather than one hard-coded host-major directory.")

    required_v26 = {
        "V26 compile symbol": "BRICSCAD_V26",
        "shared V25 source link": '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"',
    }
    missing_v26 = [label for label, token in required_v26.items() if token not in v26_project]
    if missing_v26:
        return fail("V26 shared-source contract is missing: " + ", ".join(missing_v26) + ".")

    print(
        "PASS: persisted UI layout uses compile-time BricsCAD V25/V26 host-major directories while preserving the shared source and LocalApplicationData contract."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
