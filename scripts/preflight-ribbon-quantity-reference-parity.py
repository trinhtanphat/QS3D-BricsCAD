#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
AUGMENTER = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/QuantityReferenceRibbonAugmenter.cs"
ICON_FACTORY = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonIconFactory.cs"
COORDINATOR = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs"
PLUGIN = ROOT / "src/QS3D.BricsCAD.V25/PluginEntry.cs"
SETTINGS_COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/QuantitySettingsCommands.cs"
ENGINE2_COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/QuantityEngine2Commands.cs"
CORE_COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/Commands.cs"
INSIGHT_COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/QuantityInsightCommands.cs"
REVIEW_COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/ReviewCommands.cs"
CUSTOMER_EXCEL_COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/CustomerExcelCommands.cs"
CAD_TO_EXCEL_COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/CadToExcelCommands.cs"
TEMPLATE_EXCEL_COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/ExcelTemplateCommands.cs"
errors = []

for path in (
    AUGMENTER,
    ICON_FACTORY,
    COORDINATOR,
    PLUGIN,
    SETTINGS_COMMANDS,
    ENGINE2_COMMANDS,
    CORE_COMMANDS,
    INSIGHT_COMMANDS,
    REVIEW_COMMANDS,
    CUSTOMER_EXCEL_COMMANDS,
    CAD_TO_EXCEL_COMMANDS,
    TEMPLATE_EXCEL_COMMANDS,
):
    if not path.is_file():
        errors.append("missing quantity Ribbon parity source: " + str(path.relative_to(ROOT)))

button_specs = (
    ("QS3D_QTY_BLT_SETTINGS", "Cài đặt\\ntính toán", "QS3DQUANTITYSETTINGS", "QuantitySettings"),
    ("QS3D_QTY_BLT_CALCULATE", "Tính khối lượng\\n(Engine2)", "QS3DQUANTITYENGINE2", "QuantityCalculate"),
    ("QS3D_QTY_BLT_EXPORT", "Xuất\\nExcel", "QS3DEXCEL", "QuantityExport"),
    ("QS3D_QTY_BLT_TEMPLATE_EXPORT", "Xuất theo\\nmẫu", "QS3DEXCELTEMPLATE", "QuantityExport"),
    ("QS3D_QTY_BLT_VIEW", "Xem khối\\nlượng", "QS3DBQ", "QuantityView"),
    ("QS3D_QTY_BLT_EXPLAIN", "Diễn\\ngiải", "QS3DQUANTITYINSIGHT", "QuantityExplain"),
    ("QS3D_QTY_BLT_COMPARE", "Excel →\\nCAD", "QS3DEXCELTRACE", "QuantityCompare"),
    ("QS3D_QTY_BLT_CAD_TO_EXCEL", "CAD →\\nExcel", "QS3DCADTOEXCEL", "QuantityCompare"),
)

legacy_panel_ids = (
    "QS3D_QTY_EXCEL_PANEL_SOURCE",
    "QS3D_QTY_OPENINGS_PANEL_SOURCE",
    "QS3D_QTY_REBAR_SCHEDULE_PANEL_SOURCE",
    "QS3D_QTY_REBAR_3D_PANEL_SOURCE",
    "QS3D_QTY_REBAR_HEALTH_PANEL_SOURCE",
    "QS3D_QTY_REFERENCE_PANEL_SOURCE",
    "QS3D_QTY_PANEL_SOURCE",
)

