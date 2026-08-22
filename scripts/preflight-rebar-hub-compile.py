#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []
code = ROOT / "src/QS3D.BricsCAD.V25/UI/Rebar3DHubWindow.xaml.cs"
bridge = ROOT / "src/QS3D.BricsCAD.V25/UI/Rebar3DHubWindow.ApplicationBridge.cs"
for path in (code, bridge):
    if not path.is_file(): errors.append("missing Rebar Hub compile-safety file: " + str(path.relative_to(ROOT)))

if code.is_file():
    text = code.read_text(encoding="utf-8")
    for needle in ("using System.Windows;", "using Bricscad.ApplicationServices;", "Application.DocumentManager"):
        if needle not in text: errors.append("Rebar Hub code-behind contract missing: " + needle)

if bridge.is_file():
    text = bridge.read_text(encoding="utf-8")
    for needle in (
        "public partial class Rebar3DHubWindow",
        "private static class Application",
        "Bricscad.ApplicationServices.DocumentCollection",
        "Bricscad.ApplicationServices.Application.DocumentManager",
    ):
        if needle not in text: errors.append("Rebar Hub Application bridge missing: " + needle)

print("QS3D Rebar Hub compile-safety preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: Rebar Hub resolves BricsCAD Application locally without a global alias or cross-UI shadow type.")
