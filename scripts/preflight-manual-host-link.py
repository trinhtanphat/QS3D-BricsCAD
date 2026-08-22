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
        "Cad.EntitySnapshotReader.ReadCurrentSelection(doc)",
        "if (selectedHandles.Count == 0)",
        'ExistingProjectMutationContext.Require(doc, "Link opening host")',
        "SemanticReferenceHandles.MatchesSelection",
        "openings.Count != 1 || hosts.Count != 1",
        'opening.Properties.TryGetValue("HostWallId", out var existingHostId)',
        "var regenerationTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase)",
        "if (previousHostId.Length > 0 && project.FindElement(previousHostId) != null)",
        "regenerationTargets.Add(previousHostId)",
        "ProjectStateSnapshot.Capture(project)",
        "new HostLinkService().LinkOpening(project, opening.Id, wall.Id)",
        ".RegenerateDirtySubset(project, regenerationTargets)",
        "var currentOpening = project.FindElement(opening.Id)",
        'currentOpening.Properties.TryGetValue("HostWallId"',
        "string.Equals(persistedHostId, wall.Id, StringComparison.OrdinalIgnoreCase)",
        "rollback.Restore(project)",
        "new AggregateException(operationError, restoreError)",
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
        "ProjectContextCoordinator.GetOrCreate(doc)",
        "RegenerateProject(project)",
    )
    for token in forbidden:
        if token in block:
            errors.append("QS3DLINKHOST contains unsafe/manual-link shortcut: " + token)

    selection = block.find("Cad.EntitySnapshotReader.ReadCurrentSelection(doc)")
    empty_guard = block.find("if (selectedHandles.Count == 0)")
    bind = block.find('ExistingProjectMutationContext.Require(doc, "Link opening host")')
    previous = block.find('opening.Properties.TryGetValue("HostWallId", out var existingHostId)')
    targets = block.find("var regenerationTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase)")
    capture = block.find("ProjectStateSnapshot.Capture(project)")
    link = block.find("new HostLinkService().LinkOpening")
    regen = block.find(".RegenerateDirtySubset(project, regenerationTargets)")
    resolve = block.find("var currentOpening = project.FindElement(opening.Id)")
    verify = block.find('currentOpening.Properties.TryGetValue("HostWallId"')
    restore = block.find("rollback.Restore(project)")
    refresh = block.find("PaletteCoordinator.RefreshProject()")
    if min(selection, empty_guard, bind, previous, targets, capture, link, regen, resolve, verify, restore, refresh) >= 0:
        if not (selection < empty_guard < bind < previous < targets < capture < link < regen < resolve < verify < restore < refresh):
            errors.append("QS3DLINKHOST must read/guard selection before binding existing project state, snapshot old host scope before mutation, scoped-regenerate only opening/new/old host, canonical re-resolve, verify, rollback path and post-commit UI refresh")

if errors:
    print("QS3D manual host-link preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DLINKHOST guards empty selection before existing-project bind, scopes regeneration to the opening plus new/previous live hosts, verifies canonical HostWallId, rolls back semantic failure, and leaves unrelated dirty project elements untouched.")