if AUGMENTER.is_file():
    text = AUGMENTER.read_text(encoding="utf-8")
    required = (
        'private const string TabId = "QS3D_QTY";',
        'private const string SettingsPanelSourceId = "QS3D_QTY_SETTINGS_PANEL_SOURCE";',
        'private const string QuantityPanelSourceId = "QS3D_QTY_QUANTITY_PANEL_SOURCE";',
        'RemoveOwnedPanel(panels, sourceId);',
        'AddPanel(panels, SettingsPanelSourceId, "Cài đặt", SettingsButtons);',
        'AddPanel(panels, QuantityPanelSourceId, "Khối lượng", QuantityButtons);',
        'SetProperty(button, "ShowText", true);',
        'SetProperty(button, "ShowImage", true);',
        'SetProperty(button, "CommandParameter", spec.Command);',
        'SetProperty(button, "CommandHandler", new CommandHandler());',
        'SetEnumProperty(button, "Size", "Large");',
        'SetProperty(button, "Image", RibbonIconFactory.Create(spec.Icon, 16));',
        'SetProperty(button, "LargeImage", RibbonIconFactory.Create(spec.Icon, 32));',
        '_initialized = false;',
        'BltQuantityIconPolisher.Reset();',
    )
    for needle in required:
        if needle not in text:
            errors.append("QuantityReferenceRibbonAugmenter missing customer quantity reconciliation contract: " + needle)

    if text.count("AddPanel(panels,") != 2:
        errors.append("quantity layout must add exactly two panels")

    for panel_id in legacy_panel_ids:
        if text.count(f'"{panel_id}"') != 1:
            errors.append("legacy QS3D quantity panel must be owned exactly once for deterministic removal: " + panel_id)

    for button_id, label, command, icon in button_specs:
        if text.count(f'"{button_id}"') != 1:
            errors.append("expected exactly one quantity button id: " + button_id)
        if text.count(f'"{label}"') != 1:
            errors.append("expected exactly one quantity button label: " + label)

    for icon in sorted({spec[3] for spec in button_specs}):
        expected_count = sum(1 for spec in button_specs if spec[3] == icon)
        actual_count = text.count(f"RibbonIconKind.{icon}")
        if actual_count != expected_count:
            errors.append(
                f"expected {expected_count} quantity button icon binding(s) for {icon}, found {actual_count}"
            )

    expected_command_counts = {
        "QS3DQUANTITYSETTINGS": 1,
        "QS3DQUANTITYENGINE2": 1,
        "QS3DREGEN": 0,
        "QS3DEXCEL": 1,
        "QS3DEXCELTEMPLATE": 1,
        "QS3DBQ": 1,
        "QS3DQUANTITYINSIGHT": 1,
        "QS3DEXCELTRACE": 1,
        "QS3DCADTOEXCEL": 1,
        "QS3DED2": 0,
        "QS3DREVDIFF": 0,
    }
    for command, expected_count in expected_command_counts.items():
        actual_count = text.count(f'"{command}"')
        if actual_count != expected_count:
            errors.append(
                f"expected {expected_count} quantity Ribbon binding(s) for {command}, found {actual_count}"
            )

    if '"QS3D_QTY_BLT_EXPLAIN",\n                "Diễn\\ngiải",\n                "QS3DBQ"' in text:
        errors.append("Diễn giải must not reuse the Xem khối lượng QS3DBQ route")

    forbidden_button_ids = (
        "QS3D_QTY_REF_SETTINGS",
        "QS3D_QTY_REF_CALCULATE",
        "QS3D_QTY_REF_ED2",
        "QS3D_QTY_REF_WALL",
        "QS3D_QTY_REF_EXCELLOCATE",
    )
    for button_id in forbidden_button_ids:
        if button_id in text:
            errors.append("stale additive quantity-reference button survived replacement: " + button_id)

if ICON_FACTORY.is_file():
    text = ICON_FACTORY.read_text(encoding="utf-8")
    for icon in sorted({spec[3] for spec in button_specs}):
        if text.count(f"case RibbonIconKind.{icon}:") != 1:
            errors.append("RibbonIconFactory must render exactly one quantity icon case: " + icon)
        enum_count = text.count(f"        {icon},") + text.count(f"        {icon}\n")
        if enum_count != 1:
            errors.append("RibbonIconKind must declare exactly one quantity icon: " + icon)

