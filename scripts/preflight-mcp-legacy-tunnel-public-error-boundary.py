from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
files = {
    'fallback': ROOT / 'src/QS3D.BricsCAD.V25/McpCloudflareOnboarding.cs',
    'bootstrap': ROOT / 'src/QS3D.BricsCAD.V25/McpCloudflaredBootstrapper.cs',
    'connector': ROOT / 'src/QS3D.BricsCAD.V25/McpConnectorRibbonCommands.cs',
    'openai': ROOT / 'src/QS3D.BricsCAD.V25/McpOpenAiSecureTunnel.cs',
}
text = {name: path.read_text(encoding='utf-8-sig') for name, path in files.items()}
raw = {
    'fallback': ['fallback setup lỗi: " + ex.Message', 'tunnel settings: " + ex.Message', 'tunnel token: " + ex.Message', '_lastError = ex.Message;', 'error = ex.Message;'],
    'bootstrap': ['Cloudflare Tunnel: " + ex.Message', 'signer certificate: " + ex.Message'],
    'connector': ['không mở được: " + ex.Message', '"MCP lỗi: " + ex.Message', 'status + " " + ex.Message', 'protocol error: " + ex.Message'],
    'openai': ['tunnel-client: " + ex.Message', 'Runtime API key an toàn trước khi khởi động tunnel: " + ex.Message', 'Secure MCP Tunnel: " + ex.Message', 'autostart=OFF: " + ex.Message', 'error = ex.Message;', 'message = ex.Message;', 'watchdog error: " + ex.Message', 'signer certificate: " + ex.Message', 'SHA-256: " + ex.Message'],
}
for name, needles in raw.items():
    for needle in needles:
        if needle in text[name]:
            raise SystemExit(f'FAIL {name}: raw public exception sink: {needle}')
    if 'McpPublicTextSanitizer.Sanitize(' not in text[name]:
        raise SystemExit(f'FAIL {name}: canonical public sanitizer missing')
print('PASS: legacy MCP tunnel/bootstrap public exception boundaries are sanitized')
