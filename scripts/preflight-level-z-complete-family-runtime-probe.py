#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/LevelZCompleteFamilyRuntimeProbeCommands.cs"
RUNNER = ROOT / "scripts/test-bricscad-v25-level-z-complete-family.ps1"
errors = []

for path in (COMMAND, RUNNER):
    if not path.is_file():
        errors.append("missing LOCAL-003 complete-family runtime file: " + str(path.relative_to(ROOT)))

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    for token in (
        'CommandMethod("QS3DLEVELZCOMPLETEFAMILY", CommandFlags.Modal)',
        'Schema = "LOCAL_003_COMPLETE_FAMILY_RUNTIME_V1"',
        'qualification_boundary=LOCAL_003_COMPLETE_FAMILY_HOSTS_ONLY',
        'production_local003_qualified=false',
        'RuntimeSourceIdentityGuard.RequireExactSourceLink',
        'ElementCategory.ArchitecturalWall', 'ElementCategory.GlassWall', 'ElementCategory.WallPier',
        'ElementCategory.StructuralWall', 'ElementCategory.Beam', 'ElementCategory.Column',
        'ElementCategory.Slab', 'ElementCategory.Foundation', 'ElementCategory.Stair', 'ElementCategory.Railing',
        'OpeningBooleanService.CutLinkedOpenings', 'ElementCategory.Door', 'ElementCategory.WallOpening',
        'FailureKind.TopOnly', 'FailureKind.MissingLevel', 'FailureKind.AmbiguousLevel',
        'FailureKind.NonFiniteOffset', 'FailureKind.InvalidVerticalRange',
        'failure_stage=', 'failure_case=',
        'failureStage = "admission"', 'failureStage = "matrix_configure"',
        'failureStage = "family_build"', 'failureStage = "family_range"',        'failureStage = "family_snapshot"', 'failureStage = "hosted_openings"',
        'failureStage = "fail_closed"', 'failureStage = "level_health"',
        'project.Elements.Count != 0', 'project.Floors.Clear();',
        'project.Floors.Add(new FloorDefinition("L0", "Level 0", 0d));',
    ):
        if token not in text:
            errors.append("complete-family command missing contract token: " + token)
    for forbidden in ('error.Message', 'error.ToString()', 'error.StackTrace', 'GeneratedSolidHandle=" +'):
        if forbidden in text:
            errors.append("complete-family failure taxonomy leaks forbidden detail token: " + forbidden)

    build_loop = text.find("foreach (var item in matrix)")
    door_setup = text.find("var door = AddOpening(project, ElementCategory.Door")
    wall_opening_setup = text.find("var wallOpening = AddOpening(project, ElementCategory.WallOpening")
    verify_call = text.find("VerifyStraightHostedOpenings(document, project, door, wallOpening)")
    if min(build_loop, door_setup, wall_opening_setup) < 0:
        errors.append("complete-family hosted-opening setup/build ordering tokens are missing")
    elif not (door_setup < build_loop and wall_opening_setup < build_loop):
        errors.append("complete-family hosted openings must be configured before host build so generated geometry is fresh")
    if verify_call < 0:
        errors.append("complete-family hosted-opening verification must consume preconfigured Door and WallOpening")

    verify_start = text.find("private static int VerifyStraightHostedOpenings")
    add_opening_start = text.find("private static ProjectElement AddOpening", verify_start)
    if verify_start < 0 or add_opening_start < 0:
        errors.append("complete-family hosted-opening verification method boundary is missing")
    else:
        verify_body = text[verify_start:add_opening_start]
        for forbidden in ("CreateLineSource(", "AddOpening("):
            if forbidden in verify_body:
                errors.append("complete-family verify phase must not mutate hosted-opening setup: " + forbidden)

if RUNNER.is_file():
    text = RUNNER.read_text(encoding="utf-8-sig")
    for token in (
        'ValidateSet("Millimeter", "Meter")',
        'ExpectedSourceSha', 'git -C $repoRoot rev-parse HEAD', 'status --porcelain=v1',
        'Assert-Qs3dExactSourceIdentity', 'Get-Qs3dExactBricsCadProcesses',
        '.level-z-complete-family-probe-copy.dwg', 'QS3DLEVELZCOMPLETEFAMILY',
        'QS3D_LEVEL_Z_COMPLETE_FAMILY_RESULT', 'QS3D_LEVEL_Z_COMPLETE_FAMILY_NONCE',
        'QS3D_LEVEL_Z_COMPLETE_FAMILY_SOURCE_SHA', 'LOCAL_003_COMPLETE_FAMILY_RUNTIME_V1',
        'host_family_count', 'family_case_count', 'hosted_opening_count',
        'drawing_restore_verified', 'process_cleanup_verified', 'private_state_cleanup_verified',
        '$unsavedProjectChangesDialogsDiscarded = 0',
        'unsaved_project_changes_dialogs_discarded = $unsavedProjectChangesDialogsDiscarded',
    ):
        if token not in text:
            errors.append("complete-family runner missing contract token: " + token)

    graceful_loop_start = text.find('$gracefulDeadline = (Get-Date).AddSeconds($GracefulExitTimeoutSeconds)')
    graceful_loop_end = text.find('if (-not $gracefulExit)', graceful_loop_start)
    if graceful_loop_start < 0 or graceful_loop_end < 0:
        errors.append("complete-family post-marker graceful-exit loop is missing")
    else:
        graceful_loop = text[graceful_loop_start:graceful_loop_end]
        discard_call = graceful_loop.find('Close-Qs3dUnsavedProjectChangesDialog -Process $process')
        wait_call = graceful_loop.find('$process.WaitForExit(250)')
        if discard_call < 0:
            errors.append("complete-family graceful-exit loop must discard the runner-owned unsaved-project dialog")
        elif wait_call < 0 or discard_call > wait_call:
            errors.append("complete-family unsaved-project dialog handling must run before WaitForExit")

if errors:
    for error in errors:
        print("FAIL: " + error)
    sys.exit(1)

print("PASS: LOCAL-003 complete-family runtime contract covers all 10 Level-aware host families, straight hosted openings, refusal rows, exact-SHA identity, dual native units, bounded failure taxonomy and disposable cleanup boundaries.")
