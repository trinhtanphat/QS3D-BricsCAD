#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(rel):
    return (ROOT / rel).read_text(encoding="utf-8")


def require(text, needle, rel):
    if needle not in text:
        raise SystemExit(f"FAIL: {rel} missing required contract: {needle}")


def forbid(text, needle, rel):
    if needle in text:
        raise SystemExit(f"FAIL: {rel} contains forbidden stale contract: {needle}")


def main():
    store_rel = "src/QS3D.BricsCAD.V25/Services/UserUiLayoutStore.cs"
    v26_project_rel = "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj"

    store = read(store_rel)
    for needle in (
        "#if BRICSCAD_V26",
        'private const string HostMajorDirectory = "BricsCAD-V26";',
        "#else",
        'private const string HostMajorDirectory = "BricsCAD-V25";',
        'Environment.SpecialFolder.LocalApplicationData',
        'Path.Combine(root, "QS3D", HostMajorDirectory, "ui-layout-v1.txt")',
        'File.Replace(temp, path, backup, true)',
        'Normalize(layout);',
    ):
        require(store, needle, store_rel)

    forbid(
        store,
        'Path.Combine(root, "QS3D", "BricsCAD-V25", "ui-layout-v1.txt")',
        store_rel,
    )

    v26_project = read(v26_project_rel)
    for needle in (
        '<DefineConstants>$(DefineConstants);BRICSCAD_V26</DefineConstants>',
        '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"',
        '<AssemblyName>QS3D.BricsCAD.V26</AssemblyName>',
    ):
        require(v26_project, needle, v26_project_rel)

    print(
        "PASS: per-user UI layout persistence keeps its existing format/atomic-save contract "
        "while V25 and V26 compile to isolated host-major settings directories."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
