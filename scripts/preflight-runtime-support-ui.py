#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

runtime_check = ROOT / "src/QS3D.BricsCAD.V25/RuntimeDiagnosticsCommands.cs"
support_bundle = ROOT / "src/QS3D.BricsCAD.V25/SupportBundleCommands.cs"
domain_hub = ROOT / "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml"
runtime_harness = ROOT / "scripts/test-bricscad-v25-runtime.ps1"

checks = {
    runtime_check: [
        'CommandMethod("QS3DRUNTIMECHECK", CommandFlags.Modal)',
        "QS3DRUNTIMECHECK PASS",
        "QS3DRUNTIMECHECK FAIL",
        "cryptographic publisher/timestamp verification belongs to the signed installer/release gate",
    ],
    support_bundle: [
        'CommandMethod("QS3DSUPPORTBUNDLE", CommandFlags.Modal)',
        "QS3D_SUPPORT_BUNDLE_V1",
        "No drawing path, source/generated handles, semantic IDs, Family names, project metadata, user name or machine name are included.",
    ],
    domain_hub: [
        'Content="Kiểm tra runtime V25" Tag="QS3DRUNTIMECHECK"',
        'Content="Tạo Support Bundle" Tag="QS3DSUPPORTBUNDLE"',
    ],
    runtime_harness: [
        '"QS3DRUNTIMEPROBE"',
        'Require-Qs3dMarkerValue -Marker $marker -Key "command" -Expected "QS3DRUNTIMEPROBE"',
    ],
}

for path, needles in checks.items():
    if not path.is_file():
        errors.append("missing runtime/support integration dependency: " + str(path.relative_to(ROOT)))
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(str(path.relative_to(ROOT)) + " missing runtime/support boundary: " + needle)

if domain_hub.is_file():
    hub = domain_hub.read_text(encoding="utf-8")
    if 'Tag="QS3DRUNTIMEPROBE"' in hub:
        errors.append("Full Domain Hub must use user-facing QS3DRUNTIMECHECK, not automation-only QS3DRUNTIMEPROBE")
    if hub.count('Tag="QS3DRUNTIMECHECK"') != 1:
        errors.append("Full Domain Hub must expose exactly one user-facing QS3DRUNTIMECHECK action")
    if hub.count('Tag="QS3DSUPPORTBUNDLE"') != 1:
        errors.append("Full Domain Hub must expose exactly one QS3DSUPPORTBUNDLE action")

commands = []
source_root = ROOT / "src/QS3D.BricsCAD.V25"
if source_root.is_dir():
    for path in source_root.rglob("*.cs"):
        commands.extend(re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8")))
for name in ("QS3DRUNTIMECHECK", "QS3DRUNTIMEPROBE", "QS3DSUPPORTBUNDLE"):
    count = sum(1 for command in commands if command.upper() == name)
    if count != 1:
        errors.append(name + " must be registered exactly once, found " + str(count))

if runtime_harness.is_file():
    harness = runtime_harness.read_text(encoding="utf-8")
    if "QS3DRUNTIMECHECK" in harness:
        errors.append("Automated NETLOAD marker harness must stay on deterministic QS3DRUNTIMEPROBE rather than the user-facing diagnostic command")

print("QS3D runtime/support UI integration preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: user-facing Domain Hub diagnostics use QS3DRUNTIMECHECK and privacy-safe QS3DSUPPORTBUNDLE exactly once, while the automated NETLOAD marker harness retains QS3DRUNTIMEPROBE as its deterministic automation-only contract.")
