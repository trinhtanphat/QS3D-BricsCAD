from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / 'src/QS3D.BricsCAD.V25/McpTransportSupervisor.cs').read_text(encoding='utf-8-sig')
errors = []

for raw in (
    '"Cannot persist owned tunnel process identity: " + ex.Message',
    '"Cannot prove/clean QS3D-owned stale tunnel process: " + ex.Message',
):
    if raw in source:
        errors.append('raw exception-derived transport ownership error remains: ' + raw)

required = (
    '"Cannot persist owned tunnel process identity: " + SanitizePublicError(ex)',
    '"Cannot prove/clean QS3D-owned stale tunnel process: " + SanitizePublicError(ex)',
    'private static string SanitizePublicError(Exception exception)',
    'McpPublicTextSanitizer.Sanitize(exception.ToString())',
)
for token in required:
    if token not in source:
        errors.append('missing transport public-error contract: ' + token)

if errors:
    print('MCP transport-supervisor public error preflight FAILED')
    for error in errors:
        print(' - ' + error)
    raise SystemExit(1)

print('MCP transport-supervisor public error preflight PASS')
