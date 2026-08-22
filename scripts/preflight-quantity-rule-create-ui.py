#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml"
CODE = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml.cs"
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
        'x:Name="CreateSelectedRuleButton"',
        'Content="＋  Tạo luật A → B"',
        'Click="CreateSelectedRule_Click"',
        'IsEnabled="False"',
        'Visibility="Collapsed"',
    ):
        if token not in xaml:
            errors.append("quantity rule create UI missing XAML contract: " + token)

if not CODE.is_file():
    errors.append("missing QuantitySettingsWindow.xaml.cs")
else:
    code = CODE.read_text(encoding="utf-8")
    for token in (
        "UpdateCreateSelectedRuleButton(selected == null)",
        'FindName("CreateSelectedRuleButton") as Button',
        "button.Visibility = isMissingRule ? Visibility.Visible : Visibility.Collapsed",
        "button.IsEnabled = isMissingRule && !_persistentSettingsWriteBlocked",
        "private void CreateSelectedRule_Click(object sender, RoutedEventArgs e)",
        "IntersectionRows.Any(x => x.SourceCode == source.CategoryCode && x.TargetCode == target.CategoryCode)",
        "MessageBoxButton.YesNo",
        "answer != MessageBoxResult.Yes",
        "IntersectionRows.Add(new QuantityIntersectionRuleRow(new QuantityIntersectionRuleSetting",
        "Source = source.CategoryCode",
        "Target = target.CategoryCode",
        "Luật chiều ngược không được tự tạo",
        "_store.Save(current)",
    ):
        if token not in code:
            errors.append("quantity rule create UI missing code contract: " + token)

    handler_start = code.find("private void CreateSelectedRule_Click(object sender, RoutedEventArgs e)")
    handler_end = code.find("private void ViewReverseRule_Click", handler_start)
    if handler_start < 0 or handler_end <= handler_start:
        errors.append("cannot isolate CreateSelectedRule_Click handler")
    else:
        handler = code[handler_start:handler_end]
        duplicate = handler.find("IntersectionRows.Any(")
        confirm = handler.find("var answer = MessageBox.Show(", duplicate)
        yes = handler.find("answer != MessageBoxResult.Yes", confirm)
        append = handler.find("IntersectionRows.Add(new QuantityIntersectionRuleRow(new QuantityIntersectionRuleSetting", yes)
        refresh = handler.find("RefreshSelectedIntersectionRule();", append)
        if min(duplicate, confirm, yes, append, refresh) < 0 or not (duplicate < confirm < yes < append < refresh):
            errors.append("create handler must re-check duplicate, confirm, append one row, then refresh")
        if handler.count("IntersectionRows.Add(new QuantityIntersectionRuleRow(new QuantityIntersectionRuleSetting") != 1:
            errors.append("create handler must append exactly one directed rule row")
        for token in (
            "_store.Save(",
            "_store.Import(",
            "_store.Export(",
            "ProjectContextCoordinator",
            "ExistingProjectMutationContext",
            "TransactionManager",
            "CadHandleService",
            "DataContractJsonSerializer",
        ):
            if token in handler:
                errors.append("create handler must remain an in-memory settings edit until Save: " + token)

    refresh_start = code.find("private void RefreshSelectedIntersectionRule()")
    clear_start = code.find("private void ClearIntersectionRuleDetail()", refresh_start)
    if refresh_start < 0 or clear_start <= refresh_start:
        errors.append("cannot isolate selected-rule refresh contract")
    else:
        refresh_body = code[refresh_start:clear_start]
        selected = refresh_body.find("var selected = IntersectionRows.SingleOrDefault")
        toggle = refresh_body.find("UpdateCreateSelectedRuleButton(selected == null)")
        if selected < 0 or toggle <= selected:
            errors.append("create action state must follow exact selected-pair lookup")

print("QS3D Quantity Settings in-window rule-create preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: missing selected A→B pairs expose one confirmed in-memory create action, existing/future-schema states stay fail-closed, reverse rules are not synthesized, and persistence remains on the existing Save path.")
