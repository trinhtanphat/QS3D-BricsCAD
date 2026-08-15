#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
V25 = ROOT / "src" / "QS3D.BricsCAD.V25"


def read(relative: str) -> str:
    path = ROOT / relative
    if not path.is_file():
        fail(f"missing {relative}")
    return path.read_text(encoding="utf-8")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        fail(f"{label}: missing {needle!r}")


def forbid(text: str, needle: str, label: str) -> None:
    if needle in text:
        fail(f"{label}: forbidden {needle!r}")


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    sys.exit(1)


palette = read("src/QS3D.BricsCAD.V25/PaletteCoordinator.cs")
require(palette, "public static void Show() => ShowWorkspace();", "palette isolation")
require(palette, "public static void ShowWorkspace()", "workspace mode")
require(palette, "SetVisibility(workspace: true, right: false, quantityInsight: false);", "workspace mode")
require(palette, "public static void ShowQuantityInsight()", "quantity mode")
require(palette, "SetVisibility(workspace: false, right: false, quantityInsight: true);", "quantity mode")
forbid(palette, "_right.Visible = true;\n                _quantityInsight.Visible = true;", "all-palettes takeover")

quantity = read("src/QS3D.BricsCAD.V25/QuantityInsightCommands.cs")
require(quantity, '[CommandMethod("QS3DQUANTITYINSIGHT", CommandFlags.UsePickSet)]', "quantity command")
require(quantity, "EntitySnapshotReader.ReadImpliedSelection(document)", "quantity selection")
require(quantity, "PaletteCoordinator.ShowQuantityInsight();", "quantity palette")
for token in ("GetOrCreate(", "Build3D", "TransactionManager", "AppendEntity", "ProjectRepository"):
    forbid(quantity, token, "quantity command must be read-only")

context = read("src/QS3D.BricsCAD.V25/QuantityContextMenuCoordinator.cs")
require(context, '"Diễn giải khối lượng"', "context menu label")
require(context, 'private const string QuantityCommand = "QS3DQUANTITYINSIGHT";', "context command")
require(context, "RXObject.GetClass(typeof(Entity))", "entity context class")
require(context, '"AddObjectContextMenuExtension"', "object context registration")
require(context, '"RemoveObjectContextMenuExtension"', "object context teardown")
require(context, "document.SendStringToExecute(QuantityCommand + \" \", true, false, false);", "context dispatch")
for token in ("AddDefaultContextMenuExtension", "RemoveDefaultContextMenuExtension"):
    forbid(context, token, "quantity action must use selected-object context")
for token in ("GetOrCreate(", "Build3D", "TransactionManager", "AppendEntity", "ProjectRepository"):
    forbid(context, token, "native context-menu callback must be read-only")

raft = read("src/QS3D.BricsCAD.V25/RaftFoundationCommands.cs")
require(raft, '[CommandMethod("QS3DDRAWRAFTFOUNDATION", CommandFlags.Modal)]', "raft command")
require(raft, "new DirectDrawP1Commands().DrawFoundation();", "canonical Foundation delegation")

ribbon = read("src/QS3D.BricsCAD.V25/Ribbon/RaftFoundationRibbonAugmenter.cs")
require(ribbon, 'private const string ButtonText = "Móng Bè";', "raft ribbon label")
require(ribbon, 'private const string Command = "QS3DDRAWRAFTFOUNDATION";', "raft ribbon command")
require(ribbon, 'private const string StructurePanelSourceId = "QS3D_AUTHOR_STRUCTURE_PANEL_SOURCE";', "raft ribbon placement")

init = read("src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs")
require(init, "RaftFoundationRibbonAugmenter.TryInitialize()", "raft ribbon initialization")

plugin = read("src/QS3D.BricsCAD.V25/PluginEntry.cs")
require(plugin, "QuantityContextMenuCoordinator.Start();", "context startup")
require(plugin, "TryCleanup(QuantityContextMenuCoordinator.Stop);", "context teardown")
require(plugin, "TryCleanup(RaftFoundationRibbonAugmenter.Reset);", "raft ribbon teardown")

all_cs = "\n".join(path.read_text(encoding="utf-8") for path in V25.rglob("*.cs"))
for command in ("QS3DQUANTITYINSIGHT", "QS3DDRAWRAFTFOUNDATION"):
    registrations = len(re.findall(r'\[CommandMethod\(\s*"' + re.escape(command) + r'"', all_cs))
    if registrations != 1:
        fail(f"{command}: expected exactly one CommandMethod registration, found {registrations}")

print("PASS: ribbon-first palettes, Móng Bè quick draw, and selected-object quantity explanation source contracts")
