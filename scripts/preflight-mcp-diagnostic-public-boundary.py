from pathlib import Path

root = Path(__file__).resolve().parents[1]
src = (root / 'src/QS3D.BricsCAD.V25/McpDiagnosticHub.cs').read_text(encoding='utf-8-sig')
errors = []

record = 'var safeMessage = McpPublicTextSanitizer.Sanitize(Redact(message));'
if record not in src:
    errors.append('McpDiagnosticHub.Record does not canonical-sanitize persisted/public diagnostic messages')

if 'var safeMessage = Redact(message);' in src:
    errors.append('McpDiagnosticHub.Record still persists Redact-only diagnostic text')

if 'cad_audit_tail' not in (root / 'src/QS3D.BricsCAD.V25/McpEmbeddedServer.cs').read_text(encoding='utf-8-sig'):
    errors.append('cad_audit_tail public retrieval surface missing; contract audit cannot prove publication path')

if 'McpPublicTextSanitizer.Sanitize(Redact(message))' not in src:
    errors.append('canonical public sanitizer must wrap the existing secret redaction at the persisted audit boundary')

if errors:
    print('MCP diagnostic public boundary preflight FAILED')
    for error in errors:
        print(' - ' + error)
    raise SystemExit(1)

print('MCP diagnostic public boundary preflight PASS')
