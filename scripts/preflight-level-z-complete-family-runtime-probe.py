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
    ):
        if token not in text:
            errors.append("complete-family command missing contract token: " + token)

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

print("PASS: LOCAL-003 complete-family runtime contract covers all 10 Level-aware host families, straight hosted openings, refusal rows, exact-SHA identity, dual native units and disposable cleanup boundaries.")
