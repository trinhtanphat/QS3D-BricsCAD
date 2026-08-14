#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]

required = {
    "README.md": (
        "It runs inside BricsCAD as a managed plugin; it is not a standalone CAD executable.",
        "plugin for BricsCAD V25 and V26 x64",
        "A matching licensed BricsCAD installation is required for host builds and runtime qualification.",
        "docs/PRODUCT-BOUNDARY.md",
    ),
    "AGENTS.md": (
        "Locked product form: BricsCAD plugin",
        "BricsCAD V25 + V26 Windows x64 hosted plugin",
        "docs/PRODUCT-BOUNDARY.md",
        "Do not reinterpret",
    ),
    "docs/PRODUCT-BOUNDARY.md": (
        "QS3D in this repository is intentionally a **Windows x64 BricsCAD plugin** with host-specific managed assemblies",
        "A standalone `QS3D.exe` is not a required or expected artifact of the BricsCAD package",
        "BLT/BLT3D material is a clean-room **workflow and UX reference only**",
    ),
    "docs/REQUIREMENTS.md": ("Product/runtime boundary", "BricsCAD V25 + V26 Windows x64 hosted plugin", "not a standalone", "net8.0-windows"),
    "docs/ARCHITECTURE.md": ("Hosted-plugin boundary", "QS3D is a **BricsCAD-hosted plugin**", "`QS3D.BricsCAD.V25` — BricsCAD V25", "`QS3D.BricsCAD.V26` — BricsCAD V26", "not a standalone"),
    "docs/UI-SPEC.md": ("Plugin hosting boundary", "no separate QS3D desktop shell", "workflow/UX familiarity only"),
    "docs/V25-INSTALL.md": ("This installs a **BricsCAD V25 plugin**", "There is intentionally no `QS3D.exe`", "DemandLoad or `NETLOAD`"),
    "docs/BLT3D-RESEARCH.md": ("Product-form clarification", "BricsCAD-hosted plugin", "current V25/V26", "workflow/UX only"),
    "docs/DIRECT-DRAW-WORKFLOW.md": ("QS3D remains a **BricsCAD V25 x64 .NET plugin**", "not a request to create a standalone"),
    "docs/DIRECT-DRAW-P0-IMPLEMENTATION.md": ("QS3D remains a **BricsCAD V25 x64 .NET plugin**", "does not introduce a standalone CAD engine"),
    "docs/DIRECT-DRAW-P1-IMPLEMENTATION.md": ("QS3D remains a **BricsCAD V25 x64 .NET plugin**", "not a standalone CAD application"),
    "docs/AGENT-HANDOFF-CURRENT-2026-08-10.md": ("QS3D is a **BricsCAD V25 x64 .NET plugin**", "not a standalone", "docs/PRODUCT-BOUNDARY.md"),
}

errors = []
for relative, needles in required.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append(relative + " is missing")
        continue
    text = path.read_text(encoding="utf-8")
    errors.extend(relative + " missing product-boundary marker: " + needle for needle in needles if needle not in text)

host_projects = {
    "V25": ("src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj", ("<TargetFramework>net48</TargetFramework>", "<OutputType>Library</OutputType>", "<AssemblyName>QS3D.BricsCAD.V25</AssemblyName>")),
    "V26": ("src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj", ("<TargetFramework>net8.0-windows</TargetFramework>", "<OutputType>Library</OutputType>", "<AssemblyName>QS3D.BricsCAD.V26</AssemblyName>")),
}
for host, (relative, tokens) in host_projects.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append(relative + " is missing")
        continue
    text = path.read_text(encoding="utf-8")
    errors.extend("BricsCAD " + host + " adapter missing hosted Library identity: " + token for token in tokens if token not in text)

for relative in ("src/QS3D.BricsCAD.V25/PluginEntry.cs", "src/QS3D.BricsCAD.V26/PluginEntry.cs"):
    path = ROOT / relative
    if not path.is_file():
        errors.append(relative + " is missing")
    elif "IExtensionApplication" not in path.read_text(encoding="utf-8"):
        errors.append(relative + " must remain a BricsCAD/Teigha extension entry point")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: canonical policy/docs and both host projects keep QS3D explicitly scoped as BricsCAD V25 + V26 managed Library plugins; BLT wording cannot silently redefine it as a standalone EXE or cross-major binary.")
