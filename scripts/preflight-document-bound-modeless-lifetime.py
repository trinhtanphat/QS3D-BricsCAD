#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src/QS3D.BricsCAD.V25"
UI = ADAPTER / "UI"
errors = []

files = {
    "lifetime": UI / "DocumentBoundWindowLifetime.cs",
    "recognition": UI / "RecognitionWindow.xaml.cs",
    "revision": UI / "RevisionWindow.xaml.cs",
    "health": UI / "ModelHealthWindow.xaml.cs",
    "bq": UI / "QuantitySummaryWindow.xaml.cs",
    "bbs": UI / "RebarScheduleWindow.xaml.cs",
    "door_schedule": UI / "DoorOpeningScheduleWindow.xaml.cs",
    "room_schedule": UI / "RoomFinishScheduleWindow.xaml.cs",
    "domain_hub": UI / "DomainHubWindow.xaml.cs",
    "rebar_hub": UI / "Rebar3DHubWindow.xaml.cs",
    "family_window": UI / "FamilyManagerWindow.xaml.cs",
    "level_window": UI / "FloorLevelWindow.xaml.cs",
    "zone_window": UI / "ZoneManagerWindow.xaml.cs",
    "material_window": UI / "MaterialCatalogWindow.xaml.cs",
    "project_tools_window": UI / "ProjectToolsWindow.xaml.cs",
    "schedule_hub_window": UI / "ScheduleHubWindow.xaml.cs",
    "curtain_hub_window": UI / "CurtainWallWindow.xaml.cs",
    "commands": ADAPTER / "Commands.cs",
    "review": ADAPTER / "ReviewCommands.cs",
    "families": ADAPTER / "FamilyManagerCommands.cs",
    "levels": ADAPTER / "FloorLevelCommands.cs",
    "zones": ADAPTER / "ZoneManagerCommands.cs",
    "materials": ADAPTER / "MaterialCatalogCommands.cs",
    "project_tools": ADAPTER / "ProjectToolsCommands.cs",
    "schedule_hub": ADAPTER / "ScheduleHubCommands.cs",
    "curtain_hub": ADAPTER / "CurtainWallHubCommands.cs",
}
for key, path in files.items():
    if not path.is_file():
        errors.append("missing modeless lifetime source: " + str(path.relative_to(ROOT)))

if not errors:
    text = {key: path.read_text(encoding="utf-8") for key, path in files.items()}

    for needle in (
        "DocumentToBeDestroyed += OnDocumentToBeDestroyed",
        "DocumentToBeDestroyed -= OnDocumentToBeDestroyed",
        "ReferenceEquals(e.Document, _document)",
        "_window.Closed += OnWindowClosed",
        "_window.Closed -= OnWindowClosed",
        "_window.Dispatcher.CheckAccess()",
        "_window.Dispatcher.BeginInvoke(new Action(_window.Close))",
    ):
        if needle not in text["lifetime"]:
            errors.append("document-bound lifetime coordinator missing: " + needle)

    for key in ("recognition", "revision", "health", "bq", "bbs", "door_schedule", "room_schedule"):
        if "DocumentBoundWindowLifetime.Attach(this, _document);" not in text[key]:
            errors.append(key + " window must auto-close when its source DWG is destroyed")

    for key, signature in (
        ("bq", "QuantitySummaryWindow(Document document"),
        ("bbs", "RebarScheduleWindow(Document document"),
        ("health", "ModelHealthWindow(Document document"),
        ("door_schedule", "DoorOpeningScheduleWindow(Document document"),
        ("room_schedule", "RoomFinishScheduleWindow(Document document"),
    ):
        if signature not in text[key]:
            errors.append(key + " must require an explicit source Document")

    if "public ModelHealthWindow(IReadOnlyList<ModelHealthIssue> issues" in text["health"]:
        errors.append("legacy ambient ModelHealthWindow constructor must not return")
    if "_document = BcadApplication.DocumentManager.MdiActiveDocument" in text["bq"]:
        errors.append("QuantitySummaryWindow must not capture ambient MdiActiveDocument")
    if "_document = BcadApplication.DocumentManager.MdiActiveDocument" in text["bbs"]:
        errors.append("RebarScheduleWindow must not capture ambient MdiActiveDocument")

    if "new QuantitySummaryWindow(doc, rows, locate, recalculate)" not in text["commands"]:
        errors.append("QS3DBQ launcher must pass its source Document to QuantitySummaryWindow")
    if "new RebarScheduleWindow(doc, rows, locate, fileName)" not in text["review"]:
        errors.append("QS3DBBSVIEW launcher must pass its source Document to RebarScheduleWindow")

    manager_contracts = {
        "families": ("new FamilyManagerWindow(document)", "family_window"),
        "levels": ("new FloorLevelWindow(document)", "level_window"),
        "zones": ("new ZoneManagerWindow(document)", "zone_window"),
        "materials": ("new MaterialCatalogWindow(document, project)", "material_window"),
        "project_tools": ("new ProjectToolsWindow(document)", "project_tools_window"),
        "schedule_hub": ("new ScheduleHubWindow(document)", "schedule_hub_window"),
        "curtain_hub": ("new CurtainWallWindow(document)", "curtain_hub_window"),
    }
    for key, (constructor, window_key) in manager_contracts.items():
        source = text[key]
        window_source = text[window_key]
        if constructor not in source:
            errors.append(key + " launcher lost its explicit source Document constructor")
        if "Application.ShowModelessWindow(IntPtr.Zero, window, true);" not in source:
            errors.append(key + " launcher must show the same registered window instance")
        if "DocumentBoundWindowLifetime.Attach(window, document);" in source:
            errors.append(key + " launcher must not duplicate lifetime attachment owned by the window constructor")
        if "DocumentBoundWindowLifetime.Attach(this, _document);" not in window_source:
            errors.append(window_key + " must own its source-DWG lifetime attachment in the window constructor")

    # These command hubs are intentionally active-document dynamic. Binding either hub to the
    # document that happened to be active when the hub opened would break intended multi-DWG UX.
    dynamic_hubs = {
        "domain_hub": "DomainHub",
        "rebar_hub": "Rebar3DHub",
    }
    for key, label in dynamic_hubs.items():
        source = text[key]
        if "MdiActiveDocument" not in source:
            errors.append(label + " must continue resolving the active DWG at command-click time")
        if "DocumentBoundWindowLifetime.Attach" in source:
            errors.append(label + " must remain active-document dynamic, not source-DWG-bound")
        if "private readonly Document _document" in source or "private Document _document" in source:
            errors.append(label + " must not retain a source Document across DWG switches")

print("QS3D document-bound modeless lifetime preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)

print("PASS: document-bound review/health/BQ/BBS/schedule/manager windows own one source-DWG lifetime attachment in their constructors; launchers do not duplicate it, while dynamic hubs remain active-document based.")
