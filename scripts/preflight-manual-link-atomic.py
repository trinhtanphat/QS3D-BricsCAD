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
    "ProjectStateSnapshot.Capture(project)",
    "new HostLinkService().LinkOpening(project, opening.Id, wall.Id)",
    "RegenerateProject(project)",
    "rollback.Restore(project)",
    "new AggregateException(operationError, restoreError)",
    "PaletteCoordinator.RefreshProject()",
]
for token in required:
    if token not in block:
        fail(f"QS3DLINKHOST missing atomicity token: {token}")

capture = block.index("ProjectStateSnapshot.Capture(project)")
link = block.index("new HostLinkService().LinkOpening(project, opening.Id, wall.Id)")
regen = block.index("RegenerateProject(project)")
restore = block.index("rollback.Restore(project)")
refresh = block.index("PaletteCoordinator.RefreshProject()")
if not (capture < link < regen < restore < refresh):
    fail("QS3DLINKHOST atomicity ordering regressed")

if "catch (System.Exception operationError)" not in block:
    fail("QS3DLINKHOST must catch operation failure for rollback")
if "catch (System.Exception restoreError)" not in block:
    fail("QS3DLINKHOST must surface rollback failure")

print("[PASS] manual QS3DLINKHOST mutation/regeneration is guarded by ProjectStateSnapshot rollback")
