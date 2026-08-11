#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src" / "QS3D.BricsCAD.V25" / "StartCenterCommands.cs"
WINDOW = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "StartCenterWindow.xaml.cs"
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "StartCenterWindow.xaml"
CATALOG = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "StartCenterCommandCatalog.cs"
STATE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "StartCenterUserStateStore.cs"


def read(path):
    if not path.exists():
        raise AssertionError("Missing Start Center surface: " + str(path.relative_to(ROOT)))
    return path.read_text(encoding="utf-8")


def require(text, token, label):
    if token not in text:
        raise AssertionError(label + " missing token: " + token)


def forbid(text, token, label):
    if token in text:
        raise AssertionError(label + " must not contain: " + token)


def main():
    commands, window, xaml, catalog, state = map(read, (COMMANDS, WINDOW, XAML, CATALOG, STATE))
    require(commands, '[CommandMethod("QS3DSTART", CommandFlags.Modal)]', "QS3DSTART registration")
    require(commands, "Application.ShowModelessWindow", "modeless BricsCAD host")
    forbid(commands, "Process.Start", "Start Center command surface")

    require(xaml, 'ResourceDictionary Source="Theme.xaml"', "premium shared theme")
    for name in ("QUICK WORKFLOW", "COMMAND LAUNCHER", "RECENT PROJECTS / DWG", "FAVORITES", "REVIEW &amp; DIAGNOSTICS"):
        require(xaml, name, "Start Center UX")
    for action in ("OnNewDrawingClick", "OnOpenDrawingClick", "OnSaveDrawingClick", "OnSaveAsDrawingClick"):
        require(xaml, action, "document action wiring")

    require(window, "StartCenterCommandCatalog.TryGet", "allowlisted command dispatch")
    require(window, "Application.DocumentManager.MdiActiveDocument", "click-time active document resolution")
    require(window, "ProjectContextCoordinator.TryGetReadOnly", "non-creating project dashboard")
    require(window, 'project.Metadata.TryGetValue("ActiveFamilyId"', "active Family summary")
    require(window, "ProjectContextCoordinator.HasPendingChanges", "pending-save summary")
    require(window, 'document.SendStringToExecute(item.Command + " "', "allowlisted QS3D dispatch")
    require(window, 'document.SendStringToExecute("_.OPEN ', "recent DWG open command")
    require(window, "File.Exists(normalized)", "recent DWG existence guard")
    require(window, "StartCenterUserStateStore.TryNormalizeDwgPath(recent.Path, out var normalized)", "recent DWG path normalization")
    require(window, "NativeDocumentAction", "fixed native file-action allowlist")
    forbid(window, "ProjectContextCoordinator.GetOrCreate", "Start Center read-only dashboard")
    forbid(window, "System.Diagnostics.Process", "Start Center window")
    forbid(window, "Ribbon", "Start Center reserved scope")

    for token in ("Path.GetFullPath", "Path.IsPathRooted", '".dwg"', "StringComparer.OrdinalIgnoreCase", "Convert.ToBase64String", "File.Replace", "MaxRecentProjects", "StartCenterCommandCatalog.TryGet"):
        require(state, token, "bounded user-state store")
    forbid(state, "Process.Start", "Start Center state store")

    declared = re.findall(r'New\("(QS3D[A-Z0-9]*)"', catalog)
    if len(declared) < 45:
        raise AssertionError("Start Center command catalog is unexpectedly small: " + str(len(declared)))
    if len(declared) != len(set(declared)):
        raise AssertionError("Start Center command catalog contains duplicate command literals.")
    for command in ("QS3D", "QS3DFAMILIES", "QS3DDRAWWALL", "QS3DDRAWBEAM", "QS3DDRAWCOLUMN", "QS3DDRAWSLAB", "QS3DDRAWDOOR", "QS3DDRAWWINDOW", "QS3DCREATESIMILAR", "QS3DBQ", "QS3DED2", "QS3DREBARHEALTHALL", "QS3DRULEPREVIEW", "QS3DREGENPREVIEW", "QS3DHEALTHALL", "QS3DDIAGSUMMARY", "QS3DRELEASECHECK", "QS3DSAVE", "QS3DRELOAD"):
        if command not in declared:
            raise AssertionError("Missing representative Start Center command: " + command)

    print("PASS: Start Center source contract is present, allowlisted, bounded and non-creating on dashboard reads.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("ERROR:", exc)
        raise SystemExit(1)
