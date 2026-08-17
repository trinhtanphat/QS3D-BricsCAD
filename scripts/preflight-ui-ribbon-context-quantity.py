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
# #2450 intentionally kept Show() isolated only until #2399 landed as a complete dedicated
# Properties implementation. With the real QS3D editor now dynamically reparented, owner-facing
# QS3D activation restores BIM again while the explicit ShowWorkspace() helper remains isolated.
require(palette, "public static void Show() => ShowBimWorkspace();", "completed #2399 owner activation")
require(palette, "public static void ShowWorkspace()", "workspace mode")
require(palette, "_workspacePanel?.SetDedicatedPropertiesPaletteActive(false);", "workspace embedded properties")
require(palette, "SetVisibility(workspace: true, right: false, quantityInsight: false);", "workspace mode")
require(palette, "public static bool ShowBimWorkspace()", "BIM mode")
require(palette, "_workspacePanel?.SetDedicatedPropertiesPaletteActive(true);", "BIM dedicated properties")
require(palette, "private static readonly Guid PropertiesGuid", "dedicated properties palette")
require(palette, "public static bool IsPropertiesVisible", "dedicated properties visibility")
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
require(context, 'private const string ExtensionTypeName = "Bricscad.Windows.ContextMenuExtension, BrxMgd";', "native context extension type")
require(context, 'private const string MenuItemTypeName = "Bricscad.Windows.MenuItem, BrxMgd";', "native context menu item type")
require(context, '"System.Drawing.Icon"', "native context menu constructor compatibility")
require(context, "RXObject.GetClass(typeof(Entity))", "entity context class")
require(context, '"AddObjectContextMenuExtension"', "object context registration")
require(context, '"RemoveObjectContextMenuExtension"', "object context teardown")
require(context, "document.SendStringToExecute(QuantityCommand + \" \", true, false, false);", "context dispatch")
for token in ("AddDefaultContextMenuExtension", "RemoveDefaultContextMenuExtension"):
    forbid(context, token, "quantity action must use selected-object context")
for token in ("System.Windows.Forms.MenuItem", "Windows Forms MenuItem", "GetOrCreate(", "Build3D", "TransactionManager", "AppendEntity", "ProjectRepository"):
    forbid(context, token, "native context-menu callback must stay BricsCAD-native and read-only")

raft = read("src/QS3D.BricsCAD.V25/RaftFoundationCommands.cs")
require(raft, '[CommandMethod("QS3DDRAWRAFTFOUNDATION", CommandFlags.Modal)]', "raft command")
require(raft, "RaftFoundationBoundaryAuthoring.Execute();", "raft exact-boundary delegation")
forbid(raft, "new DirectDrawP1Commands().DrawFoundation();", "raft must not fall back to point picking")

raft_boundary = read("src/QS3D.BricsCAD.V25/RaftFoundationBoundaryAuthoring.cs")
for token, label in (
    ("PromptEntityOptions", "existing-boundary prompt"),
    ("selected is Polyline polyline", "closed Polyline support"),
    ("selected is Region region", "Region support"),
    ("if (!polyline.Closed)", "closed Polyline validation"),
    ("polyline.GetBulgeAt(index)", "curved Polyline rejection"),
    ('RequireSimplePolygon(points, "Polyline Móng Bè");', "closed Polyline simple-polygon validation"),
    ('RequireSimplePolygon(points, "Region Móng Bè");', "Region simple-polygon validation"),
    ("private static void RequireSimplePolygon", "simple-polygon validator"),
    ("SegmentsIntersectOrTouch", "non-adjacent edge intersection/touch validation"),
    ("Math.Abs(area2) <= crossTolerance", "zero-area rejection"),
    ("self-intersection/touching", "self-intersection refusal"),
    ("region.Explode(exploded);", "exact Region decomposition"),
    ("var line = item as Line;", "linear Region-only contract"),
    ("usedCount != segments.Count", "multi-loop/hole rejection"),
    ("CreateExactWcsPolyline(document, boundary)", "owned exact WCS source clone"),
    ("SemanticCaptureService.Capture(document, ElementCategory.Foundation)", "canonical Foundation semantic capture"),
    ("new Build3DCommands().Build3D();", "canonical native build"),
    ("ProjectStateSnapshot.Capture(project)", "atomic project rollback"),
    ("GeneratedGeometryService.RequireMatchingOwnership", "owned generated-CAD rollback"),
    ("transaction.GetObject(result.ObjectId, OpenMode.ForRead, false)", "selected source stays read-only"),
):
    require(raft_boundary, token, label)

polyline_validation = raft_boundary.find('RequireSimplePolygon(points, "Polyline Móng Bè");')
project_bootstrap = raft_boundary.find("ProjectContextCoordinator.GetOrCreate(document)")
source_clone = raft_boundary.find("CreateExactWcsPolyline(document, boundary)")
if polyline_validation < 0 or project_bootstrap < 0 or source_clone < 0:
    fail("raft simple-polygon pre-mutation ordering could not be located")
if polyline_validation > project_bootstrap or polyline_validation > source_clone:
    fail("raft simple-polygon validation must precede project bootstrap and source clone mutation")

for token in (
    "GeometricExtents",
    "Extents3d",
    "GetPointAtDist",
    "Tessell",
    "Sample",
    "ConvexHull",
    "TransformBy(document.Editor.CurrentUserCoordinateSystem)",
):
    forbid(raft_boundary, token, "raft boundary must never guess/tessellate/retarget exact geometry")

legacy_foundation = read("src/QS3D.BricsCAD.V25/DirectDrawP1Commands.cs")
require(legacy_foundation, '[CommandMethod("QS3DDRAWFOUNDATION", CommandFlags.Modal)]', "legacy Foundation command preserved")
require(legacy_foundation, 'AcquirePath(document, "Móng nhanh", 3, true);', "legacy Foundation point-pick preserved")

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

print("PASS: completed #2399 owner BIM activation with an isolated ShowWorkspace helper, exact simple closed-boundary Móng Bè, and selected-object quantity explanation source contracts")
