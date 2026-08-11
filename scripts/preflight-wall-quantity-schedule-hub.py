#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/ScheduleHubWindow.xaml"
CODE = ROOT / "src/QS3D.BricsCAD.V25/UI/ScheduleHubWindow.xaml.cs"

errors = []


def require(text, token, label):
    if token not in text:
        errors.append(f"{label}: missing {token!r}")


xaml = XAML.read_text(encoding="utf-8")
code = CODE.read_text(encoding="utf-8")

require(xaml, 'Content="Khối lượng Tường"', "wall quantity launcher")
require(xaml, 'Tag="QS3DWALLQTY"', "wall quantity command")
require(xaml, 'Click="OnCommandClick"', "generic command dispatcher")
require(xaml, 'BQ tổng • Tường •', "hub subtitle")
require(code, "private void OnCommandClick", "dispatcher")
require(code, "EnsureActive(\"chạy \" + normalizedCommand)", "document affinity")
require(code, "_document.SendStringToExecute(normalizedCommand + \" \", true, false, false)", "command queue")
require(code, "ProjectContextCoordinator.TryGetReadOnly", "read-only snapshot")
require(code, "ProjectStateSnapshot.CreateDetachedCopy", "detached snapshot")

for forbidden in (
    "ProjectContextCoordinator.GetOrCreate",
    "ProjectContextCoordinator.SetCurrent",
    "QsdbProjectStore",
    "StartTransaction(",
):
    if forbidden in code:
        errors.append(f"Schedule Hub read-only boundary: forbidden token {forbidden!r}")

try:
    ET.parse(XAML)
except ET.ParseError as exc:
    errors.append(f"Schedule Hub XAML XML parse failed: {exc}")

if xaml.count('Tag="QS3DWALLQTY"') != 1:
    errors.append("wall quantity launcher: expected exactly one QS3DWALLQTY button")

if errors:
    print("wall quantity Schedule Hub preflight: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("wall quantity Schedule Hub preflight: PASS")
