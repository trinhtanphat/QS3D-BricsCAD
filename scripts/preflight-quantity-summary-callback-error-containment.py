#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE_ROOT = ROOT / "src/QS3D.BricsCAD.V25/UI"
SOURCES = sorted(SOURCE_ROOT.glob("QuantitySummaryWindow*.cs"))

errors = []
if not SOURCES:
    errors.append("missing QuantitySummaryWindow C# sources")
    source = ""
else:
    source = "\n".join(path.read_text(encoding="utf-8") for path in SOURCES)

if not any(path.name == "QuantitySummaryWindow.EstimateWorkspace.cs" for path in SOURCES):
    errors.append("missing Quantity Summary estimate workspace partial")

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
    "Không thể mở workspace dự toán này. Hãy thử lại hoặc đóng bảng BQ và mở lại.",
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
