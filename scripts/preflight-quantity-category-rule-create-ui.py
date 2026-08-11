#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml"
CODE = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.CategoryRuleCreation.cs"
errors = []

if not XAML.is_file():
    errors.append("missing QuantitySettingsWindow.xaml")
else:
    try:
        ET.parse(XAML)
    except ET.ParseError as ex:
        errors.append("QuantitySettingsWindow.xaml is not well-formed XML: " + str(ex))
    xaml = XAML.read_text(encoding="utf-8")
    for token in (
        'Loaded="QuantitySettingsWindow_Loaded"',
        'x:Name="MissingCategoryRuleStatusText"',
        'x:Name="MissingCategoryRuleList"',
        'ItemsSource="{Binding MissingCategoryRuleChoices}"',
        'SelectionChanged="MissingCategoryRuleSelectionChanged"',
        'x:Name="CreateCategoryRuleButton"',
        'Content="＋  Tạo quy tắc loại"',
        'Click="CreateCategoryRule_Click"',
    ):
        if token not in xaml:
            errors.append("category-rule create UI missing XAML contract: " + token)

if not CODE.is_file():
    errors.append("missing QuantitySettingsWindow.CategoryRuleCreation.cs")
else:
    code = CODE.read_text(encoding="utf-8")
    for token in (
        "MissingCategoryRuleChoices",
        "CategoryRows.CollectionChanged += QuantityRuleRows_CollectionChanged",
        "IntersectionRows.CollectionChanged += QuantityRuleRows_CollectionChanged",
        "new HashSet<int>(CategoryRows.Select(x => x.CategoryCode))",
        ".SelectMany(x => new[] { x.SourceCode, x.TargetCode })",
        ".Where(x => !categoryCodes.Contains(x))",
        "MissingCategoryRuleList.IsEnabled = MissingCategoryRuleChoices.Count > 0 && !_persistentSettingsWriteBlocked",
        "CategoryRows.Any(x => x.CategoryCode == code)",
        "!IntersectionRows.Any(x => x.SourceCode == code || x.TargetCode == code)",
        "MessageBoxButton.YesNo",
        "answer != MessageBoxResult.Yes",
        "CategoryRows.Add(new QuantityCategoryRuleRow(new QuantityCategoryRuleSetting",
        "Category = code",
        "ExtractSide = false",
        "ExtractBottom = false",
        "FaceAngleThresholdDeg = 30d",
        "RebuildIntersectionBrowser()",
    ):
        if token not in code:
            errors.append("category-rule create UI missing code contract: " + token)

    rebuild_start = code.find("private void RebuildMissingCategoryRuleChoices()")
    selection_start = code.find("private void MissingCategoryRuleSelectionChanged", rebuild_start)
    if rebuild_start < 0 or selection_start <= rebuild_start:
        errors.append("cannot isolate missing-category choice rebuild")
    else:
        rebuild = code[rebuild_start:selection_start]
        categories = rebuild.find("new HashSet<int>(CategoryRows.Select(x => x.CategoryCode))")
        intersections = rebuild.find("IntersectionRows")
        missing = rebuild.find(".Where(x => !categoryCodes.Contains(x))")
        replace = rebuild.find("MissingCategoryRuleChoices.Clear()")
        if min(categories, intersections, missing, replace) < 0 or not (categories < intersections < missing < replace):
            errors.append("missing category choices must be derived from intersection codes absent from category rules before UI replacement")

    handler_start = code.find("private void CreateCategoryRule_Click(object sender, RoutedEventArgs e)")
    handler_end = code.find("\n        }\n    }\n}", handler_start)
    if handler_start < 0 or handler_end <= handler_start:
        errors.append("cannot isolate CreateCategoryRule_Click handler")
    else:
        handler = code[handler_start:handler_end]
        duplicate = handler.find("CategoryRows.Any(x => x.CategoryCode == code)")
        referenced = handler.find("!IntersectionRows.Any(x => x.SourceCode == code || x.TargetCode == code)")
        confirm = handler.find("var answer = MessageBox.Show(")
        yes = handler.find("answer != MessageBoxResult.Yes")
        append = handler.find("CategoryRows.Add(new QuantityCategoryRuleRow(new QuantityCategoryRuleSetting")
        refresh = handler.find("RebuildIntersectionBrowser()", append)
        if min(duplicate, referenced, confirm, yes, append, refresh) < 0 or not (
            duplicate < referenced < confirm < yes < append < refresh
        ):
            errors.append("category-rule create must recheck duplicate/reference, confirm, append one conservative row, then refresh")
        if handler.count("CategoryRows.Add(new QuantityCategoryRuleRow(new QuantityCategoryRuleSetting") != 1:
            errors.append("category-rule create must append exactly one category row")
        for token in (
            "IntersectionRows.Add(",
            "_store.Save(",
            "_store.Import(",
            "_store.Export(",
            "ProjectContextCoordinator",
            "ExistingProjectMutationContext",
            "TransactionManager",
            "CadHandleService",
            "DataContractJsonSerializer",
            "File.Write",
        ):
            if token in handler:
                errors.append("category-rule create must remain in-memory/settings-only until existing Save: " + token)

print("QS3D Quantity Settings category-rule create UI preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: intersection-only category codes can gain exactly one confirmed conservative Category Rule in QS3DSETUP without external JSON editing, reverse/intersection rules are untouched, future-schema mode stays read-only, and persistence remains on the existing Save path.")
