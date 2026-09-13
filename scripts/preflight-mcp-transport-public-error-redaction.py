from pathlib import Path
p=Path('src/QS3D.BricsCAD.V25/McpTransportAgentCenterAugmenter.cs')
s=p.read_text(encoding='utf-8-sig')
need=['McpPublicTextSanitizer.Sanitize(ex.ToString())','McpPublicTextSanitizer.Sanitize(ex.Message)']
raw=['McpAgentExperience.Error("onboarding", "Không copy được lệnh WinGet recovery: " + ex.Message','McpAgentExperience.Error("onboarding", "Không copy được tunnel diagnostics: " + ex.Message','McpAgentExperience.Error("onboarding", "Không mở được tunnel diagnostics log: " + ex.Message','McpAgentExperience.Error("onboarding", "Restart OpenAI tunnel lỗi: " + ex.Message','MessageBox.Show(ex.Message, "QS3D MCP"']
for x in raw:
    if x in s:
        raise SystemExit('FAIL raw public exception sink: '+x)
if not any(x in s for x in need):
    raise SystemExit('FAIL canonical sanitizer missing')
if 'var publicMessage = ok ? message : McpPublicTextSanitizer.Sanitize(message);' not in s:
    raise SystemExit('FAIL tunnel Start failure message is not sanitized before public sinks')
if 'McpAgentExperience.Error("onboarding", message,' in s:
    raise SystemExit('FAIL raw tunnel Start failure message reaches Agent Experience')
print('PASS: MCP transport public exception sinks are sanitized')
