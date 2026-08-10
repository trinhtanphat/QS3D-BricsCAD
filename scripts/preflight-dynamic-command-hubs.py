#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src/QS3D.BricsCAD.V25/UI"
FILES = (
    "DomainHubWindow.xaml.cs",
    "GeometryExtensionsWindow.xaml.cs",
    "Rebar3DHubWindow.xaml.cs",
)
errors = []

for name in FILES:
    path = UI / name
    if not path.is_file():
        errors.append("missing dynamic hub: " + str(path.relative_to(ROOT)))
        continue
    text = path.read_text(encoding="utf-8")
    if "DocumentBoundWindowLifetime.Attach" in text:
        errors.append(name + " is active-document dynamic and must not be bound to one source DWG.")
    if "MdiActiveDocument" not in text:
        errors.append(name + " must resolve the active BricsCAD document at click time.")
    if "SendStringToExecute" not in text:
        errors.append(name + " must dispatch through BricsCAD command input.")
    if "catch" not in text:
        errors.append(name + " must contain command-dispatch exception handling.")

for name in ("DomainHubWindow.xaml.cs", "Rebar3DHubWindow.xaml.cs"):
    path = UI / name
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    send = text.find("SendStringToExecute")
    success = text.find("Đã gửi lệnh")
    if send < 0 or success < 0 or send > success:
        errors.append(name + " must report success only after SendStringToExecute returns.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Domain, Geometry Extensions and Rebar 3D hubs remain active-document dynamic and contain fail-safe command dispatch reporting.")
