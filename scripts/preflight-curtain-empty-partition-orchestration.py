from pathlib import Path
import re
import sys


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "CurtainWallBuildCommands.cs"
text = SOURCE.read_text(encoding="utf-8")
errors: list[str] = []

start = text.find('public void BuildCurtain3D()')
end = text.find('private static void ApplySelection', start)
if start < 0 or end < 0:
    errors.append("Cannot isolate the QS3DCURTAIN3D command body")
    body = text
else:
    body = text[start:end]

line_guard = 'if (validatedSelection.LineSourceIds.Count > 0)'
path_guard = 'if (validatedSelection.PathSourceIds.Count > 0)'
if body.count(line_guard) != 3:
    errors.append("QS3DCURTAIN3D must guard exactly the three LINE host/frame/panel builders")
if body.count(path_guard) != 3:
    errors.append("QS3DCURTAIN3D must guard exactly the three path host/frame/panel builders")

phases = (
    (
        "LINE host replacement",
        "LineSourceIds",
        "lineHostSolids = WallSolidBuilder.BuildSelectedLineWalls(document, project, ElementCategory.GlassWall);",
        "CurtainWallBuildFailureInjection.LineHost",
    ),
    (
        "open-POLYLINE host replacement",
        "PathSourceIds",
        "pathHostSolids = PolylineWallSolidBuilder.BuildSelected(document, project, ElementCategory.GlassWall);",
        "CurtainWallBuildFailureInjection.PathHost",
    ),
    (
        "LINE frame replacement",
        "LineSourceIds",
        "lineFrames = CurtainWallFrameSolidBuilder.BuildSelectedLineWalls(document, project);",
        "CurtainWallBuildFailureInjection.LineFrame",
    ),
    (
        "open/bulged path frame replacement",
        "PathSourceIds",
        "pathFrames = CurtainWallPathFrameSolidBuilder.BuildSelectedOpenPolylines(document, project);",
        "CurtainWallBuildFailureInjection.PathFrame",
    ),
    (
        "LINE panel replacement",
        "LineSourceIds",
        "linePanels = CurtainWallPanelSolidBuilder.BuildSelectedLineWalls(document, project);",
        "CurtainWallBuildFailureInjection.LinePanel",
    ),
    (
        "open/bulged path panel replacement",
        "PathSourceIds",
        "pathPanels = CurtainWallPathPanelSolidBuilder.BuildSelectedOpenPolylines(document, project);",
        "CurtainWallBuildFailureInjection.PathPanel",
    ),
)

cursor = 0
for phase, partition, call, hook in phases:
    pattern = re.compile(
        rf'phase = "{re.escape(phase)}";\s*'
        rf'if \(validatedSelection\.{partition}\.Count > 0\)\s*\{{\s*'
        rf'ApplySelection\(document, validatedSelection\.{partition}\);\s*'
        rf'{re.escape(call)}\s*\}}\s*'
        rf'CurtainWallBuildFailureInjection\.ThrowIfArmed\({re.escape(hook)}\);',
        re.DOTALL,
    )
    match = pattern.search(body, cursor)
    if match is None:
        errors.append("Missing guarded Curtain phase or adjacent failure hook: " + phase)
    else:
        cursor = match.end()

for token in (
    'CurtainWallBuildSelectionGuard.Validate(document, project)',
    'ProjectStateSnapshot.Capture(project)',
    'CurtainWallUndoCoordinator.BeginTransition(document, project, undoBefore)',
    'using (var commandTransaction = document.Database.TransactionManager.StartTransaction())',
    'undoTransition.StageAfter(project, commandTransaction, undoAfter)',
    'commandTransaction.Commit();',
    'undoTransition?.ConfirmCommitted();',
    'ApplySelection(document, validatedSelection.AllSourceIds);',
    'CurtainWallFrameLiveStateService.TryStampSelected(document, project, out stampWarning)',
    'CurtainWallPanelLiveStateService.TryStampSelected(document, project, out panelStampWarning)',
    'FinalizeUi(document, hostSolids, frameSolids, panelSolids, checked(stamped + panelsStamped), regenerated, stampWarning)',
    'TryRestoreSelection(document, validatedSelection)',
):
    if token not in body:
        errors.append("QS3DCURTAIN3D lost an atomicity/Undo/selection/post-commit invariant: " + token)

if 'Editor.GetSelection' in body:
    errors.append("QS3DCURTAIN3D must not add a second interactive selection after canonical prevalidation")

print("QS3D Curtain empty-partition orchestration preflight")
if errors:
    for error in errors:
        print("ERROR: " + error)
    sys.exit(1)

print(
    "PASS: QS3DCURTAIN3D skips absent LINE/path host, frame and panel builders after canonical partitioning while preserving six-phase order, failure hooks, outer transaction, Curtain Undo registration, full-selection restore and post-commit stamping/UI boundaries."
)
