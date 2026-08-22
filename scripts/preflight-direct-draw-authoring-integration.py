#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = {
    "src/QS3D.BricsCAD.V25/GlobalUsings.cs": ["global using QS3D.Core.Persistence;"],
    "src/QS3D.BricsCAD.V25/DirectDrawCommands.cs": ["ProjectStateSnapshot.Capture(project)", "UseBasePoint = true"],
    "src/QS3D.BricsCAD.V25/DirectDrawP1Commands.cs": ["ProjectStateSnapshot.Capture(project)", "UseBasePoint = true"],
    "src/QS3D.BricsCAD.V25/DirectDrawOpeningCommands.cs": [
        'CommandMethod("QS3DDRAWDOOR"',
        'CommandMethod("QS3DDRAWOPENING"',
        "ProjectStateSnapshot.Capture(project)",
        "new AutoHostLinkCommands().AutoLinkHosts()",
        'Properties.TryGetValue("HostWallId"',
    ],
    "src/QS3D.BricsCAD.V25/FamilyManagerCommands.cs": ['CommandMethod("QS3DFAMILIES"'],
    "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs": [
        '"QS3D_AUTHOR"',
        '"TẠO MỚI"',
        'Button("Family / Type", "QS3DFAMILIES")',
        '"QS3DDRAWDOOR"',
        '"QS3DDRAWOPENING"',
    ],
    "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml": [
        'Text="TẠO MỚI / DIRECT DRAW"',
        'Content="Family / Type — chọn bộ thông số" Tag="QS3DFAMILIES"',
        'Tag="QS3DDRAWDOOR"',
        'Tag="QS3DDRAWOPENING"',
    ],
    "docs/DIRECT-DRAW-OPENINGS.md": [
        "`QS3DDRAWDOOR` and `QS3DDRAWOPENING` are implemented in source",
        "**does not automatically call** `QS3DCUTOPENINGS`",
    ],
    "docs/CURTAIN-PATH-FRAMES.md": [
        "Status: implemented in source",
        "open `POLYLINE` source in WCS XY with +Z normal, including bulged segments",
    ],
    "docs/COMMANDS.md": [
        "### Door / Opening Direct Draw",
        "`QS3DDRAWDOOR`",
        "`QS3DDRAWOPENING`",
        "guarded open/bulged WCS-XY path support",
    ],
    "docs/AGENT-HANDOFF-CURRENT-2026-08-10.md": [
        "`QS3DDRAWDOOR`",
        "`QS3DDRAWOPENING`",
        "Family / Type",
        "Curtain path-frame",
    ],
}

for relative, needles in required.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing authoring integration dependency: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing authoring integration contract: " + needle)

commands = []
command_root = ROOT / "src/QS3D.BricsCAD.V25"
if command_root.is_dir():
    for path in command_root.rglob("*.cs"):
        text = path.read_text(encoding="utf-8")
        commands.extend(re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', text))

for name in (
    "QS3DFAMILIES",
    "QS3DDRAWWALL", "QS3DDRAWGLASSWALL", "QS3DDRAWWALLPIER",
    "QS3DDRAWBEAM", "QS3DDRAWSTRUCTWALL", "QS3DDRAWCOLUMN",
    "QS3DDRAWSLAB", "QS3DDRAWFOUNDATION", "QS3DDRAWDOOR", "QS3DDRAWOPENING",
):
    count = commands.count(name)
    if count != 1:
        errors.append(name + " must be declared exactly once, found " + str(count))

handoff = ROOT / "docs/AGENT-HANDOFF-CURRENT-2026-08-10.md"
if handoff.is_file():
    text = handoff.read_text(encoding="utf-8")
    if "richer Family/type chooser in Direct Draw" in text:
        errors.append("current handoff still lists Family/type discoverability as wholly future work")

print("QS3D Direct Draw authoring integration preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Direct Draw snapshot namespace, Family/Type discoverability, Door/Opening source status and Curtain path-frame status are synchronized; full DrawJig/runtime behavior remains a licensed V25 gate.")
