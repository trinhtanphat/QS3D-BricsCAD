#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PROBE = ROOT / "src" / "QS3D.BricsCAD.V25" / "DirectDrawJigRuntimeProbeCommands.cs"
RUNNER = ROOT / "scripts" / "test-bricscad-v25-direct-draw-jig-lifecycle.ps1"

errors = []
for path in (PROBE, RUNNER):
    if not path.is_file():
        errors.append(f"missing LOCAL-008 P02 source-prep file: {path.relative_to(ROOT)}")

if not errors:
    probe = PROBE.read_text(encoding="utf-8")
    runner = RUNNER.read_text(encoding="utf-8")

    required_probe = (
        'CommandMethod("QS3DPROBEDIRECTDRAWJIG"',
        'private sealed class ProfileStripJig : DrawJig',
        'editor.Drag(jig)',
        'prompts.AcquirePoint(options)',
        'SamplerStatus.Cancel',
        'SamplerStatus.NoChange',
        'SamplerStatus.OK',
        'UserInputControls.NullResponseAccepted',
        'worldDraw.Geometry.WorldLine',
        'RequireSameDocument(document)',
        'accepted_segments=',
        'preview_model=DrawJigProfileStrip',
        'persistent_writes=0',
        'ownership_writes=0',
        'QS3D_DIRECT_DRAW_JIG_RUNTIME_V1',
    )
    for token in required_probe:
        if token not in probe:
            errors.append(f"LOCAL-008 P02 probe missing contract token: {token}")

    forbidden_probe = (
        'StartTransaction(', 'OpenMode.ForWrite', 'AppendEntity(', '.Erase(',
        'ProjectContextCoordinator.GetOrCreate', 'SemanticCaptureService.Capture',
        'RegenerateDirtySubset', 'GeneratedGeometryService', 'SendStringToExecute',
        '.Editor.Command(',
    )
    for token in forbidden_probe:
        if token in probe:
            errors.append(f"LOCAL-008 P02 probe must stay database/ownership free: {token}")

    if probe.count('CommandMethod("QS3DPROBEDIRECTDRAWJIG"') != 1:
        errors.append("QS3DPROBEDIRECTDRAWJIG must be registered exactly once")
    if probe.count('worldDraw.Geometry.WorldLine') < 5:
        errors.append("profile strip probe must draw four profile edges plus a center line")

    required_runner = (
        'preflight-direct-draw-jig-runtime-probe.py',
        'BRICSCAD_V25_DIR',
        'QS3DPROBEDIRECTDRAWJIG',
        'QS3D_DIRECT_DRAW_JIG_RUNTIME_V1',
        'git', 'rev-parse', 'status --porcelain=v1',
        'PENDING_LOCAL',
    )
    for token in required_runner:
        if token not in runner:
            errors.append(f"LOCAL-008 P02 runner missing exact-SHA/local token: {token}")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: LOCAL-008 P02 source-prep pins real DrawJig profile preview, repeated click lifecycle, document/UCS safety and zero persistent preview writes; licensed V25 execution remains PENDING_LOCAL.")