if COORDINATOR.is_file():
    text = COORDINATOR.read_text(encoding="utf-8")
    bootstrap_pos = text.find("RibbonBootstrapper.TryInitialize()")
    quick_pos = text.find("QuickWorkflowRibbonAugmenter.TryInitialize()")
    raft_pos = text.find("RaftFoundationRibbonAugmenter.TryInitialize()")
    quantity_pos = text.find("QuantityReferenceRibbonAugmenter.TryInitialize()")
    update_pos = text.find("UpdateRibbonAugmenter.TryInitialize()")
    if min(bootstrap_pos, quick_pos, raft_pos, quantity_pos, update_pos) < 0:
        errors.append("RibbonInitializationCoordinator is missing a required quantity initialization stage")
    elif not bootstrap_pos < quick_pos < raft_pos < quantity_pos < update_pos:
        errors.append(
            "QuantityReferenceRibbonAugmenter must run after canonical/quick/raft setup and before Update augmentation"
        )

if PLUGIN.is_file():
    text = PLUGIN.read_text(encoding="utf-8")
    for needle in (
        "RibbonInitializationCoordinator.Start();",
        "TryCleanup(QuantityReferenceRibbonAugmenter.Reset);",
        "TryCleanup(RibbonBootstrapper.Reset);",
    ):
        if needle not in text:
            errors.append("PluginEntry missing coordinated Quantity reference lifecycle hook: " + needle)

behavior_contracts = (
    (
        SETTINGS_COMMANDS,
        "Cài đặt tính toán",
        (
            '[CommandMethod("QS3DQUANTITYSETTINGS", CommandFlags.Modal)]',
            "ShowQuantitySettings();",
            "Application.ShowModalWindow(window);",
        ),
    ),
    (
        ENGINE2_COMMANDS,
        "Tính khối lượng (Engine2)",
        (
            '[CommandMethod("QS3DQUANTITYENGINE2", CommandFlags.Modal)]',
            'ExistingProjectMutationContext.TryGet(document, out var project)',
            'QuantityCalculationResultWindow.ShowNoProject(noProjectMessage)',
            'PaletteCoordinator.ShowBimWorkspace()',
            ".RegenerateDirty(project);",
            "ProjectQuantityReportBuilder.Group(project);",
            "QuantityEngine2Summary.Build(rows, regenerated);",
            "QuantityCalculationResultWindow.ShowSuccess(summary);",
        ),
    ),
    (
        CUSTOMER_EXCEL_COMMANDS,
        "Xuất Excel",
        (
            '[CommandMethod("QS3DEXCEL", CommandFlags.UsePickSet)]',
            "ProjectStateSnapshot.CreateDetachedCopy(project)",
            "ProjectQuantityReportBuilder.Detail(preview",
            "ProjectQuantityReportBuilder.Group(preview",
            "EnsureHandlesAreLive(document, details)",
            "QsCustomerWorkbookExporter.Export(dialog.FileName, details, summary);",
        ),
    ),
    (
        TEMPLATE_EXCEL_COMMANDS,
        "Xuất theo mẫu",
        (
            '[CommandMethod("QS3DEXCELTEMPLATE", CommandFlags.UsePickSet)]',
            'DrawingUnitWorkflow.EnsureResolved(document, "QS3DEXCELTEMPLATE")',
            "ProjectStateSnapshot.CreateDetachedCopy(currentProject)",
            "ProjectQuantityReportBuilder.Detail(preview",
            "ProjectQuantityReportBuilder.Group(preview",
            "EnsureHandlesAreLive(document, rows)",
            "QsWorkbookTemplateExporter.Export(",
        ),
    ),
    (
        CORE_COMMANDS,
        "Xem khối lượng",
        (
            '[CommandMethod("QS3DBQ", CommandFlags.UsePickSet)]',
            "new QuantitySummaryWindow(doc, rows, locate, recalculate)",
        ),
    ),
    (
        INSIGHT_COMMANDS,
        "Diễn giải",
        (
            '[CommandMethod("QS3DQUANTITYINSIGHT", CommandFlags.UsePickSet)]',
            "PaletteCoordinator.SetInspection(snapshots);",
            "PaletteCoordinator.ShowQuantityInsight();",
        ),
    ),
    (
        CUSTOMER_EXCEL_COMMANDS,
        "Excel → CAD",
        (
            '[CommandMethod("QS3DEXCELTRACE", CommandFlags.Modal)]',
            "QsCustomerWorkbookTraceReader.Read(dialog.FileName, sheet, row.Value);",
            "ExcelLocateResolutionService.ResolveCustomerTrace(document, project, trace);",
            "document.Editor.SetImpliedSelection(resolution.ObjectIds.ToArray());",
            'SendStringToExecute("QS3DZOOMSELECTED "',
        ),
    ),
    (
        CAD_TO_EXCEL_COMMANDS,
        "CAD → Excel",
        (
            '[CommandMethod("QS3DCADTOEXCEL", CommandFlags.UsePickSet)]',
            "ExcelModelRowActivationService.TryFindActiveWorkbookRow(",
            "QsCustomerWorkbookTraceReader.Read(",
            "XlsxHandleReader.ReadHandleLookup(",
            "ExcelLocateResolutionService.ResolveCustomerTrace(document, project, trace);",
            "ExcelLocateResolutionService.ResolveModern(document, project, lookup);",
            "ExcelModelRowActivationService.TryActivateValidatedRow(candidate, out var activationError)",
        ),
    ),
)

