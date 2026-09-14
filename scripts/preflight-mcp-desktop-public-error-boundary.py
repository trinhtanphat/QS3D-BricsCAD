from pathlib import Path

root = Path(__file__).resolve().parents[1]
session = (root / 'src/QS3D.BricsCAD.V25/McpDesktopControlSession.cs').read_text(encoding='utf-8-sig')
automation = (root / 'src/QS3D.BricsCAD.V25/McpDesktopAutomationRuntime.cs').read_text(encoding='utf-8-sig')
errors = []

required_session = [
    'McpPublicTextSanitizer.Sanitize(ex.ToString())',
    'McpPublicTextSanitizer.Sanitize(value)',
]
for token in required_session:
    if token not in session:
        errors.append(f'McpDesktopControlSession missing public sanitizer token: {token}')

forbidden_session = [
    'quyền desktop: " + ex.Message',
    'BoundMessage(ex.Message)',
]
for token in forbidden_session:
    if token in session:
        errors.append('McpDesktopControlSession raw public exception sink remains')

if 'Cause: "\\n                        + ex.Message' in automation:
    errors.append('McpDesktopAutomationRuntime desktop_sequence exposes raw exception message')
if 'McpPublicTextSanitizer.Sanitize(ex.ToString())' not in automation:
    errors.append('McpDesktopAutomationRuntime missing canonical public sanitizer at sequence failure boundary')

if errors:
    print('MCP desktop public error boundary preflight FAILED')
    for error in errors:
        print(' - ' + error)
    raise SystemExit(1)

print('MCP desktop public error boundary preflight PASS')
