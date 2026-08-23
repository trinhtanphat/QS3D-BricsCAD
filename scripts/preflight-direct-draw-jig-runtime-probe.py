#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PROBE = ROOT / "src" / "QS3D.BricsCAD.V25" / "DirectDrawJigRuntimeProbeCommands.cs"
JIG = ROOT / "src" / "QS3D.BricsCAD.V25" / "DirectDrawProfileStripJig.cs"
RUNNER = ROOT / "scripts" / "test-bricscad-v25-direct-draw-jig-lifecycle.ps1"

errors = []
for path in (PROBE, JIG, RUNNER):
    if not path.is_file():
        errors.append(f"missing LOCAL-008 P02 source-prep file: {path.relative_to(ROOT)}")

if not errors:
    probe = PROBE.read_text(encoding="utf-8")
    jig = JIG.read_text(encoding="utf-8")
    runner = RUNNER.read_text(encoding="utf-8")

    required_probe = (
        'CommandMethod("QS3DPROBEDIRECTDRAWJIG"',
        'editor.Drag(jig)',
        'new DirectDrawProfileStripJig(start, widthDrawingUnits, ucsToWcs)',
        'RequireSameDocument(document)',
        'MinimumQualifiedSegments = 3',
        'acceptedSegments >= MinimumQualifiedSegments',
        'termination == "ENTER" || termination == "ESC_OR_CANCEL"',
        '"|qualified_candidate=" + (qualifiedCandidate ? "true" : "false")',
        'accepted_segments=',
        'minimum_segments=',
        'preview_model=DrawJigProfileStrip',
        'coordinate_model=EDITOR_UCS_TO_JIG_WCS_UCS_PLANE',
        'var ucsToWcs = editor.CurrentUserCoordinateSystem;',
        'var start = first.Value.TransformBy(ucsToWcs);',
        'persistent_writes=0',
        'ownership_writes=0',
        'QS3D_DIRECT_DRAW_JIG_RUNTIME_V1',
    )
    required_jig = (
        'internal sealed class DirectDrawProfileStripJig : DrawJig',
        'prompts.AcquirePoint(options)',
        'SamplerStatus.Cancel',
        'SamplerStatus.NoChange',
        'SamplerStatus.OK',
        'UserInputControls.NullResponseAccepted',
        'worldDraw.Geometry.WorldLine',
        'BasePoint = _startWcs',
        '_endWcs = result.Value;',
        '_wcsToUcs = ucsToWcs.Inverse();',
        'var localStart = _startWcs.TransformBy(_wcsToUcs);',
        'var localEnd = _endWcs.TransformBy(_wcsToUcs);',
        'worldDraw.Geometry.WorldLine(_startWcs, _endWcs);',
    )
    for token in required_probe:
        if token not in probe:
            errors.append(f"LOCAL-008 P02 probe missing contract token: {token}")
    for token in required_jig:
        if token not in jig:
            errors.append(f"shared Direct Draw profile jig missing contract token: {token}")

    forbidden_probe = (
        'StartTransaction(', 'OpenMode.ForWrite', 'AppendEntity(', '.Erase(',
        'ProjectContextCoordinator.GetOrCreate', 'SemanticCaptureService.Capture',
        'RegenerateDirtySubset', 'GeneratedGeometryService', 'SendStringToExecute',
        '.Editor.Command(',
        '"|qualified_candidate=true"',
        'coordinate_model=WCS_INPUT_UCS_PLANE',
        'var start = first.Value;',
        'var centerStart = _startWcs.TransformBy(',
        'var centerEnd = _endWcs.TransformBy(',
        '_start.TransformBy(_ucs)',
        '_end.TransformBy(_ucs)',
    )
    probe_and_jig = probe + "\n" + jig
    for token in forbidden_probe:
        if token in probe_and_jig:
            errors.append(f"LOCAL-008 P02 probe must stay database/ownership free and preserve Editor-UCS -> Jig-WCS coordinate boundaries: {token}")

    if probe.count('CommandMethod("QS3DPROBEDIRECTDRAWJIG"') != 1:
        errors.append("QS3DPROBEDIRECTDRAWJIG must be registered exactly once")
    if jig.count('worldDraw.Geometry.WorldLine') < 5:
        errors.append("profile strip probe must draw four profile edges plus a center line")

    required_runner = (
        'preflight-direct-draw-jig-runtime-probe.py',
        'BRICSCAD_V25_DIR',
        'QS3DPROBEDIRECTDRAWJIG',
        'QS3D_DIRECT_DRAW_JIG_RUNTIME_V1',
        'coordinate_model=EDITOR_UCS_TO_JIG_WCS_UCS_PLANE',
        'git', 'rev-parse', 'status --porcelain=v1',
        '[string[]]$ArgumentList', '@ArgumentList',
        'rotated or translated UCS',
        'first point',
        'anchored to the picked cursor points',
        'PENDING_LOCAL',
    )
    for token in required_runner:
        if token not in runner:
            errors.append(f"LOCAL-008 P02 runner missing exact-SHA/local token: {token}")

    forbidden_runner = ('[string[]]$Args', '@Args')
    for token in forbidden_runner:
        if token in runner:
            errors.append(f"LOCAL-008 P02 runner must not shadow PowerShell automatic $Args: {token}")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: LOCAL-008 P02 source-prep pins Editor first-point UCS->WCS normalization, WCS DrawJig acquisition/base points, UCS-plane profile offset math, repeated click lifecycle, document/UCS safety and zero persistent preview writes; licensed V25 execution remains PENDING_LOCAL.")
