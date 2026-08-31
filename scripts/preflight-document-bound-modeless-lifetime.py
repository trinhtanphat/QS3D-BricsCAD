#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src/QS3D.BricsCAD.V25"
UI = ADAPTER / "UI"
errors = []

files = {
    "lifetime": UI / "DocumentBoundWindowLifetime.cs",
    "native": UI / "DocumentBoundNativeLifecycleCoordinator.cs",
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
        "private readonly IntPtr _nativeDatabaseIdentity;",
        "_nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
        "database.UnmanagedObject == _nativeDatabaseIdentity",
        "if (!MatchesNativeDatabase(document))",
        "if (!MatchesNativeDatabase(e.Document)) return;",
        "private IDisposable? _nativeLifecycleSubscription;",
        "_nativeLifecycleSubscription = DocumentBoundNativeLifecycleCoordinator.Register(",
        "DetachNativeLifecycleSubscription();",
        "_window.Closed += OnWindowClosed",
        "_window.Closed -= OnWindowClosed",
        "_window.Dispatcher.CheckAccess()",
        "_window.Dispatcher.BeginInvoke(new Action(TryCloseWindowOnDispatcher))",
        "private void TryCloseWindowOnDispatcher()",
    ):
        if needle not in text["lifetime"]:
            errors.append("document-bound lifetime coordinator missing: " + needle)

    for legacy in (
        "ReferenceEquals(e.Document, _document)",
        "ReferenceEquals(document, _document)",
    ):
        if legacy in text["lifetime"]:
            errors.append("document-bound lifetime must not depend on managed Document wrapper identity: " + legacy)

    for forbidden in (
        "BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;",
        "BcadApplication.DocumentManager.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;",
        "_lifecycleDocument.BeginDocumentClose += OnBeginDocumentClose;",
        "_lifecycleDocument.CloseAborted += OnDocumentCloseAborted;",
    ):
        if forbidden in text["lifetime"]:
            errors.append("per-window lifetime must not directly own native lifecycle reactor: " + forbidden)

    for needle in (
        "private static readonly Dictionary<IntPtr, Entry> Entries",
        "BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;",
        "lifecycleDocument.BeginDocumentClose += OnBeginDocumentClose;",
        "lifecycleDocument.CloseAborted += OnDocumentCloseAborted;",
        "lifecycleDocument.BeginDocumentClose -= OnBeginDocumentClose;",
        "lifecycleDocument.CloseAborted -= OnDocumentCloseAborted;",
        "new WeakReference<Callbacks>(callbacks)",
        "return new Subscription(entry, callbacks);",
        "if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;",
    ):
        if needle not in text["native"]:
            errors.append("shared native lifecycle coordinator missing: " + needle)

    if text["native"].count("BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;") != 1:
        errors.append("shared native lifecycle coordinator must expose one global DocumentToBeDestroyed subscription site")

    # Most legacy modeless windows still keep their explicit source wrapper. Revision is hardened:
    # constructor binding remains explicit, while callbacks resolve a fresh wrapper by native DB identity.
    for key in ("recognition", "health", "bq", "bbs", "door_schedule", "room_schedule"):
        if "DocumentBoundWindowLifetime.Attach(this, _document);" not in text[key]:
            errors.append(key + " window must auto-close when its source DWG is destroyed")
    if "DocumentBoundWindowLifetime.Attach(this, document);" not in text["revision"]:
        errors.append("revision window must auto-close when its constructor-bound source DWG is destroyed")
    for needle in (
        "private readonly IntPtr _nativeDatabaseIdentity;",
        "database.UnmanagedObject == _nativeDatabaseIdentity",
        "TryGetBoundActiveDocument(out var document)",
    ):
        if needle not in text["revision"]:
            errors.append("revision window missing native-identity live-wrapper contract: " + needle)
    if "private readonly Document _document" in text["revision"]:
        errors.append("revision window must not retain a managed Document wrapper across modeless lifetime")

    for key, signature in (
        ("bq", "QuantitySummaryWindow(Document document"),
        ("bbs", "RebarScheduleWindow(Document document"),
        ("health", "ModelHealthWindow(Document document"),
        ("door_schedule", "DoorOpeningScheduleWindow(Document document"),
        ("room_schedule", "RoomFinishScheduleWindow(Document document"),
        ("revision", "RevisionWindow(Document document"),
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
    if "new RevisionWindow(doc, before, after, rows, locate)" not in text["review"]:
        errors.append("QS3DREVDIFF launcher must pass its source Document to RevisionWindow")

    manager_contracts = {
        "families": ("new FamilyManagerWindow(document)", "family_window"),
        "levels": ("new FloorLevelWindow(document)", "level_window"),
        "zones": ("new ZoneManagerWindow(document)", "zone_window"),
        "materials": ("new MaterialCatalogWindow(document, project)", "material_window"),
        "project_tools": ("new ProjectToolsWindow(document)", "project_tools_window"),
        "schedule_hub": ("new ScheduleHubWindow(document)", "schedule_hub_window"),
        "curtain_hub": ("new CurtainWallWindow(document)", "curtain_hub_window"),
    }
    legacy_publication_tracked_managers = {"families", "levels"}
    for key, (constructor, window_key) in manager_contracts.items():
        source = text[key]
        window_source = text[window_key]
        if constructor not in source:
            errors.append(key + " launcher lost its explicit source Document constructor")
        if key in legacy_publication_tracked_managers:
            if "var publishedWindow = candidate;" not in source:
                errors.append(key + " launcher must alias the exact candidate before attaching publication lifecycle")
            if "Application.ShowModelessWindow(IntPtr.Zero, publishedWindow, true);" not in source:
                errors.append(key + " launcher must show the exact publication-tracked candidate instance")
        elif key == "zones":
            for needle in (
                "private static PublishedManager? _pending;",
                "private static PublishedManager? _published;",
                "var owner = new PublishedManager(window, document);",
                "_pending = owner;",
                "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
                "if (!window.IsLoaded)",
                "if (!ReferenceEquals(_pending, owner))",
                "_pending = null;",
                "_published = owner;",
                "if (ReferenceEquals(_pending, owner)) _pending = null;",
                "if (ReferenceEquals(_published, owner)) _published = null;",
            ):
                if needle not in source:
                    errors.append("zones launcher missing pending-first exact publication lifecycle token: " + needle)
            try:
                construct = source.index("var window = new ZoneManagerWindow(document);")
                owner = source.index("var owner = new PublishedManager(window, document);", construct)
                pending = source.index("_pending = owner;", owner)
                show = source.index("Application.ShowModelessWindow(IntPtr.Zero, window, true);", pending)
                loaded = source.index("if (!window.IsLoaded)", show)
                exact = source.index("if (!ReferenceEquals(_pending, owner))", loaded)
                clear = source.index("_pending = null;", exact)
                publish = source.index("_published = owner;", clear)
                if not (construct < owner < pending < show < loaded < exact < clear < publish):
                    errors.append("zones launcher must own pending before host show and publish only after loaded/exact-owner proof")
            except ValueError as exc:
                errors.append("zones launcher publication ordering marker missing: " + str(exc))
        elif "Application.ShowModelessWindow(IntPtr.Zero, window, true);" not in source:
            errors.append(key + " launcher must show the same registered window instance")
        if "DocumentBoundWindowLifetime.Attach(window, document);" in source:
            errors.append(key + " launcher must not duplicate lifetime attachment owned by the window constructor")
        if "DocumentBoundWindowLifetime.Attach(this, _document);" not in window_source:
            errors.append(window_key + " must own its source-DWG lifetime attachment in the window constructor")

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

print("PASS: document-bound review/health/BQ/BBS/schedule/manager windows keep one source-DWG registration; Family/Level retain exact publication aliases while Zone Manager is pending-first and loaded/exact-owner proven before publication; Revision retains only stable native database identity for callbacks, native reactors stay centralized, and dynamic hubs remain active-document based.")