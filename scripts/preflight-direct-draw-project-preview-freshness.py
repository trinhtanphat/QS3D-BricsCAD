#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"
HELPER = SRC / "Services" / "DirectDrawProjectPreviewContext.cs"
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"
DOC = ROOT / "docs" / "DIRECT-DRAW-PREVIEW-PROJECT-FRESHNESS.md"

errors = []


def read(path):
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


helper = read(HELPER)
for token in (
    "using System.IO;",
    "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
    "ExistingProjectMutationContext.Require(document, operation)",
    "string.Equals(project.ProjectId, ExpectedProjectId, StringComparison.OrdinalIgnoreCase)",
    "ProjectContextCoordinator.TryGetReadOnly(document, out _)",
    "private static bool HasBackingStore(Document document)",
    "ProjectContextCoordinator.GetProjectPath(document)",
    'File.Exists(path) || File.Exists(path + ".bak")',
    "var created = ProjectContextCoordinator.GetOrCreate(document);",
    "ProjectContextCoordinator.Forget(document);",
    "project đã thay đổi trong lúc xác nhận Direct Draw",
    "project đã xuất hiện trong lúc xác nhận Direct Draw",
):
    if token not in helper:
        errors.append("preview helper missing: " + token)

projectless_probe = helper.find("ProjectContextCoordinator.TryGetReadOnly(document, out _) || HasBackingStore(document)")
projectless_bind = helper.find("var created = ProjectContextCoordinator.GetOrCreate(document);", projectless_probe)
post_bind_probe = helper.find("if (HasBackingStore(document))", projectless_bind)
forget = helper.find("ProjectContextCoordinator.Forget(document);", post_bind_probe)
if projectless_probe < 0 or projectless_bind < projectless_probe or post_bind_probe < projectless_bind or forget < post_bind_probe:
    errors.append("projectless Direct Draw must check backing-store absence before bind, recheck after GetOrCreate, then forget any speculative bind before refusing")

families = {
    "DirectDrawCommands.cs": {
        "advanced": ["QS3DDRAWWALLADV", "QS3DDRAWBEAMADV", "QS3DDRAWSLABADV", "QS3DDRAWCOLUMNADV"],
        "captures": 4,
        "resolver": "projectPreview.ResolveForMutation(document, operation)",
        "snapshot": "ProjectStateSnapshot.Capture(project)",
    },
    "DirectDrawP1Commands.cs": {
        "advanced": ["QS3DDRAWGLASSWALLADV", "QS3DDRAWWALLPIERADV", "QS3DDRAWSTRUCTWALLADV", "QS3DDRAWFOUNDATIONADV"],
        "captures": 4,
        "resolver": "projectPreview.ResolveForMutation(document, operation)",
        "snapshot": "ProjectStateSnapshot.Capture(project)",
    },
    "DirectDrawOpeningCommands.cs": {
        "advanced": ["QS3DDRAWDOORADV", "QS3DDRAWOPENINGADV"],
        "captures": 1,
        "resolver": "projectPreview.ResolveForMutation(document, operation)",
        "snapshot": "ProjectStateSnapshot.Capture(project)",
    },
    "DirectDrawWindowCommands.cs": {
        "advanced": ["QS3DDRAWWINDOWADV"],
        "captures": 1,
        "resolver": "BindProjectAfterPrompts(document, projectPreview, expectedProjectChangeVersion, operation)",
        "snapshot": "ProjectStateSnapshot.Capture(project)",
    },
    "DirectDrawReferenceWallCommands.cs": {
        "advanced": ["QS3DDRAWWALLREFADV"],
        "captures": 1,
        "resolver": "projectPreview.ResolveForMutation(document, operation)",
        "snapshot": "ProjectStateSnapshot.Capture(project)",
    },
}

for filename, contract in families.items():
    text = read(SRC / filename)
    for command in contract["advanced"]:
        if 'CommandMethod("' + command + '"' not in text:
            errors.append(filename + ": missing command " + command)
    capture = "DirectDrawProjectPreviewContext.Capture(document)"
    if text.count(capture) < contract["captures"]:
        errors.append(filename + ": not all prompt-bearing Direct Draw flows capture project preview identity")
    resolver = contract["resolver"]
    snapshot = contract["snapshot"]
    resolver_pos = text.find(resolver)
    snapshot_pos = text.find(snapshot)
    if resolver_pos < 0:
        errors.append(filename + ": missing preview project resolver")
    if snapshot_pos < 0:
        errors.append(filename + ": missing semantic snapshot boundary")
    if resolver_pos >= 0 and snapshot_pos >= 0 and resolver_pos > snapshot_pos:
        errors.append(filename + ": project freshness must resolve before semantic snapshot/mutation")

p0 = read(SRC / "DirectDrawCommands.cs")
p1 = read(SRC / "DirectDrawP1Commands.cs")
for text, filename in ((p0, "DirectDrawCommands.cs"), (p1, "DirectDrawP1Commands.cs")):
    if "DirectDrawProjectPreviewContext? projectPreview = null" not in text:
        errors.append(filename + ": executor must accept preview context without changing quick-path callers")

opening = read(SRC / "DirectDrawOpeningCommands.cs")
window = read(SRC / "DirectDrawWindowCommands.cs")
reference = read(SRC / "DirectDrawReferenceWallCommands.cs")
for text, filename in ((opening, "DirectDrawOpeningCommands.cs"), (window, "DirectDrawWindowCommands.cs")):
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append(filename + ": shared quick/ADV executor must not bypass preview project resolver")
if "var project = ProjectContextCoordinator.GetOrCreate(document);" in reference:
    errors.append("DirectDrawReferenceWallCommands.cs: reference-wall commit must not bypass preview project resolver")
if "var project = projectPreview.ResolveForMutation(document, operation);" not in window:
    errors.append("DirectDrawWindowCommands.cs: freshness helper must resolve the shared preview project before returning the mutation target")

for token in (
    "same-ProjectId",
    "projectless",
    "backing store",
    "QS3DDRAWWALLADV",
    "QS3DDRAWDOORADV",
    "QS3DDRAWWINDOWADV",
    "QS3DDRAWWALLREFADV",
):
    if token not in read(DOC):
        errors.append("freshness doc missing: " + token)

inbox = read(INBOX)
for token in (
    "Direct Draw",
    "PENDING_LOCAL",
    "DO_NOT_RETRY_REMOTE",
):
    if token not in inbox:
        errors.append("LOCAL-008 handoff missing baseline token: " + token)

if errors:
    print("QS3D Direct Draw project-preview freshness preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: prompt-bearing Direct Draw binds the reviewed project identity, and projectless preview rechecks primary/backup backing-store absence on both sides of GetOrCreate so an appearing sidecar cannot be silently adopted before mutation.")
