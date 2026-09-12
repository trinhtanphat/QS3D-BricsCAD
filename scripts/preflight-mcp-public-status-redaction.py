#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
helper = ROOT / 'src/QS3D.BricsCAD.V25/McpPublicTextSanitizer.cs'
v1 = ROOT / 'src/QS3D.BricsCAD.V25/McpEmbeddedServer.cs'
v2 = ROOT / 'src/QS3D.BricsCAD.V25/McpEmbeddedServerV2.cs'
status = ROOT / 'src/QS3D.BricsCAD.V25/McpCadViewStatusRuntime.cs'
qs3d = ROOT / 'src/QS3D.BricsCAD.V25/McpQs3dDomainRuntime.cs'
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

text = qs3d.read_text(encoding='utf-8-sig')
if 'internal static string BuildStatusJson(bool deprecatedAlias)' not in text:
    errors.append('missing QS3D BuildStatusJson')
if 'Escape(McpPublicTextSanitizer.Sanitize(contextReason))' not in text:
    errors.append('qs3d status contextReason crosses public success boundary unsanitized')
if 'Escape(McpPublicTextSanitizer.Sanitize(errorMessage))' not in text:
    errors.append('qs3d status error message crosses public success boundary unsanitized')
if 'Escape(contextReason)' in text:
    errors.append('qs3d status still serializes raw contextReason')
if 'Escape(errorMessage)' in text:
    errors.append('qs3d status still serializes raw error message')

if errors:
    print('MCP public status redaction preflight FAILED')
    for error in errors:
        print(' - ' + error)
    sys.exit(1)
print('MCP public status redaction preflight PASS')
