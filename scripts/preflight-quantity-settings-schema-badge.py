#!/usr/bin/env python3
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
xaml_path = root / "src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml"
source_path = root / "src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.SchemaVersion.cs"

xaml = xaml_path.read_text(encoding="utf-8")
source = source_path.read_text(encoding="utf-8")
errors = []

try:
    ET.fromstring(xaml)
except ET.ParseError as exc:
    errors.append(f"QuantitySettingsWindow.xaml is not well-formed XML: {exc}")

if 'Text="{Binding SchemaVersionLabel}"' not in xaml:
    errors.append("Quantity Settings schema badge must bind to SchemaVersionLabel.")

if re.search(r'Text\s*=\s*["\']Schema\s+v\d+["\']', xaml, re.IGNORECASE):
    errors.append("Quantity Settings schema badge must not hardcode a numeric schema version.")

required_source_tokens = (
    "public string SchemaVersionLabel =>",
    '"Schema v" + QuantityCalculationSettings.CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture)',
)
for token in required_source_tokens:
    if token not in source:
        errors.append(f"Missing canonical schema-label token: {token}")

if re.search(r'"Schema v\d+"', source):
    errors.append("SchemaVersionLabel must not hardcode a numeric schema version.")

if errors:
    print("FAIL: Quantity Settings schema badge contract")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("PASS: Quantity Settings schema badge derives from CurrentSchemaVersion")
