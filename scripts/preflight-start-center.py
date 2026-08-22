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
    require(commands, "StartCenterWindow? createdWindow = null;", "new Start Center ownership tracking")
    require(commands, "createdWindow = new StartCenterWindow();", "new Start Center instance capture")
    require(commands, "createdWindow.Closed += OnStartCenterClosed;", "named Start Center close lifecycle")
    require(commands, "_documentActivatedSubscribed", "idempotent activation subscription guard")
    show_method = section(
        commands,
        "public void ShowStartCenter()",
        "private static void SubscribeToDocumentActivation",
        "Start Center show lifecycle")
    require(show_method, "if (createdWindow != null)", "failed-open ownership check")
    require(show_method, "ReleaseStartCenterWindow(createdWindow);", "failed-open lifecycle rollback")
    cleanup_pos = show_method.find("ReleaseStartCenterWindow(createdWindow);")
    diagnostic_pos = show_method.find('document?.Editor.WriteMessage("\\nQS3DSTART error: " + ex.Message);')
    if cleanup_pos < 0 or diagnostic_pos < 0 or cleanup_pos > diagnostic_pos:
        raise AssertionError("failed-open Start Center ownership must be released before command diagnostics")
    diagnostic_try_pos = show_method.find("try", cleanup_pos)
    diagnostic_catch_pos = show_method.find("catch (System.Exception)", diagnostic_pos)
    if diagnostic_try_pos < 0 or diagnostic_try_pos > diagnostic_pos or diagnostic_catch_pos < diagnostic_pos:
        raise AssertionError("Start Center command diagnostics must be contained by their own exception boundary")
    require(
        show_method,
        "Never let optional Start Center diagnostics escape the command failure boundary.",
        "command diagnostic exception containment")
    unsubscribe_handler = section(
        commands,
        "private static void UnsubscribeFromDocumentActivation()",
        "private static void OnDocumentActivated",
        "active-DWG refresh unsubscription")
    require(unsubscribe_handler, "if (!_documentActivatedSubscribed) return;", "idempotent activation unsubscription guard")
    require(unsubscribe_handler, "try", "activation unsubscription exception boundary")
    require(unsubscribe_handler, "Application.DocumentManager.DocumentActivated -= OnDocumentActivated;", "activation event remove")
    require(unsubscribe_handler, "_documentActivatedSubscribed = false;", "successful activation unsubscription state")
    require(unsubscribe_handler, "catch (System.Exception)", "activation unsubscription exception containment")
    require(
        unsubscribe_handler,
        "Keep the flag true so later cleanup can retry without creating a duplicate subscription.",
        "failed activation unsubscription state contract")
    unsubscribe_remove_pos = unsubscribe_handler.find("Application.DocumentManager.DocumentActivated -= OnDocumentActivated;")
    unsubscribe_clear_pos = unsubscribe_handler.find("_documentActivatedSubscribed = false;")
    unsubscribe_catch_pos = unsubscribe_handler.find("catch (System.Exception)")
    if min(unsubscribe_remove_pos, unsubscribe_clear_pos, unsubscribe_catch_pos) < 0 or not (
        unsubscribe_remove_pos < unsubscribe_clear_pos < unsubscribe_catch_pos
    ):
        raise AssertionError("activation subscription flag must clear only after a successful host event remove")
    activation_handler = section(
        commands,
        "private static void OnDocumentActivated",
        "private static void ReleaseStartCenterWindow",
        "BricsCAD document activation handler")
    require(activation_handler, "try", "activation refresh exception boundary")
    require(activation_handler, "catch (System.Exception ex)", "activation refresh exception containment")
    require(activation_handler, 'e.Document?.Editor.WriteMessage("\\nQS3DSTART refresh warning: " + ex.Message);', "activation refresh diagnostic")
    require(activation_handler, "catch (System.Exception)", "activation diagnostic exception containment")
    release_handler = section(
        commands,
        "private static void ReleaseStartCenterWindow",
        "private static void OnStartCenterClosed",
        "Start Center lifecycle release")
    require(release_handler, "if (!ReferenceEquals(window, _window)) return;", "exact Start Center ownership guard")
    require(release_handler, "window.Closed -= OnStartCenterClosed;", "failed/closed window handler release")
    require(release_handler, "UnsubscribeFromDocumentActivation();", "failed/closed activation subscription release")
    require(release_handler, "_window = null;", "failed/closed singleton release")
    release_unsubscribe_pos = release_handler.find("UnsubscribeFromDocumentActivation();")
    release_clear_pos = release_handler.find("_window = null;")
    if release_unsubscribe_pos < 0 or release_clear_pos < 0 or release_unsubscribe_pos > release_clear_pos:
        raise AssertionError("Start Center singleton release must complete after the fail-soft unsubscribe attempt")
    require(commands, "if (sender is StartCenterWindow window)", "typed Start Center close owner")
    require(commands, "ReleaseStartCenterWindow(window);", "normal close shared lifecycle release")
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
    state_load = section(
        state,
        "private static StartCenterUserStateSnapshot LoadCore()",
        "private static StartCenterUserStateSnapshot Normalize",
        "bounded local-state load")
    require(state_load, "File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)", "single opened state stream")
    require(state_load, "if (stream.Length < 0 || stream.Length > MaxFileBytes) return state;", "opened-stream state size guard")
    require(state_load, "new StreamReader(stream, Encoding.UTF8, true, 4096, false)", "streaming state reader")
    require(state_load, "while ((raw = reader.ReadLine()) != null)", "line-by-line state parsing")
    forbid(state_load, "new FileInfo(", "path-level state size precheck")
    forbid(state_load, "File.ReadAllLines", "whole-file state materialization")
    state_save = section(
        state,
        "private static void TrySaveCore(StartCenterUserStateSnapshot state)",
        "private static string Serialize",
        "bounded local-state save")
    save_tokens = (
        "var serialized = Serialize(state);",
        "if (Encoding.UTF8.GetByteCount(serialized) > MaxFileBytes) return;",
        "Directory.CreateDirectory(directory);",
        'temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";',
        "File.WriteAllText(temp, serialized, new UTF8Encoding(false));",
    )
    save_positions = []
    for token in save_tokens:
        require(state_save, token, "bounded local-state save")
        save_positions.append(state_save.find(token))
    if save_positions != sorted(save_positions):
        raise AssertionError("bounded local-state save must size-check serialized bytes before directory/temp creation and publish")
    forbid(state_save, "File.WriteAllText(temp, Serialize(state)", "unbounded serialized state publish")
    forbid(state, "Process.Start", "Start Center state store")

    require(catalog, "private const int MaxSearchQueryChars = 512;", "bounded launcher query characters")
    require(catalog, "private const int MaxSearchTerms = 16;", "bounded launcher search terms")
    bounded_search = section(
        catalog,
        "public static IReadOnlyList<StartCenterCommandItem> Search(string query, string group)",
        "private static int Score(StartCenterCommandItem item, IReadOnlyList<string> terms)",
        "bounded launcher search")
    require(bounded_search, "if (normalizedQuery.Length > MaxSearchQueryChars)", "launcher query character bound")
    require(bounded_search, "char.IsHighSurrogate(normalizedQuery[length - 1])", "launcher query surrogate boundary")
    require(bounded_search, "char.IsLowSurrogate(normalizedQuery[length])", "launcher query surrogate pair preservation")
    require(bounded_search, ".Take(MaxSearchTerms)", "launcher term bound")
    require(bounded_search, ".ToArray();", "bounded launcher term materialization")
    query_bound_pos = bounded_search.find("if (normalizedQuery.Length > MaxSearchQueryChars)")
    split_pos = bounded_search.find(".Split(new[] { ' ', '\\t' }, StringSplitOptions.RemoveEmptyEntries)")
    term_bound_pos = bounded_search.find(".Take(MaxSearchTerms)")
    ranking_pos = bounded_search.find("var ranked = Items")
    if min(query_bound_pos, split_pos, term_bound_pos, ranking_pos) < 0 or not (
        query_bound_pos < split_pos < term_bound_pos < ranking_pos
    ):
        raise AssertionError("launcher search must bound query and terms before ranking")
    require(catalog, "StringSplitOptions.RemoveEmptyEntries", "multi-token launcher search")
    require(catalog, "ScoreTerm", "multi-token launcher scoring")
    require(catalog, "if (termScore == 0) return 0;", "AND-semantics launcher search")
    require(catalog, "FoldForSearch", "accent-insensitive launcher search")
    require(catalog, "NormalizationForm.FormD", "Unicode decomposition search fold")
    require(catalog, "UnicodeCategory.NonSpacingMark", "diacritic removal search fold")
    require(catalog, "if (c == 'đ') builder.Append('d');", "Vietnamese d-stroke search fold")
    require(catalog, "else if (c == 'Đ') builder.Append('D');", "Vietnamese D-stroke search fold")
    search_fold = section(
        catalog,
        "private static string FoldForSearch(string value)",
        "private static List<StartCenterCommandItem> Build()",
        "launcher Unicode search fold")
    require(search_fold, "try", "launcher Unicode normalization exception boundary")
    require(search_fold, "catch (ArgumentException)", "malformed Unicode normalization containment")
    require(search_fold, "return text;", "malformed Unicode raw-text fallback")

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

    print("PASS: Start Center source contract is present, allowlisted, registration-backed, active-DWG-aware, activation-fail-soft, failed-open-rollback-safe, command-diagnostic-fail-soft, unsubscribe-fail-soft, search-bounded, optional-state-fail-soft, stream-size-bounded, write-size-bounded, malformed-Unicode-safe, accent-insensitive, featured, recent-filtered, keyboard-complete, favorite-targeted, token-searchable, corruption-tolerant and non-creating on dashboard reads.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("ERROR:", exc)
        raise SystemExit(1)