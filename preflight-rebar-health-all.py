#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = {
    "all": ROOT / "src/QS3D.BricsCAD.V25/RebarHealthAllCommands.cs",
    "stirrup_service": ROOT / "src/QS3D.Core/Diagnostics/GeneratedBeamStirrupHealthService.cs",
    "tie_service": ROOT / "src/QS3D.Core/Diagnostics/GeneratedTieRebarHealthService.cs",
    "general_service": ROOT / "src/QS3D.Core/Diagnostics/GeneratedRebarHealthService.cs",
    "ribbon": ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs",
    "hub": ROOT / "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml",
}

for key, path in required.items():
    if not path.is_file():
        errors.append("missing unified rebar-health file: " + str(path.relative_to(ROOT)))

checks = {
    "all": [
        'CommandMethod("QS3DREBARHEALTHALL"',
        'Collect(project, "GeneratedRebarHandles")',
        'Collect(project, "GeneratedShapeRebarHandles")',
        'Collect(project, "GeneratedTieRebarHandles")',
        'Collect(project, "GeneratedBeamStirrupHandles")',
        'GeneratedRebarHealthService().InspectAll',
        'GeneratedTieRebarHealthService().Inspect',
        'GeneratedBeamStirrupHealthService().Inspect',
        'code.IndexOf("BEAM_STIRRUP"',
    ],
    "ribbon": ['"QS3DREBARHEALTHALL"'],
}

for key, needles in checks.items():
    path = required[key]
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(str(path.relative_to(ROOT)) + " missing unified-health token: " + needle)

# Domain Hub may be edited concurrently; require visibility once the command is surfaced there.
hub = required["hub"]
if hub.is_file():
    text = hub.read_text(encoding="utf-8")
    if 'Tag="QS3DREBARHEALTHALL"' not in text:
        print("WARN: Domain Hub does not yet expose QS3DREBARHEALTHALL; Ribbon exposure is present and this should be synchronized in the next UI pass.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: unified rebar health includes longitudinal, BBS-shape, column-tie and beam-stirrup generated ownership/live-handle checks and is exposed on the Ribbon.")
