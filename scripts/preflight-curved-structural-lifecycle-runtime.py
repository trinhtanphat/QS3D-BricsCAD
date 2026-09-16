from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
BASE_SOURCE = ROOT / "src/QS3D.BricsCAD.V25/CurvedStructuralRuntimeProbeCommands.cs"
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/CurvedStructuralLifecycleRuntimeProbeCommands.cs"
RUNNER = ROOT / "scripts/test-bricscad-v25-curved-structural-lifecycle.ps1"
errors = []
if not BASE_SOURCE.is_file() or "public sealed partial class CurvedStructuralRuntimeProbeCommands" not in BASE_SOURCE.read_text(encoding="utf-8"):
    errors.append("curved capture/build probe must expose a partial class boundary")

def require_tokens(path: Path, tokens, label: str):
    if not path.is_file():
        errors.append(f"missing {label}: {path.relative_to(ROOT)}")
        return ""
    text = path.read_text(encoding="utf-8")
    for token in tokens:
        if token not in text:
            errors.append(f"{label} missing contract token: {token}")
    return text

source = require_tokens(SOURCE, [
    'CommandMethod("QS3DCURVEDLIFEPREPARE"',
    'CommandMethod("QS3DCURVEDLIFESELECTBEAMS"',
    'CommandMethod("QS3DCURVEDLIFESELECTSLAB"',
    'CommandMethod("QS3DCURVEDLIFECAPTUREBASELINE"',
    'CommandMethod("QS3DCURVEDLIFECHECKUNDO"',
    'CommandMethod("QS3DCURVEDLIFECAPTUREREDO"',
], "curved lifecycle probe")
require_tokens(SOURCE, [
    'CommandMethod("QS3DCURVEDLIFECHECKREDO"',
    'CommandMethod("QS3DCURVEDLIFESESSION1"',
    'CommandMethod("QS3DCURVEDLIFEREOPEN"',
    'CommandMethod("QS3DCURVEDLIFEAFTERREBUILD"',
    'CommandMethod("QS3DCURVEDLIFEPREPAREB"',
    'CommandMethod("QS3DCURVEDLIFECAPTUREB"',
    'CommandMethod("QS3DCURVEDLIFEACTIVATEA"',
    'CommandMethod("QS3DCURVEDLIFECHECKA"',
    'CommandMethod("QS3DCURVEDLIFEACTIVATEB"',
    'CommandMethod("QS3DCURVEDLIFECOMPLETE"',
    'ProjectPersistenceCheckpoint.Capture(',
    '.Matches(context.Project)',
    'savedCheckpoint.Matches(context.Project)',
    'reopenedCheckpoint.Matches(context.Project)',
    'LifeStateFingerprint(',
    'QS3D_CURVED_LIFECYCLE_EXPECTED_REOPEN_FINGERPRINT',
    'reopen_coherent=" + LifeBool(a.ReopenCoherent)',
    'ExistingProjectMutationContext.Require(',
    'ProjectContextCoordinator.Save(',
    'beam_arc',
    'beam_circle',
    'beam_polyline_curved',
    'slab_circle',
    'old_generated_removed=',
    'new_generated_disjoint=',
    'multi_dwg_isolated=',
], "curved lifecycle probe")

runner = require_tokens(RUNNER, [
    '[Parameter(Mandatory = $true)][string]$ExpectedSourceSha',
    '[Parameter(Mandatory = $true)][switch]$ConfirmDisposableCopies',
    'samples\\generated\\QS3D-Sample.dwg',
], "curved lifecycle runner")
require_tokens(RUNNER, [
    'status --porcelain=v1 --untracked-files=all',
    'Assert-Qs3dExactSourceIdentity',
    'Get-Qs3dExactBricsCadProcesses',
    'QS3DCURVEDLIFEPREPARE',
    '_.UNDO', '"_Mark"', '"_Back"', '"_Begin"', '"_End"',
    'QS3DBUILD3D', '_.U', '_.REDO',
    'QS3DSAVE', '_.QSAVE',
    'QS3DCURVEDLIFEREOPEN',
    'QS3D_CURVED_LIFECYCLE_EXPECTED_REOPEN_FINGERPRINT',
    'saved_checkpoint_matches',
    'reopen_fingerprint',
    '^[0-9A-F]{64}$',
    'QS3DCURVEDLIFEPREPAREB',
    'QS3DCURVEDLIFEACTIVATEA',
    'QS3DCURVEDLIFEACTIVATEB',
    'drawing_restore_verified',
    'sidecar_cleanup_verified',
    'process_cleanup_verified',
    'script_cleanup_verified',
    'curved-structural-lifecycle-a-probe-copy.dwg',
    'curved-structural-lifecycle-b-probe-copy.dwg',
], "curved lifecycle runner")

for forbidden in (
    'project_id=', 'source_handle=', 'generated_handle=',
    'drawing_path=', 'sidecar_path=', 'private_path='
):
    if forbidden in source:
        errors.append(f"curved lifecycle marker must not expose: {forbidden}")

if 'ReferenceEquals(state.Project, context.Project)' in source:
    errors.append('curved lifecycle session affinity must not require ProjectState object identity across native Undo/Redo')
for token in (
    'public string ProjectId { get; }',
    'string.Equals(state.ProjectId, context.Project.ProjectId, StringComparison.Ordinal)',
):
    if token not in source:
        errors.append(f'curved lifecycle canonical ProjectId affinity missing contract token: {token}')
if runner and runner.count('Start-Process -FilePath $bricscadExe') < 2:
    errors.append("curved lifecycle runner must use two isolated BricsCAD processes")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)
print("PASS: curved structural lifecycle source/runner contract is pinned")
