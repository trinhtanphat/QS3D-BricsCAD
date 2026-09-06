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
        fail(f"{source} still contains stale/latest-only updater behavior: {needle}")


def main() -> int:
    window_path = ROOT / WINDOW_REL
    receipt_path = ROOT / RECEIPT_REL
    if not window_path.is_file():
        fail(f"missing {WINDOW_REL}")
    if not receipt_path.is_file():
        fail(f"missing {RECEIPT_REL}")

    window = window_path.read_text(encoding="utf-8")
    receipt = receipt_path.read_text(encoding="utf-8")

    # UI must expose a searchable selector backed by the already-reviewed release history API.
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

    print("PASS: V25 Update Center pins a searchable selected release and verifies the loaded version/path after restart.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
