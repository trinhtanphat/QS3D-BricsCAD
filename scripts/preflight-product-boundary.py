#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]

required = {
    "README.md": [
        "Product form — BricsCAD plugin, not standalone EXE",
        "BricsCAD V25 is required at runtime",
        "docs/PRODUCT-BOUNDARY.md",
    ],
    "AGENTS.md": [
        "Locked product form: BricsCAD plugin",
        "docs/PRODUCT-BOUNDARY.md",
        "Do not reinterpret",
        "docs/AGENT-HANDOFF-CURRENT-2026-08-10.md",
    ],
    "docs/PRODUCT-BOUNDARY.md": [
        "QS3D is intentionally a **BricsCAD V25 x64 .NET plugin**",
        "A `QS3D.exe` is **not** a required or expected product artifact",
        "BLT/BLT3D material is a clean-room **workflow and UX reference only**",
    ],
    "docs/REQUIREMENTS.md": [
        "Product/runtime boundary",
        "BricsCAD V25 x64 .NET plugin",
        "not a standalone",
    ],
    "docs/ARCHITECTURE.md": [
        "Hosted-plugin boundary",
        "DemandLoad or `NETLOAD`",
        "not a standalone",
    ],
    "docs/UI-SPEC.md": [
        "Plugin hosting boundary",
        "no separate QS3D desktop shell",
        "workflow/UX familiarity only",
    ],
    "docs/V25-INSTALL.md": [
        "This installs a **BricsCAD V25 plugin**",
        "There is intentionally no `QS3D.exe`",
        "DemandLoad or `NETLOAD`",
    ],
    "docs/BLT3D-RESEARCH.md": [
        "Product-form clarification",
        "BricsCAD V25 plugin",
        "workflow/UX only",
    ],
    "docs/DIRECT-DRAW-WORKFLOW.md": [
        "QS3D must remain a **BricsCAD V25 x64 .NET plugin**",
        "not a request to create a standalone",
    ],
    "docs/DIRECT-DRAW-P0-IMPLEMENTATION.md": [
        "QS3D remains a **BricsCAD V25 x64 .NET plugin**",
        "does not introduce a standalone CAD engine",
    ],
    "docs/DIRECT-DRAW-P1-IMPLEMENTATION.md": [
        "QS3D remains a **BricsCAD V25 x64 .NET plugin**",
        "not a standalone CAD application",
    ],
    "docs/AGENT-HANDOFF-CURRENT-2026-08-10.md": [
        "QS3D is a **BricsCAD V25 x64 .NET plugin**",
        "not a standalone",
        "docs/PRODUCT-BOUNDARY.md",
    ],
}

errors = []
for relative, needles in required.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append(relative + " is missing")
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing product-boundary marker: " + needle)

csproj = ROOT / "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj"
if not csproj.is_file():
    errors.append(str(csproj.relative_to(ROOT)) + " is missing")
else:
    text = csproj.read_text(encoding="utf-8")
    if "<OutputType>Library</OutputType>" not in text:
        errors.append("BricsCAD adapter must remain a Library plugin target")

entry = ROOT / "src/QS3D.BricsCAD.V25/PluginEntry.cs"
if not entry.is_file():
    errors.append(str(entry.relative_to(ROOT)) + " is missing")
else:
    text = entry.read_text(encoding="utf-8")
    if "IExtensionApplication" not in text:
        errors.append("PluginEntry must remain a BricsCAD/Teigha extension entry point")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: canonical docs, Direct Draw handoffs and source keep QS3D explicitly scoped as a BricsCAD V25 plugin; BLT wording cannot silently redefine it as a standalone EXE.")
