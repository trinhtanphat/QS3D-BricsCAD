#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Export/RebarCsvExporter.cs",
    "src/QS3D.Core/Services/StructuralRegenerator.cs",
    "src/QS3D.BricsCAD.V25/BbsCsvCommands.cs",
    "src/QS3D.BricsCAD.V25/DomainHubCommands.cs",
    "src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml",
    "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml.cs",
    "tests/QS3D.Core.SmokeTests/CompletionRegressionSmoke.cs",
    "scripts/package-v25.ps1",
    "docs/COMMANDS.md",
]
for rel in required:
    if not (ROOT / rel).exists(): errors.append("missing full-domain file: " + rel)

command_owners = {}
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    text = path.read_text(encoding="utf-8")
    for command in re.findall(r'\[CommandMethod\("([^\"]+)"', text):
        command_owners.setdefault(command.upper(), []).append(str(path.relative_to(ROOT)))
for command, owners in sorted(command_owners.items()):
    if len(owners) > 1: errors.append("duplicate CommandMethod " + command + ": " + ", ".join(owners))

solid = ROOT / "src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs"
if solid.exists():
    text = solid.read_text(encoding="utf-8")
    for needle in ("ElementCategory.Stair", "ElementCategory.Railing", "ElementCategory.Earthwork", "DownwardFootprintMass"):
        if needle not in text: errors.append("full-domain native mass adapter missing: " + needle)

regen = ROOT / "src/QS3D.Core/Services/StructuralRegenerator.cs"
if regen.exists():
    text = regen.read_text(encoding="utf-8")
    for needle in ("RegenerateStair", "RegenerateRailing", "RegenerateEarthwork", "BulkedVolumeM3", "NetExportM3"):
        if needle not in text: errors.append("full-domain quantity regenerator missing: " + needle)

csv = ROOT / "src/QS3D.Core/Export/RebarCsvExporter.cs"
if csv.exists():
    text = csv.read_text(encoding="utf-8")
    for needle in ("TrimStart", "double.IsNaN", "double.IsInfinity", "new UTF8Encoding(true)"):
        if needle not in text: errors.append("BBS CSV safety guard missing: " + needle)

completion = ROOT / "tests/QS3D.Core.SmokeTests/CompletionRegressionSmoke.cs"
if completion.exists():
    text = completion.read_text(encoding="utf-8")
    for needle in ("StairQuantities();", "RailingQuantities();", "EarthworkQuantities();", "CsvIsExcelSafeAndFinite();", "VietnameseRecognition();"):
        if needle not in text: errors.append("completion regression coverage missing: " + needle)

registration = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
if registration.exists() and "CompletionRegressionSmoke.Run();" not in registration.read_text(encoding="utf-8"):
    errors.append("CompletionRegressionSmoke is not registered")

print("QS3D full-domain preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: full-domain files, unique commands, Stair/Railing/Earthwork quantities/native mass adapters, BBS CSV safety and completion regression registration are present.")
