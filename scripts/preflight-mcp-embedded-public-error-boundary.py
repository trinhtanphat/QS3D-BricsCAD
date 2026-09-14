from pathlib import Path
files={
 'v2':Path('src/QS3D.BricsCAD.V25/McpEmbeddedServerV2.cs').read_text(encoding='utf-8-sig'),
 'legacy':Path('src/QS3D.BricsCAD.V25/McpEmbeddedServer.cs').read_text(encoding='utf-8-sig'),
 'first':Path('src/QS3D.BricsCAD.V25/McpFirstRunExperience.cs').read_text(encoding='utf-8-sig'),
}
# Protocol and tool-error serializers must sanitize at the final public boundary.
for name in ('v2','legacy'):
    s=files[name]
    if 'JsonEscape(SanitizePublicError(message' not in s:
        raise SystemExit(f'FAIL {name}: JSON-RPC public error serializer is not sanitized')
    if 'JsonEscape(SanitizePublicError(' not in s:
        raise SystemExit(f'FAIL {name}: public tool/fallback error sanitizer missing')
# First-run Agent Experience is a direct public sink and must not concatenate raw exception text.
raw='McpAgentExperience.Error("onboarding", "Kh'
if '+ ex.Message' in files['first'] and 'McpAgentExperience.Error("onboarding"' in files['first']:
    raise SystemExit('FAIL first-run: raw exception message reaches Agent Experience')
if 'McpPublicTextSanitizer.Sanitize(ex.ToString())' not in files['first']:
    raise SystemExit('FAIL first-run: canonical public sanitizer missing')
print('PASS: MCP embedded/public runtime error boundaries are sanitized')
extra = {
    'recovery': Path('src/QS3D.BricsCAD.V25/McpProjectRecoveryService.cs').read_text(encoding='utf-8-sig'),
    'persistent': Path('src/QS3D.BricsCAD.V25/McpPersistentAgentCenterAugmenter.cs').read_text(encoding='utf-8-sig'),
    'cloudflare': Path('src/QS3D.BricsCAD.V25/McpCloudflareAccountOnboarding.cs').read_text(encoding='utf-8-sig'),
}
raw_extra = {
    'recovery': ['"Periodic backup lỗi: " + ex.Message', '"Backup thất bại: " + ex.Message', '"Khôi phục thất bại: " + ex.Message'],
    'persistent': ['Credential Manager: " + ex.Message', '"unknown error" : error.Message'],
    'cloudflare': ['account setup lỗi: " + ex.Message', 'SetState("Named Tunnel auto-start failed.", ex.Message)', 'SetState("Named Tunnel auto-start scheduling failed.", ex.Message)', 'Notify("Không kết nối được", Friendly(ex.Message))', 'Notify("MCP local lỗi", Friendly(ex.Message))'],
}
for name, needles in raw_extra.items():
    for needle in needles:
        if needle in extra[name]:
            raise SystemExit(f'FAIL {name}: raw public exception sink: {needle}')
    if 'McpPublicTextSanitizer.Sanitize(' not in extra[name]:
        raise SystemExit(f'FAIL {name}: canonical public sanitizer missing')
print('PASS: adjacent recovery/persistent/cloudflare exception sinks are sanitized')