for path, action_name, needles in behavior_contracts:
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(f"{action_name} behavior contract missing: {needle}")

legacy_compatibility_contracts = (
    (
        CORE_COMMANDS,
        "legacy ED2 export",
        (
            '[CommandMethod("QS3DED2", CommandFlags.UsePickSet)]',
            "XlsxQuantityExporter.ExportEd2(dialog.FileName, details, summary);",
        ),
    ),
    (
        REVIEW_COMMANDS,
        "legacy revision comparison",
        (
            '[CommandMethod("QS3DREVDIFF", CommandFlags.Modal)]',
            "new QuantityRevisionReport().Build(before, after);",
            "new RevisionWindow(doc, before, after, rows, locate)",
        ),
    ),
)
for path, action_name, needles in legacy_compatibility_contracts:
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(f"{action_name} compatibility contract missing: {needle}")

if ENGINE2_COMMANDS.is_file():
    engine2 = ENGINE2_COMMANDS.read_text(encoding="utf-8")
    if 'ExistingProjectMutationContext.Require(document, "Tính khối lượng (Engine2)")' in engine2:
        errors.append("Tính khối lượng (Engine2) must not use the generic missing-project mutation exception")
    if "ProjectContextCoordinator.GetOrCreate" in engine2:
        errors.append("Tính khối lượng (Engine2) must not silently create a QS3D project")
    try_get_pos = engine2.find('ExistingProjectMutationContext.TryGet(document, out var project)')
    no_project_pos = engine2.find('QuantityCalculationResultWindow.ShowNoProject(noProjectMessage)', try_get_pos)
    regenerate_pos = engine2.find('.RegenerateDirty(project)', no_project_pos)
    if not (0 <= try_get_pos < no_project_pos < regenerate_pos):
        errors.append("Tính khối lượng (Engine2) must handle missing-project UX before existing-project regeneration")

adapter_root = ROOT / "src/QS3D.BricsCAD.V25"
if adapter_root.is_dir():
    command_source = "\n".join(
        path.read_text(encoding="utf-8")
        for path in adapter_root.rglob("*.cs")
        if path.is_file() and path != AUGMENTER
    )
    for command in sorted({spec[2] for spec in button_specs}):
        marker = f'[CommandMethod("{command}"'
        if marker not in command_source:
            errors.append("quantity Ribbon points to an unregistered adapter command: " + command)

print("QS3D quantity Ribbon customer-workflow parity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print(
    "PASS: QS3D_QTY is reconciled to the two-panel customer quantity workflow with eight large icon buttons, "
    "deterministic removal of legacy QS3D quantity panels, registered QS3DEXCEL/QS3DEXCELTEMPLATE/QS3DEXCELTRACE/QS3DCADTOEXCEL routing, "
    "preserved ED2/revision compatibility commands, coordinated initialization, and contained teardown."
)
