#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
REPEATED = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawRepeatedCommands.cs"
JIG = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawProfileStripJig.cs"
DIRECT = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawCommands.cs"
UNDO = ROOT / "src/QS3D.BricsCAD.V25/SourceReconcileUndoCoordinator.cs"
STRUCTURAL = ROOT / "src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs"
ACTIVE = ROOT / "src/QS3D.BricsCAD.V25/ActiveFamilyQuickDrawCommands.cs"
RIBBON = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/QuickWorkflowRibbonAugmenter.cs"
WORKSPACE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.QuickDraw.cs"
PROBE = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawRepeatedRuntimeProbeCommands.cs"
RUNNER = ROOT / "scripts/test-bricscad-direct-draw-repeated-mode.ps1"
DOC = ROOT / "docs/DIRECT-DRAW-WORKFLOW.md"
ACTIVE_DOC = ROOT / "docs/DIRECT-DRAW-ACTIVE-FAMILY.md"

paths = (REPEATED, JIG, DIRECT, UNDO, STRUCTURAL, ACTIVE, RIBBON, WORKSPACE, PROBE, RUNNER, DOC, ACTIVE_DOC)
errors = []
for path in paths:
    if not path.is_file():
        errors.append("missing repeated Direct Draw dependency: " + str(path.relative_to(ROOT)))


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        errors.append(label + " missing: " + token)


