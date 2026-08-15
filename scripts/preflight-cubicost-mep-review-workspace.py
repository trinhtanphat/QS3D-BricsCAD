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
        "BricsApplication.ShowModelessWindow(window)",
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

for label in ("provider", "workspace", "zoom"):
    for forbidden in (
        "OpenMode.ForWrite",
        "AppendEntity",
        "AppendEntityToModelSpace",
        "Erase(",
        "TransformBy(",
        "BooleanOperation(",
        "ProjectContextCoordinator.GetOrCreate",
        "ProjectContextCoordinator.SetCurrent",
        "QsdbProjectStore",
        "Task.Run",
        "Parallel.For",
    ):
        if forbidden in texts[label]:
            errors.append(f"{label}: forbidden native/project mutation or threading token {forbidden!r}")

# The modeless workspace must not retain native document/object state in fields.
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
    if forbidden in texts["workspace"]:
        errors.append(f"workspace: forbidden retained native field {forbidden!r}")

# Runtime recognition must be centralized instead of recreating independent defaults in commands.
for label in ("takeoff", "exact", "highlight"):
    if "private static readonly MepRecognitionProfile RecognitionProfile = MepRecognitionProfiles.CreateDefault();" in texts[label]:
        errors.append(f"{label}: stale independent default recognition profile")

if errors:
    print("Cubicost MEP review workspace preflight: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("Cubicost MEP review workspace preflight: PASS")
