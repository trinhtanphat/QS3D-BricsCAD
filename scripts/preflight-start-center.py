#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src" / "QS3D.BricsCAD.V25"
COMMANDS = ADAPTER / "StartCenterCommands.cs"
HOST = ADAPTER / "StartCenterPaletteCoordinator.cs"
PANEL = ADAPTER / "UI" / "BltStartCenterPanel.cs"
PLUGIN = ADAPTER / "PluginEntry.cs"
CATALOG = ADAPTER / "Services" / "StartCenterCommandCatalog.cs"
STATE = ADAPTER / "Services" / "StartCenterUserStateStore.cs"
WALL_QUANTITY = ADAPTER / "WallQuantityCommands.cs"
REFERENCE_SEARCH = ADAPTER / "ReferenceSearchCommands.cs"
QUANTITY_SETTINGS_HEALTH = ADAPTER / "QuantitySettingsDiagnosticExportCommands.cs"
QUANTITY_RULE_CREATE = ADAPTER / "QuantityRuleCreateCommands.cs"


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
    commands, host, panel, plugin, catalog, state = map(
        read, (COMMANDS, HOST, PANEL, PLUGIN, CATALOG, STATE))
    wall_quantity, reference_search, quantity_settings_health, quantity_rule_create = map(
        read, (WALL_QUANTITY, REFERENCE_SEARCH, QUANTITY_SETTINGS_HEALTH, QUANTITY_RULE_CREATE))

    # QS3DSTART must now route exclusively to the native in-BricsCAD PaletteSet host.
    require(commands, '[CommandMethod("QS3DSTART", CommandFlags.Modal)]', "QS3DSTART registration")
    require(commands, "StartCenterPaletteCoordinator.Show();", "embedded Start Center dispatch")
    require(commands, 'Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(', "command diagnostic")
    require(commands, '"\\nQS3DSTART error: " + ex.Message', "command diagnostic message")
    require(commands, "Never let optional Start Center diagnostics escape the command failure boundary.", "command diagnostic containment")
    forbid(commands, "Application.ShowModelessWindow", "Start Center command")
    forbid(commands, "new StartCenterWindow", "Start Center command")
    forbid(commands, "new BltStartCenterWindow", "Start Center command")
    forbid(commands, "Process.Start", "Start Center command")

    # Native palette ownership, lifecycle and active-document refresh remain fail-soft.
    require(host, "private static PaletteSet? _palette;", "native palette ownership")
    require(host, "private static BltStartCenterPanel? _panel;", "embedded panel ownership")
    require(host, 'new PaletteSet("BLT3D — Khởi đầu", StartCenterGuid)', "native PaletteSet creation")
    require(host, "DockEnabled = DockSides.Left | DockSides.Right", "native docking capability")
    require(host, "Dock = DockSides.Left", "default docking")
    require(host, "MinimumSize = new DrawingSize(720, 480)", "palette minimum size")
    require(host, "_palette.DeviceIndependentSize = new WpfSize(1040, 680);", "palette initial size")
    require(host, '_palette.AddVisual("Khởi đầu", _panel, true);', "embedded WPF visual")
    require(host, "Application.DocumentManager.DocumentActivated += OnDocumentActivated;", "active-DWG refresh subscription")
    require(host, "Application.DocumentManager.DocumentActivated -= OnDocumentActivated;", "active-DWG refresh unsubscription")
    require(host, "if (_documentActivatedSubscribed) return;", "idempotent activation subscription")
    require(host, "if (!_documentActivatedSubscribed) return;", "idempotent activation unsubscription")
    require(host, "private static void OnDocumentActivated(object sender, DocumentCollectionEventArgs e)", "activation handler")
    require(host, "_panel.RefreshFromActiveDocument();", "active-DWG refresh callback")
    require(host, 'e.Document?.Editor.WriteMessage("\\nQS3DSTART refresh warning: " + ex.Message);', "activation refresh diagnostic")
    require(host, "public static void Dispose()", "palette lifecycle cleanup")
    require(host, "try { palette.Dispose(); }", "native palette dispose")
    require(host, "Dispose();\n                throw;", "failed-create rollback")
    forbid(host, "ShowModelessWindow", "native palette host")
    forbid(host, "System.Diagnostics.Process", "native palette host")

    require(plugin, "TryCleanup(StartCenterPaletteCoordinator.Dispose);", "plugin lifecycle cleanup")

    # Embedded BLT3D surface keeps project reads non-creating and project/file actions direct.
    require(panel, "internal sealed class BltStartCenterPanel : UserControl", "embedded Start Center panel")
    require(panel, 'Text = "BLT3D"', "BLT3D brand")
    require(panel, 'Text = "QUY TRÌNH NHANH"', "quick workflow")
    require(panel, 'Text = "DỰ ÁN GẦN ĐÂY"', "recent projects")
    require(panel, 'Text = "Nhấp vào dự án để mở trực tiếp và bắt đầu làm việc"', "recent-project help")
    require(panel, "Application.DocumentManager.MdiActiveDocument", "click-time active document resolution")
    require(panel, "ProjectContextCoordinator.TryGetReadOnly", "non-creating project dashboard")
    require(panel, "StartCenterUserStateStore.TryNormalizeDwgPath", "recent DWG normalization")
    require(panel, "StartCenterUserStateStore.RecordProject", "recent-project recording")
    require(panel, "StartCenterUserStateStore.GetSnapshot().RecentProjects", "recent-project state")
    require(panel, '"Tạo dự án mới"', "new-project action")
    require(panel, "ProjectFileUiService.CreateNewDrawing", "new-project dispatch")
    require(panel, '"Mở tệp dự án..."', "open-project action")
    require(panel, "ProjectFileUiService.OpenProjectFromPicker", "open-project dispatch")
    require(panel, "ProjectFileUiService.SaveCurrentProject", "save dispatch")
    require(panel, "ProjectFileUiService.SaveCurrentProjectAs", "save-as dispatch")
    require(panel, "File.Exists(normalized)", "recent DWG existence guard")
    require(panel, "Application.DocumentManager.Open(normalized, false)", "recent DWG open")
    require(panel, "StartCenterPaletteCoordinator.Hide();", "workspace navigation palette hide")
    require(panel, "new Commands().ShowWorkspace();", "model workspace navigation")
    require(panel, "new Commands().ShowQuantitySummary();", "BQ navigation")
    require(panel, "button.Click += (_, __) => RunUiAction(action);", "direct action click")
    require(panel, "button.Click += (_, __) => OpenRecentProject(recent);", "recent project click")
    forbid(panel, "ProjectContextCoordinator.GetOrCreate", "Start Center read-only dashboard")
    forbid(panel, "SendStringToExecute", "embedded Start Center direct project actions")
    forbid(panel, " : Window", "embedded Start Center panel")
    forbid(panel, "ShowModelessWindow", "embedded Start Center panel")
    forbid(panel, "System.Diagnostics.Process", "embedded Start Center panel")

    # User-state safety remains bounded, corruption-tolerant and fail-soft.
    for token in (
        "Path.GetFullPath",
        "Path.IsPathRooted",
        '".dwg"',
        "StringComparer.OrdinalIgnoreCase",
        "Convert.ToBase64String",
        "File.Replace",
        "MaxRecentProjects",
        "StartCenterCommandCatalog.TryGet",
    ):
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

    # Keep launcher catalog bounded and registration-backed even though the compact embedded
    # home surface does not render the full legacy launcher UI itself.
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
    for command in (
        "QS3D",
        "QS3DFAMILIES",
        "QS3DDRAWWALL",
        "QS3DDRAWBEAM",
        "QS3DDRAWCOLUMN",
        "QS3DDRAWSLAB",
        "QS3DDRAWDOOR",
        "QS3DDRAWWINDOW",
        "QS3DCREATESIMILAR",
        "QS3DWALLQTY",
        "QS3DBQ",
        "QS3DED2",
        "QS3DREBARHEALTHALL",
        "QS3DRULEPREVIEW",
        "QS3DREGENPREVIEW",
        "QS3DHEALTHALL",
        "QS3DDIAGSUMMARY",
        "QS3DQSETTINGSHEALTHEXPORT",
        "QS3DRULECREATE",
        "QS3DREFSEARCH",
        "QS3DRELEASECHECK",
        "QS3DSAVE",
        "QS3DRELOAD",
    ):
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

    print("PASS: embedded Start Center is native-PaletteSet-hosted, registration-backed, active-DWG-aware, lifecycle-fail-soft, project-read-only, direct-action, state-bounded, corruption-tolerant, search-bounded and catalog-registration-backed.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("ERROR:", exc)
        raise SystemExit(1)
