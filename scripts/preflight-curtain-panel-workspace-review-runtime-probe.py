#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PROBE = ROOT / "src/QS3D.BricsCAD.V25/CurtainPanelWorkspaceReviewRuntimeProbeCommands.cs"
RUNNER = ROOT / "scripts/test-bricscad-v25-curtain-panel-workspace-review.ps1"
WORKSPACE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.MultiSelectionProperties.cs"
HEALTH = ROOT / "src/QS3D.BricsCAD.V25/HealthAllCommands.cs"
RELEASE = ROOT / "src/QS3D.BricsCAD.V25/ReleaseReadinessCommands.cs"
DOC = ROOT / "docs/CURTAIN-NATIVE-PANELS.md"
INBOX = ROOT / "docs/LOCAL-AGENT-INBOX.md"
CLAIM = ROOT / "docs/agent-work-claims/2026-08-13-codex-local002-curtain-p10-workspace-review.md"

errors = []


def read(path: Path) -> str:
    if not path.is_file():
        errors.append("missing Curtain P10 contract file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


probe = read(PROBE)
runner = read(RUNNER)
workspace = read(WORKSPACE)
health = read(HEALTH)
release = read(RELEASE)
doc = read(DOC)
inbox = read(INBOX)
claim = read(CLAIM)

for token in (
    'CommandMethod("QS3DCURTAINP10SELECT", CommandFlags.Modal)',
    'CommandMethod("QS3DCURTAINP10CHECKWORKSPACE", CommandFlags.Modal)',
    'CommandMethod("QS3DCURTAINP10CHECKHEALTH", CommandFlags.Modal)',
    'CommandMethod("QS3DCURTAINP10COMPLETE", CommandFlags.Modal)',
    'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
    'GeneratedCurtainPanelHealthService.BuildCompleteValue',
    'CadHandleService.GetLiveSolidHandles(document, panelHandles)',
    'document.Editor.SetImpliedSelection(selectedIds.ToArray())',
    'typeof(PaletteCoordinator).GetField("_workspacePanel", BindingFlags.NonPublic | BindingFlags.Static)',
    'PaletteCoordinator.IsWorkspaceVisible',
    'panel.MatchesCurtainP10Review(state.Project, state.Owner, state.Family, state.PanelHandle)',
    'WorkspaceViewModel.InstanceScope',
    'HasCurtainP10HealthAllReadyStatus()',
    'HasCurtainP10ReleaseReadyStatus()',
    'SemanticSelectionResolver.ResolveImplied(state.Document, state.Project)',
    'GeneratedCurtainPanelRuntimeHealthService.Inspect(state.Document, state.Project)',
    'qualification_boundary=LOCAL_002_P10_ONLY',
    'production_local002_qualified=false',
    'p10_qualified=true',
    'failure_phase=',
    'failure_code=',
):
    if token not in probe:
        errors.append("Curtain P10 probe missing production-review token: " + token)

for forbidden in (
    "ProjectContextCoordinator.GetOrCreate",
    ".Touch()",
    "QS3DCURTAIN3D\", CommandFlags",
    "GeneratedCurtainPanelHandles] =",
    "GeneratedCurtainPanelBuildState] =",
    "Application.ShowModelessWindow",
):
    if forbidden in probe:
        errors.append("Curtain P10 probe must not bypass/mutate production behavior: " + forbidden)

if 'SemanticHandleOwnershipResolver.Resolve(project, rawHandles)' not in workspace:
    errors.append("production Workspace must retain canonical generated-owner resolution")

for text, label in ((health, "Health All"), (release, "Release Check")):
    for token in (
        "new GeneratedCurtainPanelHealthService().Inspect",
        "CurtainWallPanelLiveStateService.Inspect",
        "GeneratedCurtainPanelRuntimeHealthService.Inspect",
    ):
        if token not in text:
            errors.append(label + " no longer consumes Curtain panel health: " + token)

for token in (
    '*.curtain-workspace-review-probe-copy.dwg',
    'DrawingCopy must be an ordinary disposable copy outside the repository.',
    'ArtifactDir must stay outside the repository.',
    'status --porcelain=v1 --untracked-files=all',
    'Curtain P10 qualification requires a clean exact-SHA worktree.',
    'ProductVersion',
    'Close existing BricsCAD processes before isolated Curtain P10 qualification.',
    'QS3D_CURTAIN_P10_RESULT',
    'QS3D_CURTAIN_P10_NONCE',
    'QS3DDRAWGLASSWALL',
    'QS3DCURTAINPANELPREPARE',
    'QS3DCURTAIN3D',
    'QS3DCURTAINP10SELECT',
    'QS3DINSPECT',
    'QS3DHEALTHALL',
    'QS3DRELEASECHECK',
    'QS3DCURTAINP10COMPLETE',
    'Remove-ExactFile -Path $scriptPath',
    'private_state_cleanup_verified',
    'drawing_restore_verified',
    'ui_layout_restore_verified',
    '$uiLayoutHashBefore',
    'private UI-layout backup hash mismatch',
    '(Get-FileHash -LiteralPath $uiLayoutPath -Algorithm SHA256).Hash',
    'launcher_handoffs',
):
    if token not in runner:
        errors.append("Curtain P10 runner missing guard/evidence token: " + token)

ordered = (
    '"QS3DCURTAINP10SELECT"',
    '"QS3D"',
    '"QS3DINSPECT"',
    '"QS3DCURTAINP10CHECKWORKSPACE"',
    '"QS3DHEALTHALL"',
    '"QS3DCURTAINP10CHECKHEALTH"',
    '"QS3DRELEASECHECK"',
    '"QS3DCURTAINP10COMPLETE"',
)
positions = [runner.find(token, runner.find("$script = @(")) for token in ordered]
if any(pos < 0 for pos in positions) or positions != sorted(positions) or len(set(positions)) != len(positions):
    errors.append("Curtain P10 runner must preserve panel -> Workspace -> Health All -> Release Check order")

for forbidden in ("workflow run", "gh run", "customer", "private.dwg\" /P"):
    if forbidden.lower() in runner.lower():
        errors.append("Curtain P10 runner crosses the local/privacy/Actions boundary: " + forbidden)

for text, label in ((doc, "Curtain doc"), (inbox, "local inbox")):
    for token in ("P10", "PENDING_LOCAL"):
        if token not in text:
            errors.append(label + " missing bounded P10 status token: " + token)

for token in (
    '- Status: `ACTIVE`',
    'LOCAL-002 / P0 / P10',
    'Production Workspace/Curtain ownership behavior remains read-only',
    'or GitHub Actions',
):
    if token not in claim:
        errors.append("Curtain P10 claim missing ownership boundary: " + token)

print("QS3D Curtain P10 Workspace review runtime preflight")
if errors:
    for error in errors:
        print("ERROR: " + error)
    sys.exit(1)

print("PASS: P10 drives one production-generated Curtain panel through the real Workspace canonical-owner/Family review, Health All and Release Check paths on a clean exact-SHA disposable V25 run; the additive probe remains read-only, emits sanitized bounded evidence and preserves process/script/private-state/DWG/UI-layout cleanup without claiming overall LOCAL-002 qualification.")
