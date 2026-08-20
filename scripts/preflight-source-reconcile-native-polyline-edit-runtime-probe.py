#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PROBE = ROOT / "src" / "QS3D.BricsCAD.V25" / "SourceReconcileNativePolylineEditRuntimeProbeCommands.cs"
RUNNER = ROOT / "scripts" / "test-bricscad-v25-source-reconcile-native-polyline-edit.ps1"
CLAIM = ROOT / "docs" / "agent-work-claims" / "2026-08-20-codex-issue3287-native-slab-polyline-stretch.md"

errors = []
for path in (PROBE, RUNNER, CLAIM):
    if not path.is_file():
        errors.append(f"missing required LOCAL-004 P02 file: {path.relative_to(ROOT)}")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

probe = PROBE.read_text(encoding="utf-8")
runner = RUNNER.read_text(encoding="utf-8")
claim = CLAIM.read_text(encoding="utf-8")

commands = (
    "QS3DSRPOLYPREPARE",
    "QS3DSRPOLYSTRETCHCHECK",
    "QS3DSRPOLYSELECT",
    "QS3DSRPOLYSYNCCHECK",
    "QS3DSRPOLYFINAL",
    "QS3DSRPOLYREOPEN",
)
for command in commands:
    if probe.count(f'CommandMethod("{command}"') != 1:
        errors.append(f"probe must register {command} exactly once")

required_probe_tokens = (
    "SourceReconcileNativePolylineEditRuntimeProbeCommands",
    "ElementCategory.Slab",
    "polyline.Closed",
    "polyline.NumberOfVertices",
    "polyline.GetPoint3dAt(index)",
    "polyline.GetBulgeAt(index)",
    "polyline.Area / (unitsPerMeter * unitsPerMeter)",
    "Meters(document, polyline.Length)",
    "new Point2d(5d, 3d)",
    "12d + Math.Sqrt(10d)",
    "RequireSemanticMetrics(owner, ExpectedStage.Initial)",
    "RequireQuantities(owner, ExpectedStage.Initial)",
    "GENERATED_MUTATED_BY_NATIVE_STRETCH",
    "RequireNoGenerated(context.Document, owner, state.InitialGenerated.Handle)",
    "RequireSemanticMetrics(owner, ExpectedStage.Stretched)",
    "RequireQuantities(owner, ExpectedStage.Stretched)",
    "GeneratedGeometryService.HasMatchingOwnership",
    "solid.MassProperties.Volume",
    "RequireScopedHealth(context.Document, context.Project, owner, generated.Handle)",
    "qualification_boundary=\" + Boundary",
    "production_local004_p02_qualified=true",
    "final_geometry_class=QUADRILATERAL_13_5_M2",
    'failure_code=" + OneLine(code)',
)
for token in required_probe_tokens:
    if token not in probe:
        errors.append(f"probe is missing required native POLYLINE contract token: {token}")

for forbidden in (
    "OpenMode.ForWrite",
    "StartTransaction()",
    "AppendEntity(",
    ".Erase(",
    ".SetPointAt(",
    "ProjectContextCoordinator.GetOrCreate",
    "ProjectStateSnapshot.Capture",
    "SendStringToExecute",
    ".Editor.Command(",
):
    if forbidden in probe:
        errors.append(f"automation-only probe must not mutate or synthesize native edits directly: {forbidden}")

selection_clear = probe.find("context.Document.Editor.SetImpliedSelection(Array.Empty<ObjectId>());")
state_publish = probe.find("_state = new SequenceState(")
if state_publish < 0 or selection_clear < state_publish:
    errors.append("probe must clear Direct Draw's retained generated-solid PICKFIRST after capturing initial state")
if "CommandFlags.Modal | CommandFlags.UsePickSet" not in probe:
    errors.append("source reselection command must preserve PICKFIRST for each production command")

script_start = runner.find("$scriptOne = @(")
script_end = runner.find("    )", script_start)
if script_start < 0 or script_end < 0:
    errors.append("runner session-one script block is missing")
