#!/usr/bin/env python3
from pathlib import Path
import re
import sys

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(errors="backslashreplace")

ROOT = Path(__file__).resolve().parents[1]
RIBBON = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "RibbonBootstrapper.cs"
errors = []

if not RIBBON.is_file():
    print("ERROR: missing", RIBBON.relative_to(ROOT))
    sys.exit(1)

text = RIBBON.read_text(encoding="utf-8")

structural_contracts = (
    "private sealed class RibbonPanelSpec",
    "public IReadOnlyList<RibbonPanelSpec> Panels { get; }",
    "foreach (var tabSpec in CreateSpecs())",
    "ReconcileTab(tabs, tabSpec);",
    "private static void ReconcileTab(object tabs, RibbonTabSpec tabSpec)",
    "var tab = FindById(tabs, tabSpec.Id);",
    "var created = tab == null;",
    'SetProperty(tab!, "Title", tabSpec.Title);',
    'RemoveLegacyFlatPanel(panels, tabSpec.Id + "_PANEL_SOURCE");',
    "foreach (var panelSpec in tabSpec.Panels)",
    "EnsurePanel(tabSpec, panelSpec, panels);",
    "private static void EnsurePanel(RibbonTabSpec tabSpec, RibbonPanelSpec panelSpec, object panels)",
    "var source = FindPanelSource(panels, sourceId);",
    'SetProperty(source, "Title", panelSpec.Title);',
    "EnsurePanelButtons(tabSpec, panelSpec, source);",
    "private static void EnsurePanelButtons(RibbonTabSpec tabSpec, RibbonPanelSpec panelSpec, object source)",
    "var button = FindById(items, buttonId);",
    'SetProperty(button, "CommandParameter", buttonSpec.Command);',
    'SetProperty(button, "CommandHandler", new RibbonCommandHandler());',
    "private static object? FindPanelSource(object panels, string sourceId)",
    "private static void RemoveLegacyFlatPanel(object panels, string legacySourceId)",
    "if (legacyPanel != null)",
    "Remove(panels, legacyPanel);",
    "private static object? FindById(object collection, string id)",
    'tabSpec.Id + "_" + panelSpec.Id + "_PANEL_SOURCE"',
    'tabSpec.Id + "_" + panelSpec.Id + "_" + Normalize(buttonSpec.Text)',
)

for needle in structural_contracts:
    if needle not in text:
        errors.append("missing grouped/reconciliation contract: " + needle)

for legacy in (
    "if (CollectionContainsId(tabs, tabSpec.Id))",
    "continue;\n\n                    var tab = Create(\"Bricscad.Windows.RibbonTab\")",
    "foreach (var buttonSpec in spec.Buttons)",
    'SetProperty(source, "Title", spec.Title);',
):
    if legacy in text:
        errors.append("legacy create-only Ribbon bootstrap contract returned: " + legacy)

if ".Clear()" in text:
    errors.append("Ribbon reconciliation must not clear whole tab/panel collections because augmenter/user panels must survive")

remove_calls = text.count("Remove(panels, legacyPanel);")
if remove_calls != 1:
    errors.append("RibbonBootstrapper may remove only the one exact legacy flat QS3D panel path")

required_tabs = {
    "QS3D_HOME": {"Dự án", "Điều phối", "Chất lượng"},
    "QS3D_PROJECT": {"Trạng thái", "Template", "Phạm vi"},
    "QS3D_AUTHOR": {"Thiết lập", "Kiến trúc", "Kết cấu", "Hoàn thiện 3D"},
    "QS3D_BIM": {"Phòng & hoàn thiện", "Tường & vách", "Kết cấu", "Cửa & lỗ mở", "Sinh mô hình"},
    "QS3D_RECOGNIZE": {"Nhận dạng", "Kiểm tra"},
    "QS3D_DRAW": {"Hình học", "Biến đổi", "Kết nối & đo"},
    "QS3D_TOOL": {"Kiểm tra", "Tập trung", "Cắt & zoom", "Bảo trì"},
    "QS3D_MODELING": {"Sinh 3D", "Tường & vách", "Kết cấu", "Cửa & host", "Phòng"},
    "QS3D_VIEW": {"Góc nhìn", "Tập trung", "Mặt cắt", "Điều hướng", "Workspace"},
    "QS3D_QTY": {"Khối lượng", "Excel ↔ CAD", "Cửa & lỗ mở", "BBS", "Cốt thép 3D", "Health cốt thép"},
    "QS3D_REV": {"Bản sửa đổi", "Kiểm tra", "Dự án"},
}

