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

    opening_start = text.find("private static int VerifyStraightHostedOpenings")
    opening_end = text.find("private static ProjectElement AddOpening", opening_start)
    if opening_start < 0 or opening_end < 0:
        errors.append("complete-family opening verification boundary is missing")
    else:
        opening = text[opening_start:opening_end]
        ordered = (
            'var door = AddOpening(',
            'var wallOpening = AddOpening(',
            'Require(host.IsGeneratedSolidStale(), "opening Level mutation must stale host before rebuild");',
            'Require(CadHandleService.Select(document, host.SourceHandles) == 1, "opening host source selection");',
            'Require(WallSolidBuilder.BuildSelectedLineWalls(document, project, ElementCategory.ArchitecturalWall, false) == 1, "opening host rebuild");',
            'Require(!host.IsGeneratedSolidStale(), "opening host rebuild must clear generated solid stale state");',
            'OpeningBooleanService.CutLinkedOpenings(document, project, new[] { door.Id })',
            'OpeningBooleanService.CutLinkedOpenings(document, project, new[] { wallOpening.Id })',
        )
        cursor = -1
        for token in ordered:
            position = opening.find(token, cursor + 1)
            if position < 0:
                errors.append("complete-family opening stale-host rebuild sequencing missing token: " + token)
                break
            cursor = position

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
    ):
        if token not in text:
            errors.append("complete-family runner missing contract token: " + token)

if errors:
    for error in errors:
        print("FAIL: " + error)
    sys.exit(1)

print("PASS: LOCAL-003 complete-family runtime contract covers all 10 Level-aware host families, straight hosted openings, refusal rows, exact-SHA identity, dual native units, bounded failure taxonomy and disposable cleanup boundaries.")
