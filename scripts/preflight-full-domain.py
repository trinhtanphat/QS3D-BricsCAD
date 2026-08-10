#!/usr/bin/env python3
from pathlib import Path
import os
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
errors = []
required = [
    "src/QS3D.Core/Recognition/RecognitionEngine.cs",
    "src/QS3D.Core/Revisions/RevisionSnapshotStore.cs",
    "src/QS3D.Core/Export/RebarCsvExporter.cs",
    "src/QS3D.Core/Rebar/RebarSchedule.cs",
    "src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs",
    "src/QS3D.Core/Export/XlsxQuantityExporter.cs",
    "src/QS3D.Core/Diagnostics/ModelHealthService.cs",
    "tests/QS3D.Core.SmokeTests/FullDomainIntegrationSmoke.cs",
    "src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/Services/RevisionCoordinator.cs",
    "src/QS3D.BricsCAD.V25/DomainExtensionsCommands.cs",
    "src/QS3D.BricsCAD.V25/DomainHubCommands.cs",
    "src/QS3D.BricsCAD.V25/UI/RecognitionWindow.xaml",
    "src/QS3D.BricsCAD.V25/UI/RecognitionWindow.xaml.cs",
    "src/QS3D.BricsCAD.V25/UI/RevisionWindow.xaml",
    "src/QS3D.BricsCAD.V25/UI/RevisionWindow.xaml.cs",
    "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml",
    "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml.cs",
    "scripts/package-full-domain-v25.ps1",
    "docs/FULL-DOMAIN-RUNTIME-CHECKLIST.md",
    "docs/FULL-DOMAIN-STATUS-20260810.md",
    ".github/workflows/full-domain-ci.yml",
]
for rel in required:
    if not (ROOT / rel).exists():
        errors.append(f"missing required full-domain file: {rel}")

for path in list(ROOT.rglob("*.csproj")) + list(ROOT.rglob("*.xaml")):
    try:
        ET.parse(path)
    except Exception as exc:
        errors.append(f"invalid XML/XAML {path.relative_to(ROOT)}: {exc}")

for bad in ("BrxMgd.dll", "TD_Mgd.dll", "TD_MgdBrep.dll"):
    found = [p.relative_to(ROOT) for p in ROOT.rglob(bad)]
    if found:
        errors.append(f"proprietary BricsCAD assembly committed: {bad}: {found}")

for ext in ("*.dwg", "*.dxf", "*.docx"):
    found = [p.relative_to(ROOT) for p in ROOT.rglob(ext)]
    if found:
        errors.append(f"private/reference artifact committed ({ext}): {found}")

for path in ROOT.rglob("*"):
    if path.is_dir() and path.name.lower() in {"blt", "blt3d"}:
        errors.append(f"vendor folder must not be committed: {path.relative_to(ROOT)}")

handler_pattern = re.compile(r'\b(?:Click|TextChanged|SelectionChanged|Checked|Unchecked|MouseDoubleClick)="([A-Za-z_][A-Za-z0-9_]*)"')
for xaml in ROOT.rglob("*.xaml"):
    if xaml.name == "Theme.xaml":
        continue
    code = xaml.with_suffix(xaml.suffix + ".cs")
    if not code.exists():
        errors.append(f"{xaml.relative_to(ROOT)}: missing code-behind")
        continue
    xt = xaml.read_text(encoding="utf-8")
    ct = code.read_text(encoding="utf-8")
    for handler in set(handler_pattern.findall(xt)):
        if not re.search(r"\b" + re.escape(handler) + r"\s*\(", ct):
            errors.append(f"{xaml.relative_to(ROOT)}: missing handler {handler}")

string_comment_pattern = re.compile(r"//.*?$|/\*.*?\*/|(?:\$|@|\$@|@\$)?\"(?:\"\"|\\.|[^\"\\])*\"|'(?:\\.|[^'\\])'", re.M | re.S)
for path in ROOT.rglob("*.cs"):
    raw = path.read_text(encoding="utf-8")
    text = string_comment_pattern.sub("", raw)
    pairs = {"{": "}", "(": ")", "[": "]"}
    stack = []
    for ch in text:
        if ch in pairs:
            stack.append(pairs[ch])
        elif ch in pairs.values():
            if not stack or stack.pop() != ch:
                errors.append(f"{path.relative_to(ROOT)}: unbalanced delimiter near {ch}")
                break
    else:
        if stack:
            errors.append(f"{path.relative_to(ROOT)}: unbalanced delimiter(s)")

commands = {}
command_pattern = re.compile(r'CommandMethod\(\s*"(QS3D[A-Z0-9_]*)"')
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    text = path.read_text(encoding="utf-8")
    for command in command_pattern.findall(text):
        commands.setdefault(command.upper(), []).append(str(path.relative_to(ROOT)))
for command, owners in sorted(commands.items()):
    if len(owners) > 1:
        errors.append(f"duplicate command {command}: {owners}")
for command in ("QS3DDOMAIN", "QS3DRECOGNIZE", "QS3DRECOGNIZEAUTO", "QS3DSTRUCTSOLID", "QS3DBBSCSV", "QS3DREVBASE", "QS3DREVDIFF"):
    if command not in commands:
        errors.append(f"missing full-domain command: {command}")

package_path = ROOT / "scripts/package-full-domain-v25.ps1"
package = package_path.read_text(encoding="utf-8") if package_path.exists() else ""
for forbidden in ("BrxMgd.dll", "TD_Mgd.dll", "TD_MgdBrep.dll"):
    if forbidden not in package:
        errors.append(f"package guard missing forbidden assembly: {forbidden}")
if "QS3D.BricsCAD.V25.dll" not in package or "QS3D.Core.dll" not in package:
    errors.append("package script does not require both QS3D assemblies")

allow_branch = os.environ.get("QS3D_FULL_DOMAIN_GATE_BRANCH", "").strip()
for workflow in (ROOT / ".github/workflows").glob("*.yml"):
    text = workflow.read_text(encoding="utf-8")
    if "workflow_dispatch:" not in text:
        errors.append(f"{workflow.name}: workflow_dispatch is required")
    has_auto = re.search(r"(?m)^\s*(push|pull_request)\s*:", text)
    temp_allowed = workflow.name == "full-domain-gate-temp.yml" and bool(allow_branch) and allow_branch in text
    if has_auto and not temp_allowed:
        errors.append(f"{workflow.name}: automatic trigger forbidden before V25 runtime gate")

print("QS3D full-domain preflight")
print("root:", ROOT)
print("commands:", len(commands))
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: full-domain files, XAML handlers, C# delimiters, command uniqueness, proprietary-file, package and CI guards are clean.")
