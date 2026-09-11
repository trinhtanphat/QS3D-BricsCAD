#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
helper = ROOT / 'src/QS3D.BricsCAD.V25/McpPublicTextSanitizer.cs'
v1 = ROOT / 'src/QS3D.BricsCAD.V25/McpEmbeddedServer.cs'
v2 = ROOT / 'src/QS3D.BricsCAD.V25/McpEmbeddedServerV2.cs'
status = ROOT / 'src/QS3D.BricsCAD.V25/McpCadViewStatusRuntime.cs'
errors = []

if not helper.exists():
    errors.append('missing shared McpPublicTextSanitizer')
else:
    text = helper.read_text(encoding='utf-8-sig')
    for token in ('MaxPublicTextCharacters = 512', 'Authorization', '[REDACTED]', '[PATH]', 'char.IsControl'):
        if token not in text:
            errors.append('shared sanitizer missing contract token: ' + token)

for path in (v1, v2):
    text = path.read_text(encoding='utf-8-sig')
    block = re.search(r'private static string SanitizePublicError\(string message\)\s*\{(?P<body>.*?)\n\s*\}', text, re.S)
    if not block or 'McpPublicTextSanitizer.Sanitize' not in block.group('body'):
        errors.append(path.name + ': public error wrapper is not delegated to shared sanitizer')

text = status.read_text(encoding='utf-8-sig')
status_block = re.search(r'private static string AgentStatusJson\(\)\s*\{(?P<body>.*?)\n\s*\}', text, re.S)
if not status_block:
    errors.append('missing AgentStatusJson')
else:
    body = status_block.group('body')
    if 'McpPublicTextSanitizer.Sanitize(McpAgentExperience.LastError)' not in body:
        errors.append('agent_status lastError crosses public success boundary unsanitized')
    if 'Bound(McpAgentExperience.LastError' in body:
        errors.append('agent_status still treats raw LastError as ordinary bounded status text')

if errors:
    print('MCP public status redaction preflight FAILED')
    for error in errors:
        print(' - ' + error)
    sys.exit(1)
print('MCP public status redaction preflight PASS')
