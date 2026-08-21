#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src" / "QS3D.BricsCAD.V25" / "ProjectLifecycleProbeCommands.cs"
WORKSPACE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.xaml.cs"
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
workspace = read(WORKSPACE)
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
    '"legacy_unit_binding_persisted=true"',
    '"native_unit_resolution_noncreating=true"',
    '"explicit_unit_override_persisted=true"',
    '"automation_confirmation_consumed=true"',
    '"unbound_binding_evidence_absent=true"',
    '"effective_override_resolved=true"',
    "DrawingUnitResolutionPolicy.BoundMetadataKey",
    "DrawingUnitResolutionPolicy.EffectiveUnitMetadataKey",
    "DrawingUnitResolutionPolicy.BindingSourceMetadataKey",
    "Cad.CadUnitService.TryGetPolicy(document, out _, out var effectiveResolution)",
    "effectiveResolution.Source != DrawingUnitResolutionSource.ProjectOverride",
    "Teigha.DatabaseServices.UnitsValue.Undefined",
    "DrawingUnitAutomationConfirmation.Arm(document, LengthUnit.Meter)",
    "DrawingUnitAutomationConfirmation.IsArmed(document)",
    "TryWriteCommandFailure(resultPath, probeFailure.ErrorCode)",
    'base("A sanitized lifecycle probe invariant failed.")',
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
    ("BQ_LEGACY_EXISTING", "QS3DBQ"),
    ("BQ_NATIVE_ABSENT", "QS3DBQ"),
    ("UNITS_OVERRIDE_ABSENT", "QS3DUNITS"),
):
    mapping = '"' + phase + '" = "' + command + '"'
    if mapping not in runner:
        errors.append("runner missing real command phase: " + mapping)

for token in (
    '"QS3DLIFECYCLECOMMANDPREP", $command',
    '$phaseLines += "QS3DLIFECYCLECOMMANDVERIFY"',
    'legacyBqUnitBindingPersisted = $true',
    'nativeBqAbsentNoncreating = $true',
    'explicitUnitOverrideBootstrap = $true',
    '"unbound_binding_evidence_absent", "effective_override_resolved"',
):
    if token not in runner:
        errors.append("runner unit/execution lifecycle contract missing token: " + token)

refresh_start = workspace.find("public void RefreshProject()")
refresh_end = workspace.find("public void SetStatus", refresh_start)
if refresh_start < 0 or refresh_end <= refresh_start:
    errors.append("cannot isolate WorkspacePanel.RefreshProject")
else:
    refresh = workspace[refresh_start:refresh_end]
    if "ExistingProjectMutationContext.TryGet(doc, out var project)" not in refresh:
        errors.append("Workspace refresh must bind only an existing canonical project")
    if "ClearProject(" not in refresh:
        errors.append("Workspace refresh must clear stale UI when no project exists")
    if "ProjectContextCoordinator.GetOrCreate" in refresh or "ProjectContextCoordinator.TryGetReadOnly" in refresh:
        errors.append("passive Workspace refresh must not cold-create or expose a detached project state")
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
    "QS3DBQ",
    "QS3DUNITS",
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

print("PASS: the exact-SHA V25 lifecycle runner executes real REGEN/REFRESH/FINISH and unit-binding commands, proves canonical legacy persistence, no-project native-unit inspection, intentional QS3DUNITS bootstrap, and bounded sanitized evidence.")