if not errors:
    repeated = REPEATED.read_text(encoding="utf-8")
    jig = JIG.read_text(encoding="utf-8")
    direct = DIRECT.read_text(encoding="utf-8")
    undo = UNDO.read_text(encoding="utf-8")
    structural = STRUCTURAL.read_text(encoding="utf-8")
    active = ACTIVE.read_text(encoding="utf-8")
    ribbon = RIBBON.read_text(encoding="utf-8")
    workspace = WORKSPACE.read_text(encoding="utf-8")
    probe = PROBE.read_text(encoding="utf-8")
    runner = RUNNER.read_text(encoding="utf-8")
    doc = DOC.read_text(encoding="utf-8")
    active_doc = ACTIVE_DOC.read_text(encoding="utf-8")

    for command in ("QS3DDRAWWALLREPEAT", "QS3DDRAWBEAMREPEAT"):
        if len(re.findall(r'CommandMethod\("' + command + r'"', repeated)) != 1:
            errors.append(command + " must be declared exactly once")
    if len(re.findall(r'CommandMethod\("QS3DDRAWACTIVEREPEAT"', active)) != 1:
        errors.append("QS3DDRAWACTIVEREPEAT must be declared exactly once")

    for token in (
        "DirectDrawProfileStripJig(",
        "editor.Drag(jig)",
        "DirectDrawProjectPreviewContext.Capture(document)",
        "RequireRepeatedPromptContextUnchanged(",
        "RequireExpectedFamily(",
        "DirectDrawCommands.ExecuteDirect(",
        "DirectDrawCommands.CreateLineWcs(document, startWcs, endWcs)",
        "ProjectStateSnapshot.Capture(project)",
        "ProjectRevisionStamp.Capture(project)",
        "BeginExternalTransitionScope(document)",
        "CommitExternalTransition(",
        "EraseRepeatedDirectDrawCad(",
        '"ESC_OR_CANCEL"',
        'undo_scope=WholeCommand',
    ):
        require(repeated, token, "repeated command")

    scope = repeated.find("using (SourceReconcileUndoCoordinator.BeginExternalTransitionScope(document))")
    loop = repeated.find("while (true)", scope)
    preview = repeated.find("DirectDrawProjectPreviewContext.Capture(document)", loop)
    drag = repeated.find("editor.Drag(jig)", preview)
    execute = repeated.find("DirectDrawCommands.ExecuteDirect(", drag)
    commit = repeated.find("SourceReconcileUndoCoordinator.CommitExternalTransition(", execute)
    if min(scope, loop, preview, drag, execute, commit) < 0 or not scope < loop < preview < drag < execute < commit:
        errors.append("repeated command must scope Undo, recapture context, drag transient, commit canonically, then publish one outer transition")

    for token in (
        "internal sealed class DirectDrawProfileStripJig : DrawJig",
        "prompts.AcquirePoint(options)",
        "UserInputControls.NullResponseAccepted",
        "BasePoint = _startWcs",
        "_wcsToUcs = ucsToWcs.Inverse()",
        "worldDraw.Geometry.WorldLine(_startWcs, _endWcs)",
    ):
        require(jig, token, "shared profile jig")
    for forbidden in (
        "StartTransaction(", "OpenMode.ForWrite", "AppendEntity(", ".Erase(",
        "ProjectContextCoordinator", "SemanticCaptureService", "GeneratedGeometryService",
    ):
        if forbidden in jig:
            errors.append("shared profile jig must remain database/ownership-free: " + forbidden)
    if jig.count("worldDraw.Geometry.WorldLine") != 5:
        errors.append("shared profile jig must draw exactly four strip edges plus one center line")

    for token in (
        "internal static DirectDrawCommitResult ExecuteDirect(",
        "Action<ProjectState>? beforeMutation = null",
        "internal static ObjectId CreateLineWcs(",
        "transformFromUcs: false",
        "internal static void RequireRepeatedPromptContextUnchanged(",
        "internal static void RequireRepeatedModelSpace(",
        "internal static void EraseRepeatedDirectDrawCad(",
    ):
        require(direct, token, "canonical Direct Draw bridge")

    for token in (
        "BeginExternalTransitionScope(Document document)",
        "IsExternalTransitionActive(Document document)",
        "CommitExternalTransition(",
        "transition.StageNativeMarker();",
        "transition.StageAfter(project, afterSnapshot);",
        "transaction.Commit();",
        "transition.ConfirmCommitted();",
    ):
        require(undo, token, "whole-command Undo coordinator")
    for token in (
        "undoTransition == null && !SourceReconcileUndoCoordinator.IsExternalTransitionActive(document)",
        "if (!SourceReconcileUndoCoordinator.IsExternalTransitionActive(document))",
        "undoTransition.StageAfter(project, afterSnapshot);",
    ):
        require(structural, token, "structural nested-marker suppression")

    for token in (
        'CommandMethod("QS3DDRAWACTIVEREPEAT"',
        "DispatchRepeated(",
        "new DirectDrawRepeatedCommands().DrawActiveFamilyRepeated(",
        "family.Category != ElementCategory.ArchitecturalWall",
        "family.Category != ElementCategory.Beam",
    ):
        require(active, token, "active-Family repeated dispatcher")
    require(ribbon, 'new ButtonSpec("QS3D_AUTHOR_DRAW_ACTIVE_REPEAT", "Vẽ Liên tục", "QS3DDRAWACTIVEREPEAT")', "Ribbon")
    for token in (
        "Vẽ liên tục (Ctrl+Alt+D)",
        "ModifierKeys.Control | ModifierKeys.Alt",
        'Send("QS3DDRAWACTIVEREPEAT")',
    ):
        require(workspace, token, "Workspace repeated gesture")

    for token in (
        'CommandMethod("QS3DREPEATVERIFYAFTER"',
        'CommandMethod("QS3DREPEATVERIFYUNDO"',
        'CommandMethod("QS3DREPEATVERIFYREDO"',
        'CommandMethod("QS3DREPEATVERIFYCOLD"',
        'CommandMethod("QS3DREPEATARMESC"',
        'CommandMethod("QS3DREPEATVERIFYESC"',
        'CommandMethod("QS3DREPEATVERIFYUCS"',
        'CommandMethod("QS3DREPEATARMSWITCH"',
        'CommandMethod("QS3DREPEATVERIFYSWITCH"',
        '"QS3DREPEATVERIFYESC _.QUIT _N "',
        '"QS3DREPEATVERIFYSWITCH _.QUIT _N "',
        "RequireTwoBeamSegments(",
        "RequireOneEscBeamSegment(",
        "SegmentCommittedForRuntimeQualification",
        "SequenceCompletedForRuntimeQualification",
        "CadHandleService.GetLiveSolidHandles",
        "ProjectContextCoordinator.TryGetReadOnly",
        "QS3D_DIRECT_DRAW_REPEAT_RUNTIME_V1",
    ):
        require(probe, token, "read-only runtime verifier")
    for forbidden in (
        "StartTransaction()", "OpenMode.ForWrite", "AppendEntity(", ".Erase(",
        "GetOrCreate", "SemanticCaptureService.Capture", "StructuralSolidBuilder.BuildSelected",
    ):
        if forbidden in probe:
            errors.append("runtime verifier must remain read-only: " + forbidden)

    for token in (
        "[ValidateSet(25, 26)]",
        "status --porcelain=v1 --untracked-files=all",
        "Assert-Qs3dExactSourceIdentity",
        '"NETLOAD"',
        '"QS3DDRAWBEAMREPEAT"',
        '"_.U"',
        '"_.REDO"',
        '"QS3DREPEATVERIFYAFTER"',
        '"QS3DREPEATVERIFYUNDO"',
        '"QS3DREPEATVERIFYREDO"',
        '"QS3DREPEATVERIFYCOLD"',
        '"QS3DREPEATARMESC"',
        '"QS3DREPEATVERIFYUCS"',
        '"QS3DREPEATARMSWITCH"',
        "Send-ExactProcessEscape",
        "Send-ExactProcessCtrlTab",
        "GetWindowThreadProcessId",
        "foregroundProcessId -eq [uint32]$Process.Id",
        "keybd_event(0x1B",
        "Assert-V26DotNetRuntime",
        'SetEnvironmentVariable("DOTNET_ROOT", $dotNetRoot, "Process")',
        'SetEnvironmentVariable("DOTNET_ROOT_X64", $dotNetRoot, "Process")',
        'SetEnvironmentVariable("DOTNET_ROOT", $oldDotNetRoot, "Process")',
        'SetEnvironmentVariable("DOTNET_ROOT_X64", $oldDotNetRootX64, "Process")',
        "Microsoft.WindowsDesktop.App",
        "PresentationFramework.dll",
        "-WindowStyle Hidden",
        '"QS3DSAVE"',
        '"_.QSAVE"',
        "repeat-session2.private.scr",
        "repeat-planar-ucs.private.scr",
        "repeat-switch.private.scr",
        "private cleanup target",
        "Repository fixture changed",
    ):
        require(runner, token, "guarded V25/V26 repeated-mode runner")
    for forbidden in ("git reset", "git clean", "Get-Process -Name '*'", "Stop-Process -Name"):
        if forbidden in runner:
            errors.append("repeated-mode runner contains unsafe broad operation: " + forbidden)

    for token in (
        "QS3DDRAWWALLREPEAT", "QS3DDRAWBEAMREPEAT", "QS3DDRAWACTIVEREPEAT",
        "DrawJig", "whole-command semantic/native Undo", "not a second authoring engine",
    ):
        require(doc, token, "Direct Draw workflow documentation")
    for token in ("QS3DDRAWACTIVEREPEAT", "Ctrl+Alt+D", "Vẽ Liên tục"):
        require(active_doc, token, "active-Family documentation")

if errors:
    print("Production repeated Direct Draw preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("PASS: production Wall/Beam repeated Direct Draw uses one database-free DrawJig transient, canonical per-segment source/semantic/native ownership, per-segment context freshness and one whole-command semantic/native Undo transition with read-only V25/V26 runtime verification hooks.")
