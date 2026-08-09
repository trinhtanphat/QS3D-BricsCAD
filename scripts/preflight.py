#!/usr/bin/env python3
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "Directory.Build.props", "README.md", "src/QS3D.Core/QS3D.Core.csproj",
    "src/QS3D.Core/Export/XlsxQuantityExporter.cs",
    "src/QS3D.Core/Reporting/QuantityReportBuilder.cs",
    "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj",
    "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml",
    "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml",
    "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml",
    "tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj",
    ".github/workflows/ci.yml", ".github/workflows/bricscad-v25.yml",
]
for rel in required:
    if not (ROOT / rel).exists(): errors.append(f"missing required file: {rel}")

for path in list(ROOT.rglob("*.csproj")) + list(ROOT.rglob("*.xaml")):
    try: ET.parse(path)
    except Exception as exc: errors.append(f"invalid XML {path.relative_to(ROOT)}: {exc}")

for bad_name in ("BrxMgd.dll", "TD_Mgd.dll", "TD_MgdBrep.dll"):
    if list(ROOT.rglob(bad_name)): errors.append(f"proprietary BricsCAD assembly must not be committed: {bad_name}")

for ext in ("*.dwg", "*.dxf", "*.docx"):
    found = [p.relative_to(ROOT) for p in ROOT.rglob(ext)]
    if found: errors.append(f"private/reference artifact must not be committed in public repo ({ext}): {found}")

plugin = ROOT / "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj"
if plugin.exists():
    text = plugin.read_text(encoding="utf-8")
    for needle, message in {
        "<TargetFramework>net48</TargetFramework>": "plugin must target net48",
        "$(BRICSCAD_V25_DIR)\\BrxMgd.dll": "plugin must use external BrxMgd reference",
        "<Private>false</Private>": "BricsCAD references must not be copied locally",
    }.items():
        if needle not in text: errors.append(message)

for workflow in (ROOT / ".github/workflows").glob("*.yml"):
    text = workflow.read_text(encoding="utf-8")
    if "workflow_dispatch:" not in text: errors.append(f"{workflow.name}: must be manual-only")
    if re.search(r"(?m)^\s*(push|pull_request)\s*:", text): errors.append(f"{workflow.name}: automatic trigger forbidden before V25 runtime gate")

for path in ROOT.rglob("*"):
    if path.is_dir() and path.name.lower() in {"blt", "blt3d"}: errors.append(f"vendor folder must not be committed: {path.relative_to(ROOT)}")

# Basic XAML code-behind event check.
for xaml in ROOT.rglob("*.xaml"):
    if xaml.name == "Theme.xaml": continue
    code = xaml.with_suffix(xaml.suffix + ".cs")
    if not code.exists(): continue
    xaml_text = xaml.read_text(encoding="utf-8")
    code_text = code.read_text(encoding="utf-8")
    handlers = set(re.findall(r'\b(?:Click|TextChanged|SelectionChanged)="([A-Za-z_][A-Za-z0-9_]*)"', xaml_text))
    for handler in handlers:
        if not re.search(r"\b" + re.escape(handler) + r"\s*\(", code_text): errors.append(f"{xaml.relative_to(ROOT)}: missing code-behind handler {handler}")

# Simple delimiter balance catches many truncated-generation errors without pretending to compile C#.
for path in ROOT.rglob("*.cs"):
    text = re.sub(r"//.*?$|/\*.*?\*/|(?:\$|@|\$@|@\$)?\"(?:\"\"|\\.|[^\"\\])*\"|'(?:\\.|[^'\\])'", '', path.read_text(encoding='utf-8'), flags=re.M|re.S)
    pairs = {'{':'}','(':')','[':']'}
    stack=[]
    for ch in text:
        if ch in pairs: stack.append(pairs[ch])
        elif ch in pairs.values():
            if not stack or stack.pop()!=ch:
                errors.append(f"{path.relative_to(ROOT)}: unbalanced delimiter near '{ch}'"); break
    else:
        if stack: errors.append(f"{path.relative_to(ROOT)}: unbalanced delimiter(s)")

print("QS3D preflight")
print("root:", ROOT)
if errors:
    for e in errors: print("ERROR:", e)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: structure, XML/XAML, handler, delimiter, proprietary-file and manual-CI guards are clean.")
