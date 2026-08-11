#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src" / "QS3D.BricsCAD.V25" / "StartCenterCommands.cs"
WINDOW = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "StartCenterWindow.xaml.cs"
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "StartCenterWindow.xaml"
CATALOG = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "StartCenterCommandCatalog.cs"
STATE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "StartCenterUserStateStore.cs"
WALL_QUANTITY = ROOT / "src" / "QS3D.BricsCAD.V25" / "WallQuantityCommands.cs"
REFERENCE_SEARCH = ROOT / "src" / "QS3D.BricsCAD.V25" / "ReferenceSearchCommands.cs"
QUANTITY_SETTINGS_HEALTH = ROOT / "src" / "QS3D.BricsCAD.V25" / "QuantitySettingsDiagnosticExportCommands.cs"
QUANTITY_RULE_CREATE = ROOT / "src" / "QS3D.BricsCAD.V25" / "QuantityRuleCreateCommands.cs"


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
    wall_quantity, reference_search, quantity_settings_health, quantity_rule_create = map(
        read, (WALL_QUANTITY, REFERENCE_SEARCH, QUANTITY_SETTINGS_HEALTH, QUANTITY_RULE_CREATE))

    require(commands, '[CommandMethod("QS3DSTART", CommandFlags.Modal)]', "QS3DSTART registration")
    require(commands, "Application.ShowModelessWindow", "modeless BricsCAD host")
    forbid(commands, "Process.Start", "Start Center command surface")

    require(xaml, 'ResourceDictionary Source="Theme.xaml"', "premium shared theme")
    for name in ("QUICK WORKFLOW", "COMMAND LAUNCHER", "RECENT PROJECTS / DWG", "FAVORITES", "REVIEW &amp; DIAGNOSTICS"):
        require(xaml, name, "Start Center UX")
    for action in ("OnNewDrawingClick", "OnOpenDrawingClick", "OnSaveDrawingClick", "OnSaveAsDrawingClick"):
        require(xaml, action, "document action wiring")
    for command in ("QS3DWALLQTY", "QS3DREFSEARCH", "QS3DRULECREATE", "QS3DQSETTINGSHEALTHEXPORT"):
        require(xaml, 'Tag="' + command + '"', "featured Start Center workflow")
    require(xaml, "hỗ trợ tiếng Việt có/không dấu", "Vietnamese search discoverability")

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
    require(state, "TryDecode", "corrupt-line tolerant user-state loader")
    require(state, "Convert.FromBase64String", "encoded user-state loader")
    require(state, "if (!TryDecode(line.Substring(2), out var command)) continue;", "favorite/recent corrupt-line isolation")
    require(state, "if (!TryDecode(parts[2], out var decoded)) continue;", "recent-project corrupt-line isolation")
    forbid(state, "Process.Start", "Start Center state store")

    require(catalog, "StringSplitOptions.RemoveEmptyEntries", "multi-token launcher search")
    require(catalog, "ScoreTerm", "multi-token launcher scoring")
    require(catalog, "if (termScore == 0) return 0;", "AND-semantics launcher search")
    require(catalog, "FoldForSearch", "accent-insensitive launcher search")
    require(catalog, "NormalizationForm.FormD", "Unicode decomposition search fold")
    require(catalog, "UnicodeCategory.NonSpacingMark", "diacritic removal search fold")
    require(catalog, "if (c == 'đ') builder.Append('d');", "Vietnamese d-stroke search fold")
    require(catalog, "else if (c == 'Đ') builder.Append('D');", "Vietnamese D-stroke search fold")

    declared = re.findall(r'New\("(QS3D[A-Z0-9]*)"', catalog)
    if len(declared) < 49:
        raise AssertionError("Start Center command catalog is unexpectedly small: " + str(len(declared)))
    if len(declared) != len(set(declared)):
        raise AssertionError("Start Center command catalog contains duplicate command literals.")
    for command in ("QS3D", "QS3DFAMILIES", "QS3DDRAWWALL", "QS3DDRAWBEAM", "QS3DDRAWCOLUMN", "QS3DDRAWSLAB", "QS3DDRAWDOOR", "QS3DDRAWWINDOW", "QS3DCREATESIMILAR", "QS3DWALLQTY", "QS3DBQ", "QS3DED2", "QS3DREBARHEALTHALL", "QS3DRULEPREVIEW", "QS3DREGENPREVIEW", "QS3DHEALTHALL", "QS3DDIAGSUMMARY", "QS3DQSETTINGSHEALTHEXPORT", "QS3DRULECREATE", "QS3DREFSEARCH", "QS3DRELEASECHECK", "QS3DSAVE", "QS3DRELOAD"):
        if command not in declared:
            raise AssertionError("Missing representative Start Center command: " + command)

    require(wall_quantity, '[CommandMethod("QS3DWALLQTY", CommandFlags.Modal)]', "Wall Quantity source registration")
    require(reference_search, '[CommandMethod("QS3DREFSEARCH", CommandFlags.Modal)]', "reference-search source registration")
    require(quantity_settings_health, '[CommandMethod("QS3DQSETTINGSHEALTHEXPORT", CommandFlags.Modal)]', "quantity-settings-health source registration")
    require(quantity_rule_create, '[CommandMethod("QS3DRULECREATE", CommandFlags.Modal)]', "quantity-rule-create source registration")

    print("PASS: Start Center source contract is present, allowlisted, accent-insensitive, featured, token-searchable, corruption-tolerant and non-creating on dashboard reads.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("ERROR:", exc)
        raise SystemExit(1)
