#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src" / "QS3D.BricsCAD.V25" / "SingleFootingCommands.cs"


def require(condition, message):
    if not condition:
        raise SystemExit("ERROR: " + message)


def reject(condition, message):
    if condition:
        raise SystemExit("ERROR: " + message)


require(COMMAND.is_file(), "missing " + str(COMMAND.relative_to(ROOT)))
command = COMMAND.read_text(encoding="utf-8")

interactive_start = command.find("public void DrawSingleFooting()")
bridge_start = command.find("internal static string PlaceActiveSingleFootingAt(Document document, Point3d center)")
place_start = command.find("private static string PlaceOne(", bridge_start)
build_start = command.find("private static Solid3d BuildSolid(", place_start)

require(interactive_start >= 0, "interactive Móng đơn command is missing")
require(bridge_start > interactive_start, "one-shot Móng đơn bridge is missing or misplaced")
require(place_start > bridge_start, "one-shot bridge no longer shares PlaceOne authoring")
require(build_start > place_start, "PlaceOne boundary is missing")

interactive = command[interactive_start:bridge_start]
bridge = command[bridge_start:place_start]
place = command[place_start:build_start]

# Preserve the human repeated-pick command. The automation bridge must not steal or rewrite it.
for token in (
    "while (true)",
    "document.Editor.GetPoint(prompt)",
    "PromptStatus.None",
    "PromptStatus.Cancel",
    "RequireCurrentContext(document, expectedProjectId, expectedFamilyId, dimensions)",
    "PlaceOne(document, project, family, dimensions, point.Value)",
):
    require(token in interactive, "interactive Móng đơn workflow lost " + token)

# The one-shot bridge is deliberately prompt-free and reuses the exact active Family dimensions.
for token in (
    "RequireFiniteCenter(center)",
    "RequireModelSpace(document)",
    'ExistingProjectMutationContext.Require(document, "Đặt Móng đơn")',
    "ProjectFamilyActivationService.GetActive(project)",
    "SingleFootingContract.IsSingleFooting(family)",
    "SingleFootingContract.Read(family!)",
    "RequireCurrentContext(document, project.ProjectId, family!.Id, dimensions)",
    "return PlaceOne(document, project, family, dimensions, center);",
):
    require(token in bridge, "one-shot Móng đơn bridge lost " + token)
reject("Editor.GetPoint" in bridge, "one-shot Móng đơn bridge must not prompt for a point")
reject("SendStringToExecute" in bridge, "one-shot Móng đơn bridge must not queue an interactive command")

# Shared authoring must continue to own semantic capture, native generated geometry and rollback.
for token in (
    "ProjectStateSnapshot.Capture(project)",
    "CreateFootprint(document",
    "SemanticCaptureService.CaptureSnapshot",
    "SingleFootingContract.Apply(element, dimensions)",
    "GeneratedGeometryService.MarkGenerated",
    "GeneratedGeometryService.CommitReplacement",
    "rollback.Restore(project)",
    "EraseIfLive(document, sourceId)",
    "return generatedHandle;",
):
    require(token in place, "shared Móng đơn authoring lost " + token)

require("private static void RequireFiniteCenter(Point3d center)" in command,
        "finite Móng đơn center validation is missing")
require("double.IsNaN(value)" in command and "double.IsInfinity(value)" in command,
        "finite Móng đơn center validation is incomplete")

print("PASS: Móng đơn keeps repeated human picks and exposes a prompt-free shared one-shot authoring bridge")
