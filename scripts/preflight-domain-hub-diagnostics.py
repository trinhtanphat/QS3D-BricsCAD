#!/usr/bin/env python3
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DomainHubWindow.xaml"
RUNTIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "RuntimeDiagnosticsCommands.cs"
SUPPORT = ROOT / "src" / "QS3D.BricsCAD.V25" / "SupportBundleCommands.cs"
HARNESS = ROOT / "scripts" / "test-bricscad-v25-runtime.ps1"
HARNESS_CORE = ROOT / "scripts" / "test-bricscad-v25-runtime-core.ps1"
errors = []

for path in (UI, RUNTIME, SUPPORT, HARNESS, HARNESS_CORE):
    if not path.is_file():
        errors.append("missing diagnostics wiring source: " + str(path.relative_to(ROOT)))

if UI.is_file():
    try:
        ET.parse(UI)
    except ET.ParseError as exc:
        errors.append("DomainHubWindow.xaml is not well-formed XML/XAML: " + str(exc))
    text = UI.read_text(encoding="utf-8")
    for needle in (
        'Text="KIỂM TRA / RELEASE"',
        'Content="Kiểm tra runtime V25" Tag="QS3DRUNTIMECHECK"',
        'Content="Tạo Support Bundle" Tag="QS3DSUPPORTBUNDLE"',
    ):
        if needle not in text:
            errors.append("Domain Hub missing customer diagnostics wiring: " + needle)
    if 'Tag="QS3DRUNTIMEPROBE"' in text:
        errors.append("customer-facing Domain Hub must not route any action to automation-only QS3DRUNTIMEPROBE")
    if text.count('Tag="QS3DRUNTIMECHECK"') != 1:
        errors.append("Domain Hub must expose exactly one customer-facing QS3DRUNTIMECHECK action")
    if text.count('Tag="QS3DSUPPORTBUNDLE"') != 1:
        errors.append("Domain Hub must expose exactly one privacy-safe QS3DSUPPORTBUNDLE action")

if RUNTIME.is_file():
    text = RUNTIME.read_text(encoding="utf-8")
    for needle in (
        '[CommandMethod("QS3DRUNTIMECHECK", CommandFlags.Modal)]',
        "QS3DRUNTIMECHECK PASS",
        "QS3DRUNTIMECHECK FAIL",
    ):
        if needle not in text:
            errors.append("RuntimeDiagnosticsCommands.cs missing customer runtime contract: " + needle)

if SUPPORT.is_file():
    text = SUPPORT.read_text(encoding="utf-8")
    for needle in (
        '[CommandMethod("QS3DSUPPORTBUNDLE", CommandFlags.Modal)]',
        "QS3D_SUPPORT_BUNDLE_V1",
        "No drawing path, source/generated handles, semantic IDs, Family names, project metadata, user name or machine name are included.",
    ):
        if needle not in text:
            errors.append("SupportBundleCommands.cs missing privacy-safe support contract: " + needle)

if HARNESS.is_file():
    wrapper_text = HARNESS.read_text(encoding="utf-8")
    for needle in (
        "test-bricscad-v25-runtime-core.ps1",
        "New-Qs3dV25ProfileSandbox",
        "Restore-Qs3dV25ProfileSandbox",
        ". $coreScript @coreArgs",
    ):
        if needle not in wrapper_text:
            errors.append("runtime harness wrapper missing split-contract token: " + needle)

if HARNESS_CORE.is_file():
    text = HARNESS_CORE.read_text(encoding="utf-8")
    for needle in (
        '"QS3DRUNTIMEPROBE"',
        'Require-Qs3dMarkerValue -Marker $marker -Key "command" -Expected "QS3DRUNTIMEPROBE"',
    ):
        if needle not in text:
            errors.append("runtime harness core missing deterministic automation probe contract: " + needle)
    if "QS3DRUNTIMECHECK" in text:
        errors.append("automated NETLOAD marker harness core must stay on QS3DRUNTIMEPROBE rather than the human-facing runtime diagnostic")

commands = []
source_root = ROOT / "src" / "QS3D.BricsCAD.V25"
if source_root.is_dir():
    for path in source_root.rglob("*.cs"):
        commands.extend(re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8")))
for name in ("QS3DRUNTIMECHECK", "QS3DRUNTIMEPROBE", "QS3DSUPPORTBUNDLE"):
    count = sum(1 for command in commands if command.upper() == name)
    if count != 1:
        errors.append(name + " must be registered exactly once, found " + str(count))

print("QS3D Domain Hub diagnostics wiring preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: customer-facing Domain Hub diagnostics use QS3DRUNTIMECHECK and privacy-safe QS3DSUPPORTBUNDLE exactly once, while the profile-safe V25 wrapper delegates to a core that retains uniquely registered QS3DRUNTIMEPROBE as its deterministic automation-only command.")