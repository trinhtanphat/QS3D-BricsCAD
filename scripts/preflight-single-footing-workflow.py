#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CORE = ROOT / "src" / "QS3D.Core" / "Geometry" / "SingleFootingGeometry.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SingleFootingGeometrySmoke.cs"
CONTRACT = ROOT / "src" / "QS3D.BricsCAD.V25" / "SingleFootingContract.cs"
DIALOG = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "SingleFootingDimensionsDialog.cs"
WORKSPACE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.SingleFooting.cs"
BLT_WORKSPACE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.Blt3dFamilyWorkspace.cs"
COMMAND = ROOT / "src" / "QS3D.BricsCAD.V25" / "SingleFootingCommands.cs"
DISPATCH = ROOT / "src" / "QS3D.BricsCAD.V25" / "ActiveFamilyQuickDrawCommands.cs"
V26 = ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj"


def require(condition, message):
    if not condition:
        raise SystemExit("ERROR: " + message)


def reject(condition, message):
    if condition:
        raise SystemExit("ERROR: " + message)


def read(path):
    require(path.is_file(), "missing " + str(path.relative_to(ROOT)))
    return path.read_text(encoding="utf-8")


core = read(CORE)
smoke = read(SMOKE)
contract = read(CONTRACT)
dialog = read(DIALOG)
workspace = read(WORKSPACE)
blt_workspace = read(BLT_WORKSPACE)
command = read(COMMAND)
dispatch = read(DISPATCH)
v26 = read(V26)

# Host-neutral dimensions/validation stay deterministic and testable on hosted CI.
for token in (
    "class SingleFootingDimensions",
    "L1M",
    "W1M",
    "L2M",
    "W2M",
    "H1M",
    "H2M",
    "L2M > L1M",
    "W2M > W1M",
    "VolumeM3",
    "HasTaper",
):
    require(token in core, "single footing geometry contract lost " + token)
require("new SingleFootingDimensions(1.6d, 1.6d, 1d, 1d, 1d, 0d)" in smoke,
        "single footing default smoke case drifted")
require("ExpectInvalid" in smoke and "double.NaN" in smoke,
        "single footing invalid-input smoke coverage is incomplete")

# Family settings use the stable subtype identity and canonical persisted keys while retaining
# the 10221-era shadow keys for backward compatibility.
for token in (
    'SubtypeName = "Móng đơn"',
    'CategoryCode = "Foundation.SingleFooting"',
    'TreeTag = CategoryCode',
    'CategoryCodeKey = "CategoryCode"',
    'MarkerKey = "SingleFootingSubtype"',
    'L1Key = "SINGLE_FOOTING_L1"',
    'W1Key = "SINGLE_FOOTING_W1"',
    'L2Key = "SINGLE_FOOTING_L2"',
    'W2Key = "SINGLE_FOOTING_W2"',
    'H1Key = "SINGLE_FOOTING_H1"',
    'H2Key = "SINGLE_FOOTING_H2"',
    'LegacyL1Key = "SingleFootingL1M"',
    'LegacyW1Key = "SingleFootingW1M"',
    'LegacyL2Key = "SingleFootingL2M"',
    'LegacyW2Key = "SingleFootingW2M"',
    'LegacyH1Key = "SingleFootingH1M"',
    'LegacyH2Key = "SingleFootingH2M"',
    "new SingleFootingDimensions(1.6d, 1.6d, 1d, 1d, 1d, 0d)",
    "family.Properties[CategoryCodeKey] = CategoryCode",
    'family.Properties["ThicknessM"]',
):
    require(token in contract, "single footing persistent Family contract lost " + token)

# The Add surface must be the six-value reference-style dialog, not the generic chooser.
for key in ("L1", "W1", "L2", "W2", "H1", "H2"):
    require('AddInput(canvas, "' + key + '"' in dialog, "dimension dialog lost input " + key)
