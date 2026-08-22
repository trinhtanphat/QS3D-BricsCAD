#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src" / "QS3D.BricsCAD.V25" / "Commands.cs"
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"

errors = []
source = COMMANDS.read_text(encoding="utf-8")
start = source.find('[CommandMethod("QS3DLINKHOST"')
end = source.find('[CommandMethod("QS3DFINISH"', start)
if start < 0 or end < 0:
    errors.append("cannot isolate QS3DLINKHOST command block")
else:
    body = source[start:end]
    selection = body.find("Cad.EntitySnapshotReader.ReadCurrentSelection(doc)")
    empty_guard = body.find("if (selectedHandles.Count == 0)")
    bind = body.find('ExistingProjectMutationContext.Require(doc, "Link opening host")')
    resolve = body.find("var selected = project.Elements")
    snapshot = body.find("ProjectStateSnapshot.Capture(project)")
    mutate = body.find("new HostLinkService().LinkOpening(project, opening.Id, wall.Id)")
    regenerate = body.find("RegenerateDirtySubset(project, regenerationTargets)")
    restore = body.find("rollback.Restore(project)")

    if "ProjectContextCoordinator.GetOrCreate" in body:
        errors.append("QS3DLINKHOST must not create/cache a project")
    if min(selection, empty_guard, bind, resolve) < 0 or not selection < empty_guard < bind < resolve:
        errors.append("QS3DLINKHOST must read/validate selection before canonical project binding and semantic resolution")
    if min(snapshot, mutate, regenerate, restore) < 0 or not snapshot < mutate < regenerate:
        errors.append("QS3DLINKHOST must retain snapshot -> host mutation -> regeneration ordering")
    if "RegenerateProject(project)" in body:
        errors.append("QS3DLINKHOST must not regenerate unrelated dirty semantic elements")
    if restore < 0:
        errors.append("QS3DLINKHOST must retain semantic rollback on failure")

inbox = INBOX.read_text(encoding="utf-8") if INBOX.is_file() else ""
for token in (
    "LOCAL-001 — exact V25 build/load baseline",
    "QS3DLINKHOST",
    "empty/cancelled selection",
):
    if token not in inbox:
        errors.append("LOCAL-001 missing manual Host Link lifecycle proof token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DLINKHOST validates non-empty CAD selection before canonical existing-project binding; mutation/regeneration rollback ordering remains guarded and LOCAL-001 owns exact-V25 proof.")
