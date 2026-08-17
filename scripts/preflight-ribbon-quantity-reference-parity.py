#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
AUGMENTER = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/QuantityReferenceRibbonAugmenter.cs"
ICON_FACTORY = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonIconFactory.cs"
COORDINATOR = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs"
PLUGIN = ROOT / "src/QS3D.BricsCAD.V25/PluginEntry.cs"
SETTINGS_COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/QuantitySettingsCommands.cs"
CORE_COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/Commands.cs"
INSIGHT_COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/QuantityInsightCommands.cs"
REVIEW_COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/ReviewCommands.cs"
errors = []

for path in (
    AUGMENTER,
    ICON_FACTORY,
    COORDINATOR,
    PLUGIN,
    SETTINGS_COMMANDS,
    CORE_COMMANDS,
    INSIGHT_COMMANDS,
    REVIEW_COMMANDS,
):
    if not path.is_file():
        errors.append("missing quantity Ribbon parity source: " + str(path.relative_to(ROOT)))

button_specs = (
    ("QS3D_QTY_BLT_SETTINGS", "Cài đặt\\ntính toán", "QS3DQUANTITYSETTINGS", "QuantitySettings"),
    ("QS3D_QTY_BLT_CALCULATE", "Tính khối lượng\\n(Engine2)", "QS3DREGEN", "QuantityCalculate"),
    ("QS3D_QTY_BLT_EXPORT", "Xuất\\n.blte2", "QS3DED2", "QuantityExport"),
    ("QS3D_QTY_BLT_VIEW", "Xem khối\\nlượng", "QS3DBQ", "QuantityView"),
    ("QS3D_QTY_BLT_EXPLAIN", "Diễn\\ngiải", "QS3DQUANTITYINSIGHT", "QuantityExplain"),
    ("QS3D_QTY_BLT_COMPARE", "Đối chiếu\\nCũ/Mới", "QS3DREVDIFF", "QuantityCompare"),
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
            errors.append("QuantityReferenceRibbonAugmenter missing BLT3D reconciliation contract: " + needle)

    if text.count("AddPanel(panels,") != 2:
        errors.append("quantity BLT3D layout must add exactly two panels")

    for panel_id in legacy_panel_ids:
        if text.count(f'"{panel_id}"') != 1:
            errors.append("legacy QS3D quantity panel must be owned exactly once for deterministic removal: " + panel_id)

    for button_id, label, command, icon in button_specs:
        if text.count(f'"{button_id}"') != 1:
            errors.append("expected exactly one BLT3D quantity button id: " + button_id)
        if text.count(f'"{label}"') != 1:
            errors.append("expected exactly one BLT3D quantity button label: " + label)
        if text.count(f"RibbonIconKind.{icon}") != 1:
            errors.append("expected exactly one BLT3D quantity button icon binding: " + icon)

    expected_command_counts = {
        "QS3DQUANTITYSETTINGS": 1,
        "QS3DREGEN": 1,
        "QS3DED2": 1,
        "QS3DBQ": 1,
        "QS3DQUANTITYINSIGHT": 1,
        "QS3DREVDIFF": 1,
    }
    for command, expected_count in expected_command_counts.items():
        actual_count = text.count(f'"{command}"')
        if actual_count != expected_count:
            errors.append(
                f"expected {expected_count} BLT3D quantity command binding(s) for {command}, found {actual_count}"
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
            errors.append("stale additive quantity-reference button survived BLT3D replacement: " + button_id)

if ICON_FACTORY.is_file():
    text = ICON_FACTORY.read_text(encoding="utf-8")
    for _, _, _, icon in button_specs:
        if text.count(f"case RibbonIconKind.{icon}:") != 1:
            errors.append("RibbonIconFactory must render exactly one BLT3D quantity icon case: " + icon)
        enum_count = text.count(f"        {icon},") + text.count(f"        {icon}\n")
        if enum_count != 1:
            errors.append("RibbonIconKind must declare exactly one BLT3D quantity icon: " + icon)

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

# Pin the real behavior behind every visible BLT3D-style quantity action. A registered
# command name alone is not enough: this catches accidental visual aliases such as the
# old Diễn giải -> QS3DBQ routing bug.
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
        CORE_COMMANDS,
        "Tính khối lượng (Engine2)",
        (
            '[CommandMethod("QS3DREGEN", CommandFlags.Modal)]',
            "var count = RegenerateProject(project);",
        ),
    ),
    (
        CORE_COMMANDS,
        "Xuất .blte2 / ED2 export route",
        (
            '[CommandMethod("QS3DED2", CommandFlags.UsePickSet)]',
            "XlsxQuantityExporter.ExportEd2(dialog.FileName, details, summary);",
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
        REVIEW_COMMANDS,
        "Đối chiếu Cũ/Mới",
        (
            '[CommandMethod("QS3DREVDIFF", CommandFlags.Modal)]',
            "new QuantityRevisionReport().Build(before, after);",
            "new RevisionWindow(doc, before, after, rows, locate)",
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
            errors.append("BLT3D quantity Ribbon points to an unregistered adapter command: " + command)

print("QS3D ĐỊNH LƯỢNG Ribbon BLT3D reference-parity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print(
    "PASS: QS3D_QTY is reconciled to the BLT3D reference with exactly two groups, "
    "six large icon buttons, deterministic removal of legacy QS3D quantity panels, "
    "distinct registered command routing with pinned behavior, coordinated initialization, "
    "and contained teardown."
)
