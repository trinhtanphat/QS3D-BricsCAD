#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src" / "QS3D.BricsCAD.V25" / "Commands.cs"


def fail(message: str) -> None:
    print(f"[FAIL] {message}")
    sys.exit(1)


text = COMMANDS.read_text(encoding="utf-8")
start = text.find('[CommandMethod("QS3DLINKHOST", CommandFlags.UsePickSet)]')
end = text.find('[CommandMethod("QS3DFINISH", CommandFlags.UsePickSet)]', start)
if start < 0 or end < 0:
    fail("cannot isolate QS3DLINKHOST command")

block = text[start:end]
required = [
    "Cad.EntitySnapshotReader.ReadCurrentSelection(doc)",
    "if (selectedHandles.Count == 0)",
    'ExistingProjectMutationContext.Require(doc, "Link opening host")',
    'opening.Properties.TryGetValue("HostWallId", out var existingHostId)',
    "var regenerationTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase)",
    "if (previousHostId.Length > 0 && project.FindElement(previousHostId) != null)",
    "regenerationTargets.Add(previousHostId)",
    "ProjectStateSnapshot.Capture(project)",
    "new HostLinkService().LinkOpening(project, opening.Id, wall.Id)",
    ".RegenerateDirtySubset(project, regenerationTargets)",
    "var currentOpening = project.FindElement(opening.Id)",
    'currentOpening.Properties.TryGetValue("HostWallId", out var persistedHostId)',
    "string.Equals(persistedHostId, wall.Id, StringComparison.OrdinalIgnoreCase)",
    "rollback.Restore(project)",
    "new AggregateException(operationError, restoreError)",
    "PaletteCoordinator.RefreshProject()",
]
for token in required:
    if token not in block:
        fail(f"QS3DLINKHOST missing lifecycle/atomicity token: {token}")

for forbidden in (
    "ProjectContextCoordinator.GetOrCreate(doc)",
    "RegenerateProject(project)",
):
    if forbidden in block:
        fail("QS3DLINKHOST contains stale/creating/full-project token: " + forbidden)

selection = block.index("Cad.EntitySnapshotReader.ReadCurrentSelection(doc)")
empty_guard = block.index("if (selectedHandles.Count == 0)")
bind = block.index('ExistingProjectMutationContext.Require(doc, "Link opening host")')
previous = block.index('opening.Properties.TryGetValue("HostWallId", out var existingHostId)')
targets = block.index("var regenerationTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase)")
capture = block.index("ProjectStateSnapshot.Capture(project)")
link = block.index("new HostLinkService().LinkOpening(project, opening.Id, wall.Id)")
regen = block.index(".RegenerateDirtySubset(project, regenerationTargets)")
resolve = block.index("var currentOpening = project.FindElement(opening.Id)")
verify = block.index('currentOpening.Properties.TryGetValue("HostWallId", out var persistedHostId)')
restore = block.index("rollback.Restore(project)")
refresh = block.index("PaletteCoordinator.RefreshProject()")
if not (selection < empty_guard < bind < previous < targets < capture < link < regen < resolve < verify < restore < refresh):
    fail("QS3DLINKHOST lifecycle/scoped-regeneration/atomicity ordering regressed")

if "catch (System.Exception operationError)" not in block:
    fail("QS3DLINKHOST must catch operation failure for rollback")
if "catch (System.Exception restoreError)" not in block:
    fail("QS3DLINKHOST must surface rollback failure")

print("[PASS] manual QS3DLINKHOST reads selection before binding existing project state, captures previous host scope, regenerates only opening/new/old host, verifies canonical HostWallId, and rolls back semantic mutation failures")
