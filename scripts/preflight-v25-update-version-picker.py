#!/usr/bin/env python3
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WINDOW_REL = "src/QS3D.BricsCAD.V25/Updates/UpdateCenterWindow.cs"
RECEIPT_REL = "src/QS3D.BricsCAD.V25/Updates/PreviewInstallReceipt.cs"


def fail(message: str) -> None:
    raise SystemExit("FAIL: " + message)


def require(text: str, needle: str, source: str) -> None:
    if needle not in text:
        fail(f"{source} missing V25 selectable-preview contract: {needle}")


def require_count(text: str, needle: str, minimum: int, source: str) -> None:
    actual = text.count(needle)
    if actual < minimum:
        fail(f"{source} expected at least {minimum} occurrences of {needle!r}, found {actual}")


def forbid(text: str, needle: str, source: str) -> None:
    if needle in text:
        fail(f"{source} contains forbidden V25 updater UI/behavior: {needle}")


def brush_rgb(text: str, name: str) -> tuple[int, int, int]:
    pattern = (
        rf"private static readonly Brush {re.escape(name)} = "
        r"new SolidColorBrush\(Color\.FromRgb\((\d+),\s*(\d+),\s*(\d+)\)\);"
    )
    match = re.search(pattern, text)
    if match is None:
        fail(f"{WINDOW_REL} missing explicit RGB brush: {name}")
    return tuple(int(value) for value in match.groups())


def relative_luminance(rgb: tuple[int, int, int]) -> float:
    channels = []
    for value in rgb:
        normalized = value / 255.0
        channels.append(
            normalized / 12.92
            if normalized <= 0.04045
            else ((normalized + 0.055) / 1.055) ** 2.4
        )
    return 0.2126 * channels[0] + 0.7152 * channels[1] + 0.0722 * channels[2]


def contrast_ratio(first: tuple[int, int, int], second: tuple[int, int, int]) -> float:
    lighter = max(relative_luminance(first), relative_luminance(second))
    darker = min(relative_luminance(first), relative_luminance(second))
    return (lighter + 0.05) / (darker + 0.05)


def require_contrast(
    text: str,
    foreground_name: str,
    background_name: str,
    minimum: float,
) -> None:
    ratio = contrast_ratio(brush_rgb(text, foreground_name), brush_rgb(text, background_name))
    if ratio < minimum:
        fail(
            f"{WINDOW_REL} {foreground_name}/{background_name} contrast {ratio:.2f}:1 "
            f"is below {minimum:.1f}:1"
        )


def main() -> int:
    window_path = ROOT / WINDOW_REL
    receipt_path = ROOT / RECEIPT_REL
    if not window_path.is_file():
        fail(f"missing {WINDOW_REL}")
    if not receipt_path.is_file():
        fail(f"missing {RECEIPT_REL}")

    window = window_path.read_text(encoding="utf-8")
    receipt = receipt_path.read_text(encoding="utf-8")

    # Searchable release selection stays pinned to the chosen release.
    for needle in (
        "Phiên bản cài đặt",
        "Tìm phiên bản",
        "_releaseSearchBox",
        "_releaseVersionPicker",
        "_publishedReleases",
        "GetPublishedReleasesAsync()",
        "FilterReleaseChoices",
        "_selectedRelease",
        "Mới nhất",
        "Đang dùng",
        "Cài đặt lại ",
    ):
        require(window, needle, WINDOW_REL)

    # The search field belongs inside the dropdown popup, not beside it. The picker and item
    # containers must own their dark-theme templates so Windows' light default popup cannot
    # produce white-on-white/low-contrast release rows.
    for needle in (
        "CreateReleasePickerTemplate()",
        "CreateReleaseSearchBoxTemplate()",
        "CreateReleaseChoiceItemStyle()",
        "PART_Popup",
        "PART_SearchBox",
        "PopupAnimation.Fade",
        "AttachReleaseSearchBox()",
        "_releaseVersionPicker.DropDownOpened",
        "new Trigger { Property = ComboBoxItem.IsSelectedProperty, Value = true }",
        "new Trigger { Property = UIElement.IsMouseOverProperty, Value = true }",
    ):
        require(window, needle, WINDOW_REL)

    for needle in (
        "pickerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });",
        "Grid.SetColumn(_releaseSearchBox, 0);",
        "pickerPanel.Children.Add(_releaseSearchBox);",
    ):
        forbid(window, needle, WINDOW_REL)

    # The closed picker and the embedded search field must remain legible before focus. Their
    # custom templates bypass WPF's outer Background/BorderBrush values, so pin the actual chrome
    # to explicit high-contrast tokens while keeping AccentSoft as the focused border.
    for needle in (
        "PickerInputBackground",
        "PickerInputBorder",
        "PickerInputPlaceholder",
        "searchBox.SetValue(Control.BackgroundProperty, PickerInputBackground);",
        "searchBox.SetValue(Control.ForegroundProperty, TextPrimary);",
        "searchBox.SetValue(Control.BorderBrushProperty, PickerInputBorder);",
        "placeholder.SetValue(TextBlock.ForegroundProperty, PickerInputPlaceholder);",
        "focusTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, AccentSoft, \"SearchChrome\"));",
        "focusTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, AccentSoft, \"PickerChrome\"));",
    ):
        require(window, needle, WINDOW_REL)
    require_count(window, "chrome.SetValue(Border.BackgroundProperty, PickerInputBackground);", 2, WINDOW_REL)
    require_count(window, "chrome.SetValue(Border.BorderBrushProperty, PickerInputBorder);", 2, WINDOW_REL)
    require_contrast(window, "TextPrimary", "PickerInputBackground", 4.5)
    require_contrast(window, "PickerInputPlaceholder", "PickerInputBackground", 4.5)
    require_contrast(window, "PickerInputBorder", "PickerInputBackground", 3.0)

    # Clicking install must use the selected release object; it must not refresh latest and silently
    # switch targets after the user made a choice.
    for needle in (
        "var selectedRelease = _selectedRelease ?? _result?.Release;",
        "await DownloadPreviewAsync(selectedRelease)",
        "PreviewInstallReceipt.TryWrite(",
        "PreviewInstallReceipt.TryDelete()",
        "TryApplyPostRestartReceipt",
    ):
        require(window, needle, WINDOW_REL)
    forbid(window, "resolve the newest release again at click time", WINDOW_REL)

    # The receipt differentiates a pending same-process install from a real post-restart mismatch,
    # then binds the expected semantic version to the adapter path actually loaded by BricsCAD.
    for needle in (
        "PreviewInstallReceiptInfo",
        "OriginProcessId",
        "OriginProcessStartUtcTicks",
        "ExpectedVersion",
        "ExpectedAdapterPath",
        "IsFromCurrentProcess",
        "MatchesLoadedAssembly",
        "NormalizeVersion",
        "MaxReceiptBytes",
        "preview-install.receipt",
        "Đã yêu cầu",
        "BricsCAD đang load",
        "DLL đang load",
    ):
        require(receipt, needle, RECEIPT_REL)

    print("PASS: V25 Update Center keeps search inside a dark high-contrast release dropdown, preserves unfocused contrast, and pins the selected release through install/restart verification.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
