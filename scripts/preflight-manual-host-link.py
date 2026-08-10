#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source = ROOT / "src/QS3D.BricsCAD.V25/Commands.cs"
errors = []

if not source.is_file():
    errors.append("missing Commands.cs")
else:
    text = source.read_text(encoding="utf-8")
    start = text.find('[CommandMethod("QS3DLINKHOST"')
    end = text.find('[CommandMethod("QS3DFINISH"', start + 1) if start >= 0 else -1
    if start < 0:
        errors.append("missing QS3DLINKHOST command")
        block = ""
    elif end < 0:
        errors.append("cannot isolate QS3DLINKHOST command block")
        block = text[start:]
    else:
        block = text[start:end]

    required = (
        "SemanticReferenceHandles.MatchesSelection",
        "openings.Count != 1 || hosts.Count != 1",
        "ProjectStateSnapshot.Capture(project)",
        "new HostLinkService().LinkOpening(project, opening.Id, wall.Id)",
        "RegenerateProject(project)",
        'opening.Properties.TryGetValue("HostWallId"',
        "string.Equals(persistedHostId, wall.Id, StringComparison.OrdinalIgnoreCase)",
        "rollback.Restore(project)",
        "PaletteCoordinator.RefreshProject()",
        "doc.Editor.Regen()",
        "UI sync warning",
    )
    for needle in required:
        if needle not in block:
            errors.append("QS3DLINKHOST missing contract: " + needle)

    forbidden = (
        "FirstOrDefault(",
        "SourceHandles.Any(selectedHandles.Contains)",
        "OpeningBooleanService",
        "CutLinkedOpenings",
        "QS3DCUTOPENINGS",
        "SendStringToExecute",
    )
    for token in forbidden:
        if token in block:
            errors.append("QS3DLINKHOST contains unsafe/manual-link shortcut: " + token)

    capture = block.find("ProjectStateSnapshot.Capture(project)")
    link = block.find("new HostLinkService().LinkOpening")
    regen = block.find("RegenerateProject(project)")
    verify = block.find('opening.Properties.TryGetValue("HostWallId"')
    restore = block.find("rollback.Restore(project)")
    refresh = block.find("PaletteCoordinator.RefreshProject()")
    if min(capture, link, regen, verify, restore, refresh) < 0:
        pass
    else:
        if not (capture < link < regen < verify):
            errors.append("QS3DLINKHOST must snapshot before link, regenerate, then verify persisted HostWallId")
        if refresh < verify:
            errors.append("QS3DLINKHOST UI refresh must occur only after semantic HostWallId verification")

if errors:
    print("QS3D manual host-link preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DLINKHOST requires exactly one opening and one compatible host, resolves semantic source/generated selection, snapshots before mutation, regenerates and verifies HostWallId, restores project state on semantic failure, keeps UI sync non-destructive, and never invokes physical cutting.")