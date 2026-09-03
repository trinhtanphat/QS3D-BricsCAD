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
    if "ex.Message" in text or "Exception.Message" in text:
        errors.append(name + " must not expose raw caught host exception detail.")

for name in ("DomainHubWindow.xaml.cs", "Rebar3DHubWindow.xaml.cs"):
    path = UI / name
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    send = text.find("SendStringToExecute")
    success = text.find("Đã gửi lệnh")
    stable_failure = text.find("sang BricsCAD.", success + 1)
    type_diagnostic = text.find("dispatch failed (" + '" + ex.GetType().Name + "' + ").")
    if send < 0 or success < 0 or send > success:
        errors.append(name + " must report success only after SendStringToExecute returns.")
    if stable_failure < 0:
        errors.append(name + " must expose a stable modeless dispatch-failure status.")
    if type_diagnostic < 0:
        errors.append(name + " must retain only a best-effort exception-type diagnostic after dispatch failure.")
    if "try { document.Editor.WriteMessage" not in text or "catch { }" not in text:
        errors.append(name + " dispatch diagnostics must remain best-effort and non-escaping.")

geometry = (UI / "GeometryExtensionsWindow.xaml.cs").read_text(encoding="utf-8") if (UI / "GeometryExtensionsWindow.xaml.cs").is_file() else ""
if geometry:
    if 'StatusText.Text = normalizedCommand + " không thể gửi sang BricsCAD.";' not in geometry:
        errors.append("Geometry Extensions stable dispatch-failure status changed unexpectedly.")
    if "ex.GetType().Name" not in geometry:
        errors.append("Geometry Extensions must retain type-only best-effort dispatch diagnostic.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Domain, Geometry Extensions and Rebar 3D hubs remain active-document dynamic, report success only after dispatch, redact raw host detail and keep best-effort type-only diagnostics.")
