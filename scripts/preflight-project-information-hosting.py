#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]


def read(relative):
    path = ROOT / relative
    if not path.is_file():
        fail(f"missing required source: {relative}")
    return path.read_text(encoding="utf-8")


def require(text, needle, label):
    if needle not in text:
        fail(f"{label}: expected source contract not found: {needle}")


def forbid(text, needle, label):
    if needle in text:
        fail(f"{label}: forbidden source contract found: {needle}")


def fail(message):
    print("ERROR:", message)
    raise SystemExit(1)


def main():
    commands = read("src/QS3D.BricsCAD.V25/ProjectSetupCommands.cs")
    palette = read("src/QS3D.BricsCAD.V25/ProjectSetupPaletteCoordinator.cs")
    panel = read("src/QS3D.BricsCAD.V25/UI/BltProjectSetupPanel.cs")
    activation = read("src/QS3D.BricsCAD.V25/Ribbon/ProjectTabActivationCoordinator.cs")
    ribbon = read("src/QS3D.BricsCAD.V25/Ribbon/ProjectRibbonAugmenter.cs")
    properties_commands = read("src/QS3D.BricsCAD.V25/ProjectPropertiesCommands.cs")
    v26_project = read("src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj")

    require(commands, '[CommandMethod("QS3DPROJECTINFO", CommandFlags.Modal)]', "Project Information command")
    require(commands, "ProjectSetupPaletteCoordinator.ShowProjectInformation();", "Project Information command")
    forbid(commands, "QS3DPROJECTPROPERTIES", "Project Information command ownership")
    require(properties_commands, '[CommandMethod("QS3DPROJECTPROPERTIES", CommandFlags.Modal)]', "Project Properties command ownership")

    for token in (
        'new PaletteSet("QS3D — Thông tin dự án", ProjectSetupGuid)',
        '_palette.AddVisual("Thông tin dự án", _panel, true);',
        'ProjectSetupPaletteCoordinator.ShowProjectInformation()',
        'ProjectSetupPaletteCoordinator.Hide()',
    ):
        source = palette if token.startswith("new PaletteSet") or token.startswith("_palette") else activation
        require(source, token, "Project Information hosting/activation")

    require(panel, '(Chưa xây dựng — Thông tin dự án)', "Project Information placeholder")
    forbid(panel, "Thuộc tính dự án", "Project Information surface separation")
    forbid(palette, "ShowProperties", "Project Information palette separation")

    require(ribbon, 'internal const string ProjectTabGroupId = TabId;', "Project tab identity")
    require(activation, "ProjectRibbonAugmenter.ProjectTabGroupId", "Project tab activation")
    require(activation, "internal static void NotifyActiveTabChanged(string tabId)", "deterministic tab activation hook")
    require(ribbon, 'new ButtonSpec("QS3D_PROJECT_INFO", "Thông tin\\ndự án", "QS3DPROJECTINFO")', "Project Information ribbon route")
    require(ribbon, 'new ButtonSpec("QS3D_PROJECT_PROPERTIES", "Thuộc tính\\ndự án", "QS3DPROJECTPROPERTIES")', "Project Properties ribbon route")

    # Activation starts only after the Project ribbon has reconciled and is torn down with it.
    require(ribbon, "ProjectTabActivationCoordinator.Start();", "Project Information activation lifecycle")
    require(ribbon, "ProjectTabActivationCoordinator.Stop();", "Project Information activation lifecycle")
    require(ribbon, "ProjectSetupPaletteCoordinator.Dispose();", "Project Information palette lifecycle")

    for source_name, source in (("panel", panel), ("palette", palette), ("activation", activation)):
        for token in (
            "ProjectState",
            "ProjectContextCoordinator",
            "ExistingProjectMutationContext",
            "Touch()",
            "SetMetadata",
        ):
            forbid(source, token, f"Project Information {source_name} must remain read-only")

    require(v26_project, '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"', "V26 shared adapter source")

    print("PASS: Project Information owns a dedicated read-only host and Project-tab activation without taking Project Properties ownership.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
