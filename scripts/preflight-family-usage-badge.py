#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CONVERTER = ROOT / "src/QS3D.BricsCAD.V25/UI/FamilyUsageTextConverter.cs"
PARTIAL = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.FamilyUsageBadge.cs"
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml"
VM = ROOT / "src/QS3D.BricsCAD.V25/UI/ViewModels/WorkspaceViewModel.cs"
errors = []

for path in (CONVERTER, PARTIAL, XAML, VM):
    if not path.is_file():
        errors.append("missing Family usage source: " + str(path.relative_to(ROOT)))

if CONVERTER.is_file():
    text = CONVERTER.read_text(encoding="utf-8")
    required = (
        "public sealed class FamilyUsageTextConverter : IMultiValueConverter",
        "values[0] is ProjectFamily family",
        "BcadApplication.DocumentManager.MdiActiveDocument",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "var ownedFamily = project.FindFamily(family.Id);",
        "!ReferenceEquals(ownedFamily, family)",
        "project.Elements.Count(element =>",
        "string.Equals(element.FamilyId, family.Id, StringComparison.OrdinalIgnoreCase)",
        '+ " cấu kiện";',
        'return "—";',
        "throw new NotSupportedException(\"Family usage badge is read-only.\")",
    )
    for needle in required:
        if needle not in text:
            errors.append("FamilyUsageTextConverter missing read-only ownership/count contract: " + needle)

    for forbidden in (
        "GetOrCreate",
        "ExistingProjectMutationContext",
        "ProjectContextCoordinator.Save",
        "project.Touch",
        "SendStringToExecute",
        "SetSystemVariable",
        ".qsdb",
    ):
        if forbidden in text:
            errors.append("Family usage converter must remain passive/read-only: " + forbidden)

if PARTIAL.is_file():
    text = PARTIAL.read_text(encoding="utf-8")
    required = (
        "private static readonly bool FamilyUsageClassHandlerRegistered = RegisterFamilyUsageClassHandler();",
        'DependencyProperty.RegisterAttached(\n            "FamilyUsageUpgraded"',
        "FrameworkElement.LoadedEvent",
        "panel.EnsureFamilyUsageHooks();",
        "panel.UpgradeFamilyUsageBadges();",
        "_ = FamilyUsageClassHandlerRegistered;",
        "if (_familyUsageHooksApplied || FamilyList == null) return;",
        "FamilyList.ItemContainerGenerator.StatusChanged += OnFamilyUsageGeneratorStatusChanged;",
        "FamilyList.LayoutUpdated += OnFamilyUsageLayoutUpdated;",
        "GeneratorStatus.ContainersGenerated",
        "FamilyList.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem",
        "Descendants<TextBlock>(container)",
        'binding?.Path?.Path, "Properties.Count"',
        "var usageBinding = new MultiBinding",
        "Converter = _familyUsageConverter",
        "usageBinding.Bindings.Add(new Binding());",
        'usageBinding.Bindings.Add(new Binding("DataContext.Status")',
        "RelativeSourceMode.FindAncestor, typeof(ListBox), 1",
        "BindingOperations.SetBinding(textBlock, TextBlock.TextProperty, usageBinding);",
        "textBlock.SetValue(FamilyUsageUpgradedProperty, true);",
    )
    for needle in required:
        if needle not in text:
            errors.append("Workspace Family usage partial missing scoped/idempotent binding-upgrade contract: " + needle)

    hook_pos = text.find("private void EnsureFamilyUsageHooks()")
    guard_pos = text.find("if (_familyUsageHooksApplied || FamilyList == null) return;", hook_pos)
    generator_pos = text.find("FamilyList.ItemContainerGenerator.StatusChanged +=", guard_pos)
    layout_pos = text.find("FamilyList.LayoutUpdated +=", generator_pos)
    upgrade_pos = text.find("private void UpgradeFamilyUsageBadges()", layout_pos)
    original_binding_pos = text.find('"Properties.Count"', upgrade_pos)
    multi_pos = text.find("var usageBinding = new MultiBinding", original_binding_pos)
    set_pos = text.find("BindingOperations.SetBinding", multi_pos)
    mark_pos = text.find("FamilyUsageUpgradedProperty, true", set_pos)
    if min(hook_pos, guard_pos, generator_pos, layout_pos, upgrade_pos, original_binding_pos, multi_pos, set_pos, mark_pos) < 0 or not (
        hook_pos < guard_pos < generator_pos < layout_pos < upgrade_pos < original_binding_pos < multi_pos < set_pos < mark_pos
    ):
        errors.append("Family usage binding upgrade must hook once and replace only the original Properties.Count FamilyList badge before marking it upgraded")

    for forbidden in (
        "ProjectContextCoordinator.GetOrCreate",
        "ExistingProjectMutationContext",
        "SendStringToExecute",
        "SetSystemVariable",
        ".qsdb",
        "FamilyList.ItemsSource =",
        "FamilyList.SelectionChanged +=",
    ):
        if forbidden in text:
            errors.append("Family usage partial must not replace Family source/selection semantics or mutate CAD/project state: " + forbidden)

if XAML.is_file():
    text = XAML.read_text(encoding="utf-8")
    required = (
        'x:Name="FamilyList" ItemsSource="{Binding Families}"',
        'SelectionChanged="OnFamilySelectionChanged"',
        'Text="{Binding Properties.Count, StringFormat={}{0} thuộc tính}"',
        'Content="+ Thêm" Style="{StaticResource AccentButton}"',
        'Click="OnAddClick"',
        'Click="OnDeleteClick"',
        'Click="OnCaptureSelectedClick"',
        'Click="OnView3DClick"',
        'x:Name="FamilySearch"',
        'TextChanged="OnFamilySearchChanged"',
        'Text="{Binding Properties.Count, StringFormat={}{0} dòng}"',
    )
    for needle in required:
        if needle not in text:
            errors.append("existing Workspace Family/property XAML contract disappeared: " + needle)

if VM.is_file():
    text = VM.read_text(encoding="utf-8")
    for needle in (
        "public ObservableCollection<ProjectFamily> Families",
        "Families.Clear(); foreach (var item in project.Families.OrderBy",
        "public void SetActiveFamily(ProjectFamily? family)",
    ):
        if needle not in text:
            errors.append("WorkspaceViewModel must continue exposing canonical ProjectFamily rows/selection: " + needle)

print("QS3D Family semantic usage badge preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: FamilyList keeps canonical ProjectFamily selection while its existing property-count badge is upgraded idempotently to a read-only semantic N cấu kiện count; property-panel counts and Family actions remain unchanged.")
