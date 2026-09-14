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