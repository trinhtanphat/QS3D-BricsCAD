#!/usr/bin/env python3
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "QuantitySettingsWindow.xaml"
CODE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "QuantitySettingsWindow.xaml.cs"


def require(text, tokens, label):
    missing = [token for token in tokens if token not in text]
    if not missing:
        return []
    return [label + ": " + token for token in missing]


def method_body(text, signature, next_signature):
    start = text.find(signature)
    if start < 0:
        return ""
    end = text.find(next_signature, start + len(signature))
    return text[start:] if end < 0 else text[start:end]


def main():
    try:
        ET.parse(str(XAML))
    except ET.ParseError as exc:
        print("ERROR: QuantitySettingsWindow.xaml is not well-formed XML:", exc)
        return 1

    xaml = XAML.read_text(encoding="utf-8")
    code = CODE.read_text(encoding="utf-8")

    missing = []
    missing += require(xaml, [
        'x:Name="PrimaryCategoryList"',
        'x:Name="ReferenceCategoryList"',
        'ItemsSource="{Binding IntersectionCategoryChoices}"',
        'SelectionChanged="IntersectionCategorySelectionChanged"',
        'x:Name="SelectedRuleEditor"',
        'IsChecked="{Binding SubtractConcrete, Mode=TwoWay}"',
        'IsChecked="{Binding SubtractSideFormworkByConcrete, Mode=TwoWay}"',
        'IsChecked="{Binding SubtractBottomFormworkByConcrete, Mode=TwoWay}"',
        'IsChecked="{Binding SubtractSideFormworkBySideFormwork, Mode=TwoWay}"',
        'IsChecked="{Binding SubtractBottomFormworkByBottomFormwork, Mode=TwoWay}"',
        'x:Name="CreateSelectedRuleButton"',
        'Click="CreateSelectedRule_Click"',
        'x:Name="ReverseRuleSummaryText"',
        'Click="ViewReverseRule_Click"',
        'Chỉ dòng A → B đang chọn được chỉnh.',
    ], "xaml")
    missing += require(code, [
        'public ObservableCollection<QuantityCategoryChoice> IntersectionCategoryChoices { get; }',
        'RebuildIntersectionBrowser();',
        '.Concat(IntersectionRows.Select(x => x.SourceCode))',
        '.Concat(IntersectionRows.Select(x => x.TargetCode))',
        'IntersectionRows.SingleOrDefault(x => x.SourceCode == source.CategoryCode && x.TargetCode == target.CategoryCode)',
        'IntersectionRows.SingleOrDefault(x => x.SourceCode == target.CategoryCode && x.TargetCode == source.CategoryCode)',
        'private void CreateSelectedRule_Click(object sender, RoutedEventArgs e)',
        'if (_persistentSettingsWriteBlocked)',
        'IntersectionRows.Any(x => x.SourceCode == source.CategoryCode && x.TargetCode == target.CategoryCode)',
        'MessageBoxButton.YesNo',
        'IntersectionRows.Add(new QuantityIntersectionRuleRow(new QuantityIntersectionRuleSetting',
        'Source = source.CategoryCode,',
        'Target = target.CategoryCode',
        'private void ViewReverseRule_Click(object sender, RoutedEventArgs e)',
        'PrimaryCategoryList.SelectedItem = nextSource;',
        'ReferenceCategoryList.SelectedItem = nextTarget;',
        'IntersectionRules = IntersectionRows.Select(x => x.ToSetting()).ToList()',
    ], "code")
    if missing:
        print("ERROR: Quantity Settings intersection browser contract is incomplete:")
        for item in missing:
            print(" -", item)
        return 1

    refresh = method_body(
        code,
        "private void RefreshSelectedIntersectionRule()",
        "private void ClearIntersectionRuleDetail()",
    )
    create = method_body(
        code,
        "private void CreateSelectedRule_Click(object sender, RoutedEventArgs e)",
        "private void ViewReverseRule_Click(object sender, RoutedEventArgs e)",
    )
    reverse = method_body(
        code,
        "private void ViewReverseRule_Click(object sender, RoutedEventArgs e)",
        "private static string SummarizeIntersectionRule",
    )
    build = method_body(
        code,
        "private QuantityCalculationSettings BuildSettingsFromView()",
        "private void RebuildIntersectionBrowser()",
    )

    if "IntersectionRows.Add(" in refresh or "IntersectionRows.Add(" in reverse:
        print("ERROR: browsing a missing/reverse pair must not silently create intersection rules.")
        return 1
    if "IntersectionRows.Add(" not in create:
        print("ERROR: explicit create handler must own directed-rule creation.")
        return 1
    blocked = create.find("if (_persistentSettingsWriteBlocked)")
    duplicate = create.find("IntersectionRows.Any(")
    confirm = create.find("MessageBoxButton.YesNo")
    add = create.find("IntersectionRows.Add(")
    if min(blocked, duplicate, confirm, add) < 0 or not blocked < duplicate < confirm < add:
        print("ERROR: directed-rule creation must fail in read-only mode, reject duplicates, obtain explicit confirmation, then append exactly the selected A → B rule.")
        return 1
    if "Target = source.CategoryCode" in create or "Source = target.CategoryCode" in create:
        print("ERROR: creating A → B must not auto-create or mirror the reverse B → A rule.")
        return 1
    if "IntersectionRules = IntersectionRows.Select(x => x.ToSetting()).ToList()" not in build:
        print("ERROR: save/export must retain the complete IntersectionRows payload, not only the selected pair.")
        return 1

    source_lookup = refresh.find("SourceCode == source.CategoryCode && x.TargetCode == target.CategoryCode")
    bind = refresh.find("SelectedRuleEditor.DataContext = selected;")
    reverse_lookup = refresh.find("SourceCode == target.CategoryCode && x.TargetCode == source.CategoryCode")
    if min(source_lookup, bind, reverse_lookup) < 0 or not source_lookup < bind < reverse_lookup:
        print("ERROR: selected directed rule must resolve before binding and reverse-rule lookup.")
        return 1

    if 'ItemsSource="{Binding IntersectionRows}"' in xaml:
        print("ERROR: the Intersection Rules tab regressed to the flat all-row DataGrid instead of the directed browser.")
        return 1

    print("PASS: Quantity Settings browses one directed pair at a time, creates a missing A → B rule only through explicit confirmed writable UI, never mirrors B → A, and serializes the complete matrix.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
