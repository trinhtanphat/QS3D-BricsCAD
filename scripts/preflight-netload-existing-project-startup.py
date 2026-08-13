#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []


def read(path: str) -> str:
    target = ROOT / path
    if not target.is_file():
        errors.append("missing required startup source: " + path)
        return ""
    return target.read_text(encoding="utf-8")


def between(text: str, start: str, end: str, label: str) -> str:
    start_at = text.find(start)
    end_at = text.find(end, start_at + len(start)) if start_at >= 0 else -1
    if start_at < 0 or end_at < 0:
        errors.append(label + " method boundary not found")
        return ""
    return text[start_at:end_at]


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        errors.append(label + " missing required token: " + token)


entry = read("src/QS3D.BricsCAD.V25/PluginEntry.cs")
palette = read("src/QS3D.BricsCAD.V25/PaletteCoordinator.cs")

initialize = between(entry, "public void Initialize()", "public void Terminate()", "PluginEntry.Initialize")
show = between(palette, "public static void Show()", "public static void Hide()", "PaletteCoordinator.Show")

for token in (
    "RuntimeDiagnosticsCommands.CaptureLoadedBinaryIdentity();",
    "DocumentLifecycleCoordinator.Start();",
    "RibbonInitializationCoordinator.Start();",
    "UpdateBootstrapper.Start();",
):
    require(initialize, token, "PluginEntry.Initialize")

if "PaletteCoordinator.EnsureCreated();" in initialize:
    errors.append("PluginEntry.Initialize must not eagerly construct palette/WPF trees during NETLOAD")

require(show, "EnsureCreated();", "PaletteCoordinator.Show")
require(show, "SelectionSyncCoordinator.Refresh(Application.DocumentManager.MdiActiveDocument);", "PaletteCoordinator.Show")
if "RefreshAll();" in show:
    errors.append("PaletteCoordinator.Show must not duplicate the panels' first-load refresh with RefreshAll")

for token in (
    "_workspacePanel = new WorkspacePanel();",
    "_rightPanel = new RightPanel();",
    "_quantityInsightPanel = new QuantityInsightPanel();",
    "public static void RefreshAll() { RefreshProject(); RefreshCad(); }",
    "PaletteCoordinator.Dispose();",
):
    require(palette if token != "PaletteCoordinator.Dispose();" else entry, token, "startup lifecycle")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: NETLOAD defers palette construction; first QS3D show avoids duplicate full refresh while explicit refresh/lifecycle paths remain intact.")
