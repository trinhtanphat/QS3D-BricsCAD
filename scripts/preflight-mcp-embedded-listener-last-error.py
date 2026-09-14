from pathlib import Path

src = Path('src/QS3D.BricsCAD.V25/McpEmbeddedServer.cs').read_text(encoding='utf-8')
needle = 'private static void SetLastError(string message)'
pos = src.find(needle)
if pos < 0:
    raise SystemExit('FAIL: SetLastError boundary missing')
block = src[pos:pos+320]
if 'McpPublicTextSanitizer.Sanitize' not in block and 'SanitizePublicError' not in block:
    raise SystemExit('FAIL: embedded listener LastError publication is not canonically sanitized')
if '_lastError = message ?? string.Empty' in block:
    raise SystemExit('FAIL: raw listener/request diagnostic can still reach LastError')
print('PASS: embedded listener LastError publication is sanitized')
