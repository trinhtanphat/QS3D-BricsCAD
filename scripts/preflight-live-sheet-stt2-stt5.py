#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FILES = {
    "direct": ROOT / "src/QS3D.BricsCAD.V25/DirectDrawCommands.cs",
    "family": ROOT / "src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.xaml.cs",
    "detail_data": ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.Data.cs",
    "detail_registration": ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.Registration.cs",
    "settings": ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml",
}
errors = []

texts = {}
for key, path in FILES.items():
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))
        continue
    texts[key] = path.read_text(encoding="utf-8")

direct = texts.get("direct", "")
if direct:
    finalize_start = direct.find("private static void FinalizeUi(")
    finalize_end = direct.find("private static void EnsureActive(", finalize_start)
    if finalize_start < 0 or finalize_end < 0:
        errors.append("STT2: cannot isolate Direct Draw FinalizeUi")
    else:
        finalize = direct[finalize_start:finalize_end]
        for token in (
            "PaletteCoordinator.RefreshProject();",
            "document.Editor.Regen();",
            "PaletteCoordinator.SetStatus(status);",
        ):
            if token not in finalize:
                errors.append("STT2: FinalizeUi lost post-commit UI token: " + token)
        if "QS3DVIEW3D" in finalize or "SendStringToExecute" in finalize:
            errors.append("STT2: Direct Draw FinalizeUi must preserve the user's current viewport and must not queue an automatic view-switch command")

family = texts.get("family", "")
if family:
    save_start = family.find("private void OnSavePropertyClick(")
    remove_start = family.find("private void OnRemovePropertyClick(", save_start)
    if save_start < 0 or remove_start < 0:
        errors.append("STT3: cannot isolate Family custom-property save handler")
    else:
        save = family[save_start:remove_start]
        normalize = save.find("var key = (PropertyKeyBox.Text ?? string.Empty).Trim();")
        optional_guard = save.find("if (key.Length == 0)")
        mutation = save.find("ProjectFamilyService.SetProperty")
        if min(normalize, optional_guard, mutation) < 0 or not normalize < optional_guard < mutation:
            errors.append("STT3: blank custom-property key must be handled before ProjectFamilyService.SetProperty")
        if "Custom property là tùy chọn" not in save:
            errors.append("STT3: optional custom-property UX message missing")

    duplicate_start = family.find("private void OnDuplicateClick(")
    rename_start = family.find("private void OnRenameClick(", duplicate_start)
    duplicate = family[duplicate_start:rename_start if rename_start > duplicate_start else None]
    if duplicate_start < 0 or "ProjectFamilyService.Duplicate" not in duplicate:
        errors.append("STT3: existing Family copy/duplicate flow must remain available")

detail_data = texts.get("detail_data", "")
if detail_data:
    if "_quantityDetailOptions[0]" in detail_data:
        errors.append("STT4: Quantity detail must not index option zero blindly")
    for token in (
        "var firstOption = options.FirstOrDefault();",
        "if (firstOption == null)",
        "_quantityDetailSelectionLoading = true;",
        "_quantityDetailSelector.SelectedItem = firstOption;",
        "_quantityDetailSelectionLoading = false;",
        "RenderQuantityDetail(firstOption);",
    ):
        if token not in detail_data:
            errors.append("STT4: detail binding guard missing token: " + token)

detail_registration = texts.get("detail_registration", "")
if detail_registration:
    for token in (
        "private bool _quantityDetailSelectionLoading;",
        "if(_quantityDetailSelectionLoading)return;",
        "if(_quantityDetailSelector?.SelectedItem is QuantityInsightDetailOption option)RenderQuantityDetail(option);",
    ):
        if token not in detail_registration:
            errors.append("STT4: SelectionChanged re-entrancy guard missing token: " + token)

settings = texts.get("settings", "")
if settings:
    resources = settings.find("<Window.Resources>")
    root = settings[:resources if resources >= 0 else len(settings)]
    for token in (
        'Background="{DynamicResource Bg0Brush}"',
        'Foreground="{DynamicResource TextBrush}"',
    ):
        if token not in root:
            errors.append("STT5: root QuantitySettingsWindow must resolve theme dynamically: " + token)
    for forbidden in (
        'Background="{StaticResource Bg0Brush}"',
        'Foreground="{StaticResource TextBrush}"',
    ):
        if forbidden in root:
            errors.append("STT5: root QuantitySettingsWindow must not resolve Theme.xaml brush statically before Window.Resources: " + forbidden)

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: live-sheet STT2-STT5 source regressions remain guarded (Direct Draw preserves viewport, Family custom key/copy stays safe, quantity-detail selection stays bounded, and QS3DSETUP theme construction stays valid).")
