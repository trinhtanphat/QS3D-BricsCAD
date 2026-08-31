#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FILES = {
    "provider": ROOT / "src/QS3D.BricsCAD.V25/MepRecognitionProfileProvider.cs",
    "workspace": ROOT / "src/QS3D.BricsCAD.V25/MepReviewWorkspaceCommands.cs",
    "zoom": ROOT / "src/QS3D.BricsCAD.V25/MepZoomCommands.cs",
    "takeoff": ROOT / "src/QS3D.BricsCAD.V25/MepTakeoffCommands.cs",
    "exact": ROOT / "src/QS3D.BricsCAD.V25/MepExactClashCommands.cs",
    "highlight": ROOT / "src/QS3D.BricsCAD.V25/MepExactClashReviewCommands.cs",
    "doc": ROOT / "docs/CUBICOST-MEP-REVIEW-WORKSPACE-V25.md",
}
errors = []

for label, path in FILES.items():
    if not path.exists():
        errors.append(f"missing {label}: {path.relative_to(ROOT)}")

if errors:
    print("Cubicost MEP review workspace preflight: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

texts = {label: path.read_text(encoding="utf-8") for label, path in FILES.items()}

required = {
    "provider": [
        "MepRecognitionProfileProvider",
        "Environment.SpecialFolder.ApplicationData",
        "mep-recognition-profile.xml",
        "DtdProcessing.Prohibit",
        "XmlResolver = null",
        "MaxProfileBytes",
        "MaxRules",
        "MaxTokensPerRule",
        "File.Replace(tempPath, path, backupPath, true)",
        "MepRecognitionProfiles.CreateDefault()",
    ],
    "workspace": [
        '[CommandMethod("QS3DMEPREVIEW")]',
        "private static MepReviewWorkspaceWindow? _published;",
        "private static MepReviewWorkspaceWindow? _pending;",
        "var pending = _pending;",
        "if (pending != null && !TryClosePendingWindow(pending))",
        "var published = _published;",
        "published.IsLoaded",
        "ReleasePublishedWindow(published)",
        "_pending = window;",
        "window.Closed += (_, __) => ReleaseWindow(window)",
        "BricsApplication.ShowModelessWindow(window)",
        "if (!window.IsLoaded)",
        "_published = window;",
        "ReleasePendingWindow(window)",
        "candidate = null;",
        "TryClosePendingWindow(candidate)",
        "if (window.IsLoaded) return false;",
        "ex.GetType().Name",
        "MepRecognitionProfileProvider.Save(profile)",
        "MepRecognitionProfileProvider.Reload()",
        "DocumentManager.MdiActiveDocument",
        "SendStringToExecute(command + \" \", true, false, false)",
        "QS3DMEPTAKEOFF",
        "QS3DMEPCLASH",
        "QS3DMEPCLASHLOCATE",
        "QS3DMEPEXACTCLASH",
        "QS3DMEPEXACTCLASHHIGHLIGHT",
        "QS3DMEPZOOMSELECTION",
    ],
    "zoom": [
        '[CommandMethod("QS3DMEPZOOMSELECTION", CommandFlags.UsePickSet)]',
        "EntitySnapshotReader.ReadCurrentSelection(document)",
        "CadHandleService.Resolve(document, handles)",
        "OpenMode.ForRead",
        "entity.GeometricExtents",
        "GetCurrentView()",
        "Matrix3d.PlaneToWorld(view.ViewDirection)",
        ".TransformBy(worldToDisplay)",
        "view.CenterPoint",
        "view.Width",
        "view.Height",
        "SetCurrentView(view)",
    ],
    "takeoff": ["MepRecognitionProfileProvider.Current"],
    "exact": ["MepRecognitionProfileProvider.Current"],
    "highlight": ["MepRecognitionProfileProvider.Current"],
    "doc": [
        "QS3DMEPREVIEW",
        "QS3DMEPZOOMSELECTION",
        "mep-recognition-profile.xml",
        "PENDING_LOCAL / DO_NOT_RETRY_REMOTE",
        "QS3D-Platform",
        "QS3D-AutoCAD",
        "QS3D-CAD",
    ],
}

for label, tokens in required.items():
    for token in tokens:
        if token not in texts[label]:
            errors.append(f"{label}: missing required token {token!r}")

workspace = texts["workspace"]
for forbidden in (
    "private static MepReviewWorkspaceWindow? _window;",
    "if (_window.IsVisible)",
    "if (published.IsVisible)",
    "TryCloseUnpublishedWindow",
    '"\\nQS3DMEPREVIEW error: " + ex.Message',
    '"Không queue được " + command + ": " + ex.Message',
):
    if forbidden in workspace:
        errors.append(f"workspace: stale/unsafe publication token {forbidden!r}")

show_start = workspace.find("public void ShowReviewWorkspace()")
release_start = workspace.find("private static void ReleaseWindow", show_start + 1)
show = workspace[show_start:release_start] if show_start >= 0 and release_start > show_start else ""
ordered = [
    "var pending = _pending;",
    "candidate = new MepReviewWorkspaceWindow();",
    "_pending = window;",
    "window.Closed += (_, __) => ReleaseWindow(window);",
    "BricsApplication.ShowModelessWindow(window);",
    "if (!window.IsLoaded)",
    "_published = window;",
    "ReleasePendingWindow(window);",
]
positions = [show.find(token) for token in ordered]
if any(position < 0 for position in positions) or positions != sorted(positions) or len(set(positions)) != len(positions):
    errors.append("workspace: publication order must be drain pending -> construct -> pending-own -> Closed -> host show -> Loaded check -> publish -> release pending")
else:
    release_pending_position = positions[-1]
    cleanup_transfer_position = show.find("candidate = null;", release_pending_position + len(ordered[-1]))
    if cleanup_transfer_position < 0 or cleanup_transfer_position <= release_pending_position:
        errors.append("workspace: cleanup ownership must transfer only after pending ownership is released")

if workspace.count("_published = window;") != 1:
    errors.append("workspace: authoritative publication must have exactly one assignment")
if workspace.count("_pending = window;") != 1:
    errors.append("workspace: pending ownership must have exactly one assignment")

close_start = workspace.find("private static bool TryClosePendingWindow")
class_start = workspace.find("internal sealed class MepReviewWorkspaceWindow", close_start + 1)
close_body = workspace[close_start:class_start] if close_start >= 0 and class_start > close_start else ""
for token in (
    "if (!ReferenceEquals(_pending, window)) return true;",
    "if (ReferenceEquals(_published, window))",
    "try { window.Close(); } catch (System.Exception) { }",
    "if (window.IsLoaded) return false;",
    "ReleasePendingWindow(window);",
):
    if token not in close_body:
        errors.append(f"workspace: pending-close fail-closed missing {token!r}")

for label in ("provider", "workspace", "zoom"):
    for forbidden in (
        "OpenMode.ForWrite",
        "AppendEntity",
        "AppendEntityToModelSpace",
        "Erase(",
        "entity.TransformBy(",
        "BooleanOperation(",
        "ProjectContextCoordinator.GetOrCreate",
        "ProjectContextCoordinator.SetCurrent",
        "QsdbProjectStore",
        "Task.Run",
        "Parallel.For",
    ):
        if forbidden in texts[label]:
            errors.append(f"{label}: forbidden native/project mutation or threading token {forbidden!r}")

for forbidden in (
    "private readonly Document",
    "private Document",
    "private readonly ObjectId",
    "private ObjectId",
    "private readonly DBObject",
    "private DBObject",
    "private readonly Solid3d",
    "private Solid3d",
):
    if forbidden in workspace:
        errors.append(f"workspace: forbidden retained native field {forbidden!r}")

for label in ("takeoff", "exact", "highlight"):
    if "private static readonly MepRecognitionProfile RecognitionProfile = MepRecognitionProfiles.CreateDefault();" in texts[label]:
        errors.append(f"{label}: stale independent default recognition profile")

if errors:
    print("Cubicost MEP review workspace preflight: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("Cubicost MEP review workspace preflight: PASS")
