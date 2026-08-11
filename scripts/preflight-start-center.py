#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src" / "QS3D.BricsCAD.V25"
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


def section(text, start, end, label):
    start_index = text.find(start)
    if start_index < 0:
        raise AssertionError(label + " missing start token: " + start)
    end_index = text.find(end, start_index + len(start))
    if end_index < 0:
        raise AssertionError(label + " missing end token: " + end)
    return text[start_index:end_index]


def registered_adapter_commands():
    registrations = {}
    pattern = re.compile(r'CommandMethod\s*\(\s*"([^"]+)"')
    for path in sorted(ADAPTER.rglob("*.cs")):
        if "bin" in path.parts or "obj" in path.parts:
            continue
        text = path.read_text(encoding="utf-8")
        for command in pattern.findall(text):
            key = command.upper()
            registrations.setdefault(key, []).append(str(path.relative_to(ROOT)))
    return registrations


def main():
    commands, window, xaml, catalog, state = map(read, (COMMANDS, WINDOW, XAML, CATALOG, STATE))
    wall_quantity, reference_search, quantity_settings_health, quantity_rule_create = map(
        read, (WALL_QUANTITY, REFERENCE_SEARCH, QUANTITY_SETTINGS_HEALTH, QUANTITY_RULE_CREATE))

    require(commands, '[CommandMethod("QS3DSTART", CommandFlags.Modal)]', "QS3DSTART registration")
    require(commands, "Application.ShowModelessWindow", "modeless BricsCAD host")
    require(commands, "Application.DocumentManager.DocumentActivated += OnDocumentActivated;", "active-DWG refresh subscription")
    require(commands, "Application.DocumentManager.DocumentActivated -= OnDocumentActivated;", "active-DWG refresh unsubscription")
    require(commands, "private static void OnDocumentActivated(object sender, DocumentCollectionEventArgs e)", "BricsCAD document activation handler")
    require(commands, "window.RefreshFromActiveDocument();", "active-DWG refresh callback")
    require(commands, "_window.Closed += OnStartCenterClosed;", "named Start Center close lifecycle")
    require(commands, "_documentActivatedSubscribed", "idempotent activation subscription guard")
    activation_handler = section(
        commands,
        "private static void OnDocumentActivated",
        "private static void OnStartCenterClosed",
        "BricsCAD document activation handler")
    require(activation_handler, "try", "activation refresh exception boundary")
    require(activation_handler, "catch (System.Exception ex)", "activation refresh exception containment")
    require(activation_handler, 'e.Document?.Editor.WriteMessage("\\nQS3DSTART refresh warning: " + ex.Message);', "activation refresh diagnostic")
    require(activation_handler, "catch (System.Exception)", "activation diagnostic exception containment")
    forbid(commands, "_window.Closed += (_, __) => _window = null;", "anonymous Start Center close lifecycle")
    forbid(commands, "Process.Start", "Start Center command surface")

    require(xaml, 'ResourceDictionary Source="Theme.xaml"', "premium shared theme")
    for name in ("QUICK WORKFLOW", "COMMAND LAUNCHER", "RECENT PROJECTS / DWG", "FAVORITES", "REVIEW &amp; DIAGNOSTICS"):
        require(xaml, name, "Start Center UX")
    for action in ("OnNewDrawingClick", "OnOpenDrawingClick", "OnSaveDrawingClick", "OnSaveAsDrawingClick"):
        require(xaml, action, "document action wiring")
    for command in ("QS3DWALLQTY", "QS3DREFSEARCH", "QS3DRULECREATE", "QS3DQSETTINGSHEALTHEXPORT"):
        require(xaml, 'Tag="' + command + '"', "featured Start Center workflow")
    require(xaml, "hỗ trợ tiếng Việt có/không dấu", "Vietnamese search discoverability")
    for token in ("RecentProjectSearchBox", "RecentProjectFilter", "RecentProjectCountText", "OnRecentProjectSearchChanged", "OnRecentProjectFilterChanged"):
        require(xaml, token, "recent-DWG search/filter UI")
    require(xaml, 'Content="★ Ghim / Bỏ ghim lệnh"', "launcher favorite action label")
    require(xaml, 'Content="Bỏ ghim mục chọn"', "favorite-list removal action")
    require(xaml, 'Click="OnRemoveFavoriteClick"', "favorite-list removal wiring")

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
    require(window, 'private const string PinnedRecentProjects = "Đã ghim";', "recent-DWG pinned filter")
    require(window, 'private const string AvailableRecentProjects = "Sẵn sàng";', "recent-DWG available filter")
    require(window, 'private const string MissingRecentProjects = "Thiếu file";', "recent-DWG missing filter")
    require(window, "x.DisplayName.IndexOf(query", "recent-DWG name search")
    require(window, "x.Path.IndexOf(query", "recent-DWG path search")
    require(window, "projects = projects.Where(x => x.IsPinned);", "recent-DWG pin filtering")
    require(window, "projects = projects.Where(x => x.Exists);", "recent-DWG availability filtering")
    require(window, "projects = projects.Where(x => !x.Exists);", "recent-DWG missing filtering")
    require(window, 'RecentProjectCountText.Text = filtered.Count + " / " + state.RecentProjects.Count;', "recent-DWG filtered count")
    require(window, "e.Key == Key.Enter && (CommandList.IsKeyboardFocusWithin || SearchBox.IsKeyboardFocusWithin)", "search-box Enter execution")
    require(window, "e.Key == Key.Down && SearchBox.IsKeyboardFocusWithin && CommandList.Items.Count > 0", "search-box Down navigation")
    require(window, "CommandList.ScrollIntoView(CommandList.SelectedItem);", "keyboard result visibility")
    require(window, "private void OnToggleFavoriteClick", "launcher favorite handler")
    require(window, "if (!(CommandList.SelectedItem is StartCenterCommandItem item))", "launcher-specific favorite target")
    require(window, "private void OnRemoveFavoriteClick", "favorite removal handler")
    require(window, "if (!(FavoriteList.SelectedItem is StartCenterCommandItem item))", "favorite-list-specific removal target")
    forbid(window, "CommandList.SelectedItem as StartCenterCommandItem ?? FavoriteList.SelectedItem as StartCenterCommandItem", "ambiguous favorite target")
    forbid(window, "ProjectContextCoordinator.GetOrCreate", "Start Center read-only dashboard")
    forbid(window, "System.Diagnostics.Process", "Start Center window")
    forbid(window, "Ribbon", "Start Center reserved scope")

    for token in ("Path.GetFullPath", "Path.IsPathRooted", '".dwg"', "StringComparer.OrdinalIgnoreCase", "Convert.ToBase64String", "File.Replace", "MaxRecentProjects", "StartCenterCommandCatalog.TryGet"):
        require(state, token, "bounded user-state store")
    require(state, "TryDecode", "corrupt-line tolerant user-state loader")
    require(state, "Convert.FromBase64String", "encoded user-state loader")
    require(state, "if (!TryDecode(line.Substring(2), out var command)) continue;", "favorite/recent corrupt-line isolation")
    require(state, "if (!TryDecode(parts[2], out var decoded)) continue;", "recent-project corrupt-line isolation")
    require(state, "private static bool TrySettingsPath(out string path)", "optional local-state path resolver")
    require(state, "if (!TrySettingsPath(out var path)) return state;", "load fail-soft local-state path")
    require(state, "if (!TrySettingsPath(out var path)) return;", "save fail-soft local-state path")
    require(state, "Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)", "local state root")
    require(state, "ex is System.Security.SecurityException", "DWG/path security fail-soft guard")
    require(state, "catch (System.Security.SecurityException) { }", "local-state security fail-soft handling")
    forbid(state, 'throw new InvalidOperationException("LocalApplicationData is unavailable.")', "optional local-state path resolution")
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

    registrations = registered_adapter_commands()
    missing_registrations = sorted(command for command in declared if command.upper() not in registrations)
    if missing_registrations:
        raise AssertionError(
            "Start Center allowlist contains commands without literal adapter CommandMethod registration: "
            + ", ".join(missing_registrations)
        )

    require(wall_quantity, '[CommandMethod("QS3DWALLQTY", CommandFlags.Modal)]', "Wall Quantity source registration")
    require(reference_search, '[CommandMethod("QS3DREFSEARCH", CommandFlags.Modal)]', "reference-search source registration")
    require(quantity_settings_health, '[CommandMethod("QS3DQSETTINGSHEALTHEXPORT", CommandFlags.Modal)]', "quantity-settings-health source registration")
    require(quantity_rule_create, '[CommandMethod("QS3DRULECREATE", CommandFlags.Modal)]', "quantity-rule-create source registration")

    print("PASS: Start Center source contract is present, allowlisted, registration-backed, active-DWG-aware, activation-fail-soft, optional-state-fail-soft, accent-insensitive, featured, recent-filtered, keyboard-complete, favorite-targeted, token-searchable, corruption-tolerant and non-creating on dashboard reads.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("ERROR:", exc)
        raise SystemExit(1)
