#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WINDOW_REL = "src/QS3D.BricsCAD.V25/Updates/UpdateCenterWindow.cs"
RECEIPT_REL = "src/QS3D.BricsCAD.V25/Updates/PreviewInstallReceipt.cs"


def fail(message: str) -> None:
    raise SystemExit("FAIL: " + message)


def require(text: str, needle: str, source: str) -> None:
    if needle not in text:
        fail(f"{source} missing V25 selectable-preview contract: {needle}")


def forbid(text: str, needle: str, source: str) -> None:
    if needle in text:
        fail(f"{source} contains forbidden V25 updater UI/behavior: {needle}")


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

    print("PASS: V25 Update Center keeps search inside a dark high-contrast release dropdown and pins the selected release through install/restart verification.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
