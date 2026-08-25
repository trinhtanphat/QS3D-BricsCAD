from pathlib import Path

root = Path(__file__).resolve().parents[1]
runner = (root / "scripts" / "run-local-v25-local-010.ps1").read_text(encoding="utf-8")
runbook = (root / "docs" / "LOCAL-010-PERFORMANCE-UI-QUALIFICATION.md").read_text(encoding="utf-8")

required_runner = [
    'localItem="LOCAL-010"', 'localPassClaimedByRunner=$false', 'git rev-parse HEAD',
    'git status --porcelain', 'run-local-v25-qualification.ps1', 'runtimeSmokeStatus',
    'Get-Process -Name bricscad', 'performance.dependency_graph', 'performance.rebar_limits',
    'ui.start_center_100', 'ui.start_center_200', 'ui.ribbon_100', 'ui.ribbon_200',
    'ui.workspace_narrow', 'ui.workspace_wide', 'ui.document_switch_cleanup',
    'exit 1', 'exit 2', 'exit 3'
]
for token in required_runner:
    if token not in runner:
        raise SystemExit(f"ERROR: LOCAL-010 runner contract missing {token!r}")

required_runbook = [
    'run-local-v25-local-010.ps1', 'sanitized/disposable', '100%', '125%', '150%', '200%',
    'narrow', 'normal', 'wide', 'V25 and V26 identities are separate',
    'LOCAL-010-START-CENTER-HANDOFF-2026-08-17.md', '`Dự án`', '`Cấu hình`',
    '`Mô hình`', '`BQ`', 'exactly once', 'no standalone QS3D application/process',
    'localPassClaimedByRunner=false', 'NO_RESULT'
]
for token in required_runbook:
    if token not in runbook:
        raise SystemExit(f"ERROR: LOCAL-010 runbook contract missing {token!r}")

if 'Stop-Process' in runner or 'taskkill' in runner.lower():
    raise SystemExit("ERROR: LOCAL-010 runner must not kill unrelated BricsCAD sessions")
if 'localPassClaimedByRunner=$true' in runner:
    raise SystemExit("ERROR: LOCAL-010 runner must never manufacture LOCAL_PASS")

print("PASS: LOCAL-010 pull-and-run qualification contract is source-ready")
