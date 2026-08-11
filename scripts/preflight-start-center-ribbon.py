#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RIBBON = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "RibbonBootstrapper.cs"
START = ROOT / "src" / "QS3D.BricsCAD.V25" / "StartCenterCommands.cs"


def require(text, token, label):
    if token not in text:
        raise AssertionError(f"{label} missing token: {token}")


def main():
    ribbon = RIBBON.read_text(encoding="utf-8")
    start = START.read_text(encoding="utf-8")

    require(start, '[CommandMethod("QS3DSTART", CommandFlags.Modal)]', "Start Center command registration")
    require(start, "Application.ShowModelessWindow", "BricsCAD-hosted modeless Start Center")

    binding = 'Button("Start Center", "QS3DSTART")'
    if ribbon.count(binding) != 1:
        raise AssertionError(f"Start Center Ribbon binding must appear exactly once; found {ribbon.count(binding)}")
    if ribbon.count('"QS3DSTART"') != 1:
        raise AssertionError("QS3DSTART must appear exactly once in Ribbon source")

    home = ribbon.index('"QS3D_HOME"')
    project = ribbon.index('Panel("PROJECT", "Dự án"', home)
    binding_index = ribbon.index(binding)
    coordination = ribbon.index('Panel("COORDINATION", "Điều phối"', project)
    if not (home < project < binding_index < coordination):
        raise AssertionError("Start Center must remain in QS3D_HOME / PROJECT before the coordination panel")

    require(ribbon, 'Button("Workspace", "QS3D")', "existing home Workspace binding")
    require(ribbon, 'Button("Lưu", "QS3DSAVE")', "existing home Save binding")
    require(ribbon, "Application.DocumentManager.MdiActiveDocument?.SendStringToExecute", "click-time Ribbon dispatch")

    print("PASS: QS3DSTART is exposed exactly once in KHỞI ĐẦU / Dự án and retains BricsCAD click-time dispatch.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (AssertionError, ValueError) as exc:
        print("ERROR:", exc)
        raise SystemExit(1)