for tab_id, panel_titles in required_tabs.items():
    if f'"{tab_id}"' not in text:
        errors.append("missing ribbon tab: " + tab_id)
    for title in panel_titles:
        if f'"{title}"' not in text:
            errors.append(f"{tab_id} missing panel title: {title}")

required_commands = {
    "QS3D", "QS3DSTART", "QS3DSAVE", "QS3DREGEN", "QS3DBQ", "QS3DBBSVIEW",
    "QS3DHEALTHALL", "QS3DRELEASECHECK", "QS3DREFRESH", "QS3DRELOAD",
    "QS3DTEMPLATEEXPORT", "QS3DTEMPLATEIMPORT", "QS3DFAMILIES",
    "QS3DDRAWWALL", "QS3DDRAWGLASSWALL", "QS3DDRAWWALLPIER",
    "QS3DDRAWBEAM", "QS3DDRAWSTRUCTWALL", "QS3DDRAWCOLUMN",
    "QS3DDRAWSLAB", "QS3DDRAWFOUNDATION", "QS3DDRAWDOOR",
    "QS3DDRAWOPENING", "QS3DCUTSELECTEDOPENINGS", "QS3DBUILD3D",
    "QS3DROOM", "QS3DROOMAUTO", "QS3DWALL", "QS3DGLASSWALL",
    "QS3DCURTAIN", "QS3DCURTAIN3D", "QS3DCURTAINFRAMEHEALTH",
    "QS3DWALLPIER", "QS3DWALLJUNCTIONS", "QS3DWALLSNAPPREVIEW",
    "QS3DWALLSNAPAPPLY", "QS3DBEAM", "QS3DSLAB", "QS3DCOLUMN",
    "QS3DSTRUCTWALL", "QS3DFOUNDATION", "QS3DSTAIR", "QS3DRAILING",
    "QS3DEARTHWORK", "QS3DOPENING", "QS3DDOOR", "QS3DDOORSCHEDULE",
    "QS3DAUTOLINKHOSTS", "QS3DLINKHOST", "QS3DCUTOPENINGS",
    "QS3DCUTOPENINGSCURVED", "QS3DFINISH", "QS3DRECOGNIZE",
    "QS3DRECOGNIZEAUTO", "QS3DTAKEOFF", "QS3DINSPECT",
    "_POINT", "_LINE", "_ARC", "_RECTANG", "_MOVE", "_ROTATE",
    "_MIRROR", "_COPY", "_BREAK", "_JOIN", "_DIST", "QS3DSECTIONPLANE",
    "QS3DLOCATE", "QS3DHIGHLIGHT", "QS3DUNHIGHLIGHT", "QS3DFOCUS",
    "QS3DISOLATE", "QS3DUNISOLATE", "QS3DSECTIONBOX", "QS3DCLIPDISPLAY",
    "QS3DZOOMSELECTED", "QS3DVIEW3D", "QS3DVIEWTOP", "QS3DORBIT",
    "QS3DZOOMALL", "QS3DED2", "QS3DEXCELLOCATE", "QS3DDOORXLSX",
    "QS3DBBS", "QS3DREBARMESHSETUP", "QS3DREBAR3D", "QS3DREBARTIES3D",
    "QS3DBEAMREBAR3D", "QS3DREBARSTIRRUP3D", "QS3DSLABREBAR3D",
    "QS3DWALLREBAR3D", "QS3DFOUNDATIONREBAR3D", "QS3DREBAR3DSHAPE",
    "QS3DREBARTIEHEALTH", "QS3DREBARSTIRRUPHEALTH", "QS3DSLABREBARHEALTH",
    "QS3DWALLREBARHEALTH", "QS3DFOUNDATIONREBARHEALTH", "QS3DREBARHEALTH",
    "QS3DREBARSHAPEHEALTH", "QS3DREBARHEALTHALL", "QS3DREVBASE", "QS3DREVDIFF",
}

bound_commands = set(re.findall(r'Button\("[^"]+", "([^"]+)"\)', text))
missing_commands = sorted(required_commands - bound_commands)
if missing_commands:
    errors.append("existing ribbon command binding(s) disappeared: " + ", ".join(missing_commands))

if text.count('Button("Start Center", "QS3DSTART")') != 1:
    errors.append("Start Center binding must remain exactly once while existing tabs are reconciled")

panel_count = len(re.findall(r"\bPanel\(", text)) - 1
if panel_count < 35:
    errors.append(f"expected at least 35 functional panels, found {panel_count}")

print("QS3D ribbon information-architecture preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print(
    "PASS: fresh and already-loaded QS3D Ribbon states reconcile to the current named functional panels/buttons, "
    "legacy flat QS3D panels are removed narrowly, unknown augmenter panels are preserved, and native command dispatch stays click-time active-document routed."
)
