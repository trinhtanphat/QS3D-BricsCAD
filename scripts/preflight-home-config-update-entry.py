#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RIBBON_REL = "src/QS3D.BricsCAD.V25/Ribbon/BltHomeRibbonAugmenter.cs"


def fail(message: str) -> None:
    raise SystemExit("FAIL: " + message)


def main() -> int:
    ribbon = (ROOT / RIBBON_REL).read_text(encoding="utf-8")

    required_global = (
        "using QS3D.BricsCAD.V25.Updates;",
        '"Cập nhật QS3D"',
        "new UpdateCommands().ShowUpdateCenter()",
        "RibbonIconKind.Update",
        "DirectActionHandler",
    )
    for token in required_global:
        if token not in ribbon:
            fail(f"{RIBBON_REL} missing Home updater contract: {token}")

    config_anchor = 'ConfigPanelSourceId,\n                    "Cấu hình",'
    try:
        config_start = ribbon.index(config_anchor)
        config_end = ribbon.index("));", config_start) + 3
    except ValueError as exc:
        fail(f"{RIBBON_REL} cannot isolate Cấu hình panel: {exc}")

    config_block = ribbon[config_start:config_end]
    required_config = (
        'new HomeButtonSpec("QS3D_HOME_SETTINGS", "Cài đặt"',
        'new HomeButtonSpec("QS3D_HOME_SYSTEM_OBJECTS", "Đối tượng\\nhệ thống"',
        'new HomeButtonSpec("QS3D_HOME_UPDATE", "Cập nhật QS3D", () => new UpdateCommands().ShowUpdateCenter(), RibbonIconKind.Update)',
    )
    for token in required_config:
        if token not in config_block:
            fail(f"Cấu hình panel missing required action: {token}")

    if ribbon.count('"Cập nhật QS3D"') != 1:
        fail("BltHomeRibbonAugmenter must expose exactly one Cập nhật QS3D label")
    if ribbon.count("new UpdateCommands().ShowUpdateCenter()") != 1:
        fail("BltHomeRibbonAugmenter must bind exactly one direct Update Center action")
    if "SendStringToExecute" in ribbon:
        fail("Home updater must remain mouse-first and must not dispatch a host command string")
    if 'AddPanel(\n                    panels,\n                    UpdatePanelSourceId,' in ribbon:
        fail("KHỞI ĐẦU must not add a separate Update/Hệ thống panel")

    print("PASS: KHỞI ĐẦU → Cấu hình exposes exactly one direct Cập nhật QS3D action using the existing Update Center and semantic update icon.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
