#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml.cs"

errors = []
if not SOURCE.exists():
    errors.append("missing QuantitySummaryWindow.xaml.cs")
    source = ""
else:
    source = SOURCE.read_text(encoding="utf-8")

if "ex.Message" in source:
    errors.append("Quantity Summary callbacks must not reflect raw Exception.Message text")

required_messages = (
    "Không thể đổi chế độ BQ. Hãy thử lại hoặc đóng bảng BQ và mở lại.",
    "Không thể tính lại khối lượng. Hãy thử lại hoặc đóng bảng BQ và mở lại.",
    "Không thể đổi cấu hình cột BQ. Cấu hình trước đó đã được khôi phục.",
    "Không thể mở ED2 Excel từ bảng BQ.",
    "Không thể mở định vị từ Excel trong bảng BQ.",
    "Không thể định vị dòng BQ hiện tại. Hãy tính lại BQ và thử lại.",
    "Không thể xuất Excel từ bảng BQ.",
)
for message in required_messages:
    if message not in source:
        errors.append("missing stable callback failure text: " + message)

for handler in (
    "OnViewModeChanged",
    "OnRecalculateClick",
    "OnColumnVisibilityChanged",
    "OnEd2ExportClick",
    "OnExcelLocateClick",
    "LocateCurrent",
    "OnExportClick",
):
    if handler not in source:
        errors.append("missing Quantity Summary callback: " + handler)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Quantity Summary modeless callback failures use stable local messages without raw Exception.Message reflection.")
