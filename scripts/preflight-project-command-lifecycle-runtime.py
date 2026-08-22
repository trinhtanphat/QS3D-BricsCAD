#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src" / "QS3D.BricsCAD.V25" / "ProjectLifecycleProbeCommands.cs"
RUNNER = ROOT / "scripts" / "test-bricscad-v25-project-lifecycle.ps1"
DOCS = ROOT / "docs" / "COMMANDS.md"
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(COMMANDS)
runner = read(RUNNER)
docs = read(DOCS)
inbox = read(INBOX)

for token in (
    '[CommandMethod("QS3DLIFECYCLECOMMANDPREP", CommandFlags.Modal)]',
    '[CommandMethod("QS3DLIFECYCLECOMMANDVERIFY", CommandFlags.Modal)]',
    "ProjectContextCoordinator.Forget(document)",
    "ExistingProjectMutationContext.TryGet(document, out var project)",
    "ProjectContextCoordinator.Save(document)",
    "Cad.CadHandleService.SelectIfAny(document",
    "RoomFinishSynchronizationService.Categories",
    "RoomFinishIdentityService.FindExisting(project, room, category)",
    '"absent_sidecar_noncreating=true"',
    '"no_cached_project=true"',
    '"canonical_project_identity_matched=true"',
):
    if token not in source:
        errors.append("command lifecycle probe missing token: " + token)

for phase, command in (
    ("REGEN_EXISTING", "QS3DREGEN"),
    ("REFRESH_EXISTING", "QS3DREFRESH"),
    ("FINISH_EXISTING", "QS3DFINISH"),
    ("REGEN_ABSENT", "QS3DREGEN"),
    ("REFRESH_ABSENT", "QS3DREFRESH"),
    ("FINISH_ABSENT", "QS3DFINISH"),
):
    mapping = '"' + phase + '" = "' + command + '"'
    if mapping not in runner:
        errors.append("runner missing real command phase: " + mapping)

prep = runner.find('"QS3DLIFECYCLECOMMANDPREP", $command, "QS3DLIFECYCLECOMMANDVERIFY"')
if prep < 0:
    errors.append("runner must execute prep -> real user command -> verify in one BricsCAD process")
for token in (
    "git -C $repoRoot status --porcelain",
    "$exactSha = (& git -C $repoRoot rev-parse HEAD).Trim()",
    '"QS3D_LIFECYCLE_PHASE"',
    "Restore-EnvironmentValue -Name $name",
    "Stop-Qs3dLaunchedProcess -Process $process",
    "fixtureSha256Before",
    "fixtureSha256After",
    "commandLifecyclePhaseCount = $commandPhases.Count",
):
    if token not in runner:
        errors.append("runner exact-SHA/scope/cleanup contract missing token: " + token)

for token in (
    "QS3DLIFECYCLECOMMANDPREP",
    "QS3DLIFECYCLECOMMANDVERIFY",
    "QS3DREGEN",
    "QS3DREFRESH",
    "QS3DFINISH",
    "absent-sidecar",
):
    if token not in docs:
        errors.append("COMMANDS documentation missing lifecycle token: " + token)

for token in (
    "LOCAL-001 — exact V25 build/load baseline",
    "QS3DREGEN",
    "QS3DREFRESH",
    "QS3DFINISH",
    "no replacement project",
):
    if token not in inbox:
        errors.append("LOCAL-001 missing runtime lifecycle ownership token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: the exact-SHA V25 lifecycle runner executes real REGEN/REFRESH/FINISH commands after cold-cache preparation, proves canonical existing-project mutation and absent-sidecar non-creation, and emits bounded sanitized evidence.")
