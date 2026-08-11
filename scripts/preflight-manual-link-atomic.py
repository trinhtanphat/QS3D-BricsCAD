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
    "ExistingProjectMutationContext.TryGet(doc, out var project)",
    "var openingId = openings[0].Id;",
    "var wallId = hosts[0].Id;",
    "ProjectStateSnapshot.Capture(project)",
    "new HostLinkService().LinkOpening(project, openingId, wallId)",
    "RegenerateProject(project)",
    "project.FindElement(openingId)",
    'canonicalOpening.Properties.TryGetValue("HostWallId", out var persistedHostId)',
    "rollback.Restore(project)",
    "new AggregateException(operationError, restoreError)",
    "PaletteCoordinator.RefreshProject()",
]
for token in required:
    if token not in block:
        fail(f"QS3DLINKHOST missing lifecycle/atomicity token: {token}")

if "ProjectContextCoordinator.GetOrCreate(doc)" in block:
    fail("QS3DLINKHOST must not create/cache a replacement project")

selection = block.index("Cad.EntitySnapshotReader.ReadCurrentSelection(doc)")
bind = block.index("ExistingProjectMutationContext.TryGet(doc, out var project)")
capture = block.index("ProjectStateSnapshot.Capture(project)")
link = block.index("new HostLinkService().LinkOpening(project, openingId, wallId)")
regen = block.index("RegenerateProject(project)")
resolve = block.index("project.FindElement(openingId)")
restore = block.index("rollback.Restore(project)")
refresh = block.index("PaletteCoordinator.RefreshProject()")
if not (selection < bind < capture < link < regen < resolve < restore < refresh):
    fail("QS3DLINKHOST lifecycle/atomicity ordering regressed")

if "catch (System.Exception operationError)" not in block:
    fail("QS3DLINKHOST must catch operation failure for rollback")
if "catch (System.Exception restoreError)" not in block:
    fail("QS3DLINKHOST must surface rollback failure")
if "opening.Properties.TryGetValue" in block:
    fail("QS3DLINKHOST must validate HostWallId on the canonical post-regeneration opening, not a stale pre-regeneration reference")

print("[PASS] manual QS3DLINKHOST requires an existing canonical project, re-resolves the opening after regeneration, and rolls back semantic mutation failures")