require("SingleFootingContract.Defaults" in dialog, "dimension dialog no longer loads canonical defaults")
require("DialogResult = true" in dialog, "dimension dialog OK path is missing")
require("IsCancel = true" in dialog, "dimension dialog Cancel path is missing")

for token in (
    "EnsureSingleFootingTreeNode",
    "foundation.Items.Insert(0, existing)",
    "existing.Tag = SingleFootingContract.CategoryCode;",
    "new SingleFootingDimensionsDialog()",
    "if (dialog.ShowDialog() != true || dialog.Dimensions == null)",
    "CreateSingleFootingFamily",
    "SingleFootingContract.Apply(family, dimensions)",
    "ProjectFamilyActivationService.SetActive",
):
    require(token in workspace, "Workspace Móng đơn integration lost " + token)

# Móng đơn Add must route synchronously inside the canonical BLT3D Add handler. The older
# ContextIdle/class-handler rewiring path is forbidden because it can fall through to generic
# Foundation creation.
for token in (
    "DispatcherPriority.ContextIdle",
    "OnSingleFootingAwareAddClick",
    "button.Click += OnSingleFootingAwareAddClick",
):
    reject(token in workspace, "deferred/rewired SingleFooting Add route returned: " + token)

add_start = blt_workspace.find("private void OnBlt3dFamilyAddClick(object sender, RoutedEventArgs e)")
add_end = blt_workspace.find("private void ShowBlt3dFamilyModeChooser", add_start)
add_method = blt_workspace[add_start:add_end] if add_start >= 0 and add_end > add_start else ""
ordered = (
    "e.Handled = true;",
    "if (IsSingleFootingSelected())",
    "HandleSingleFootingAdd(e);",
    "return;",
    "ShowBlt3dFamilyModeChooser();",
)
last = -1
for token in ordered:
    pos = add_method.find(token)
    require(pos >= 0, "direct BLT Add route lost " + token)
    require(pos > last, "direct BLT Add route is out of order at " + token)
    last = pos

# Quick Draw dispatch is subtype-specific; other Foundation Families keep the existing path.
require("SingleFootingContract.IsSingleFooting(family)" in dispatch,
        "Active Family dispatcher no longer recognizes Móng đơn")
require("new SingleFootingCommands().DrawSingleFooting()" in dispatch,
        "Active Family dispatcher no longer routes Móng đơn to center-pick authoring")
require("new DirectDrawP1Commands().DrawFoundation()" in dispatch,
        "generic Foundation Quick Draw fallback was removed")
require("new DirectDrawP1Commands().DrawFoundationAdvanced()" in dispatch,
        "generic Foundation Advanced fallback was removed")

# Native authoring contract: repeated center picks, active floor Z, semantic source, native owned Solid3d.
for token in (
    '[CommandMethod("QS3DDRAWSINGLEFOOTING"',
    "while (true)",
    "document.Editor.GetPoint(prompt)",
    "PromptStatus.None",
    "PromptStatus.Cancel",
    "ResolveActiveFloorElevation(project)",
    "CreateFootprint(document",
    "EntitySnapshotReader.ReadHandles",
    "SemanticCaptureService.CaptureSnapshot",
    "SingleFootingContract.Apply(element, dimensions)",
    "CreateLoftedSolid",
    "BooleanOperation(BooleanOperationType.BoolUnite",
    "GeneratedGeometryService.MarkGenerated",
    "GeneratedGeometryService.CommitReplacement",
    "ProjectStateSnapshot.Capture",
    "rollback.Restore(project)",
):
    require(token in command, "single footing native authoring lost " + token)

# V26 intentionally links V25 adapter/UI sources; the feature must remain shared rather than forked.
require('Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"' in v26,
        "V26 no longer links the shared V25 C# source tree")
require('Page Include="..\\QS3D.BricsCAD.V25\\UI\\**\\*.xaml"' in v26,
        "V26 no longer links the shared V25 UI source tree")

print("PASS: Móng đơn tree/Add/dialog/Family/center-pick/native-ownership workflow is source-guarded")