else:
    script = runner[script_start:script_end]
    ordered = (
        '"QS3DDRAWSLAB", "0,0", "4000,0", "4000,3000", "0,3000", ""',
        '"QS3DSRPOLYPREPARE"',
        '"_.STRETCH", "_C", "3900,2900", "4100,3100", "", "0,0", "1000,0"',
        '"QS3DSRPOLYSTRETCHCHECK"',
        '"QS3DSRPOLYSELECT", "QS3DSYNCSOURCE", "QS3DSRPOLYSYNCCHECK"',
        '"QS3DSRPOLYSELECT", "QS3DBUILD3D", "QS3DSRPOLYFINAL"',
        '"QS3DSAVE", "_.QSAVE", "_.QUIT", "_Y"',
    )
    cursor = -1
    for token in ordered:
        current = script.find(token, cursor + 1)
        if current < 0:
            errors.append(f"runner session-one order is missing: {token}")
            break
        cursor = current
    if script.count('"_.STRETCH"') != 1:
        errors.append("runner must issue exactly one real top-level native STRETCH")
    if script.count('"QS3DSYNCSOURCE"') != 1:
        errors.append("runner must reconcile exactly once after the native vertex edit")
    if script.count('"QS3DBUILD3D"') != 1:
        errors.append("runner must rebuild exactly once after reconcile")

required_runner_tokens = (
    "QS3DSRPOLYREOPEN",
    "QS3D_SOURCE_RECONCILE_NATIVE_POLYLINE_RESULT",
    "QS3D_SOURCE_RECONCILE_NATIVE_POLYLINE_PHASE_RESULT",
    "QS3D_SOURCE_RECONCILE_NATIVE_POLYLINE_NONCE",
    "QS3D_SOURCE_RECONCILE_NATIVE_POLYLINE_DWG",
    "status --porcelain=v1 --untracked-files=all",
    "qualification requires a clean exact-SHA worktree",
    "ProductVersion",
    "EndsWith($expectedAssemblyRevision",
    "FixtureDwg must be the repository-generated QS3D sample",
    "ArtifactDir must stay outside the repository",
    "Close existing BricsCAD processes",
    "Start-Process -FilePath $bricscadExe",
    "-WindowStyle Hidden",
    "Stop-Qs3dLateHandoffProcesses",
    "process_cleanup_verified",
    "script_cleanup_verified",
    "private_state_cleanup_verified",
    "drawing_restore_verified",
    "drawing_persisted_changed",
    "sidecar_persisted",
    "LOCAL_004_P02_CLOSED_POLYLINE_VERTEX",
)
for token in required_runner_tokens:
    if token not in runner:
        errors.append(f"runner is missing exact-SHA/privacy/cleanup contract token: {token}")

if runner.count('"QS3DSRPOLYREOPEN"') < 1:
    errors.append("runner must execute the cold-reopen verification command")
if "Get-Clipboard" in runner or "Set-Clipboard" in runner:
    errors.append("runner must not inspect or mutate the user's clipboard")
for forbidden_marker_key in (
    '"handle=', '"element_id=', '"project_id=', '"drawing_path=',
    '"exception_message=', '"stack_trace=', '"exception_type=',
):
    if forbidden_marker_key in probe.lower():
        errors.append("sanitized probe marker must not publish IDs, handles, paths, or raw exception details")

claim_tokens = (
    "issue #3287",
    "Status: `ACTIVE`",
    "Lane-Key: `issue-3287`",
    "closed POLYLINE",
    "STRETCH",
    "AreaM2 13.5",
    "1.62 m3",
    "automation-only",
    "#74",
    "#83",
    "Parent issue #80 remains open",
)
for token in claim_tokens:
    if token not in claim:
        errors.append(f"claim is missing bounded ownership token: {token}")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: LOCAL-004 P02 drives one production Direct Draw Slab through a real top-level closed-POLYLINE vertex STRETCH, production reconcile/rebuild/save, cold reopen and exact privacy/cleanup guards without direct probe mutation.")
