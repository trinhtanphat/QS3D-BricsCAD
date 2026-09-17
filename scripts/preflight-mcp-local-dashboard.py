#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"
FILES = {
    "openai": SRC / "McpOpenAiSecureTunnel.cs",
    "cloudflare": SRC / "McpCloudflareAccountOnboarding.cs",
    "center": SRC / "McpAgentControlCenter.cs",
    "supervisor": SRC / "McpTransportSupervisor.cs",
    "server": SRC / "McpEmbeddedServerV2.cs",
    "control": SRC / "McpLocalDashboardControl.cs",
    "dashboard": ROOT / "tools" / "QS3D.McpDashboard" / "Program.cs",
    "project": ROOT / "tools" / "QS3D.McpDashboard" / "QS3D.McpDashboard.csproj",
    "installer": ROOT / "scripts" / "install-mcp-local-dashboard.ps1",
    "runbook": ROOT / "docs" / "MCP-CANONICAL-RUNBOOK.md",
}

def need(text, token, label, errors):
    if token not in text:
        errors.append(f"missing {label}: {token}")

def forbid(text, token, label, errors):
    if token in text:
        errors.append(f"forbidden {label}: {token}")

def main():
    errors = []
    for name, path in FILES.items():
        if not path.is_file(): errors.append(f"missing file {name}: {path.relative_to(ROOT)}")
    if errors:
        for e in errors: print("ERROR:", e)
        return 1
    text = {k: p.read_text(encoding="utf-8") for k,p in FILES.items()}

    for token, label in {
        'McpOpenAiSecureTunnelManager.TryAutoStart();': 'non-preferred OpenAI autostart',
        'McpCloudflareAccountTunnelManager.TryAutoStart();': 'non-preferred Cloudflare autostart',
        'public static bool AutoStartEnabled': 'OpenAI autostart status',
        'public static void SetAutoStart(bool enabled)': 'OpenAI autostart setter',
    }.items(): need(text['openai'], token, label, errors)
    if 'One selected transport should own external reachability at a time.' in text['openai']:
        errors.append('forbidden exclusive OpenAI transport comment/contract')
    need(text['cloudflare'], 'public static bool AutoStartEnabled', 'Cloudflare autostart status', errors)
    need(text['cloudflare'], 'public static void SetAutoStart(bool enabled)', 'Cloudflare autostart setter', errors)
    named_start = text['center'].split('private void StartNamedTunnel()', 1)[1].split('private void StartQuickTunnel()', 1)[0]
    forbid(named_start, 'McpOpenAiSecureTunnelManager.StopForHostShutdown();', 'Named start cross-stopping OpenAI', errors)
    forbid(text['supervisor'], 'StopOtherDurableProvider(provider);', 'exclusive durable-provider stop', errors)

    for token, label in {
        'request.Path.StartsWith("/qs3d/dashboard/"': 'dashboard control bridge routing',
        'IsValidDirectLocalBearer(request.Headers)': 'dashboard exact local bearer gate',
        'if (!McpOpenAiSecureTunnelManager.IsRunning) return false;': 'dual-lane OpenAI header liveness scope',
        'return ConstantTimeEquals(token, GetBearerToken());': 'constant-time local bearer validation',
    }.items(): need(text['server'], token, label, errors)
    forbid(text['server'], 'McpTransportCoordinator.SelectedProvider != McpTransportProvider.OpenAiSecureTunnel', 'selected-only OpenAI local auth', errors)

    for token, label in {
        'McpPersistentUserSettings.HasSavedOpenAiRuntimeApiKey': 'credential-presence-only status',
        'McpOpenAiSecureTunnelManager.Start(tunnelId, runtimeApiKey': 'Credential Manager-backed OpenAI start',
        'McpOpenAiSecureTunnelManager.SetAutoStart': 'OpenAI autostart control',
        'McpCloudflareAccountTunnelManager.SetAutoStart': 'Cloudflare autostart control',
        'McpPublicTextSanitizer.Sanitize': 'public diagnostic sanitization',
    }.items(): need(text['control'], token, label, errors)
    forbid(text['control'], 'CONTROL_PLANE_API_KEY=', 'serialized Runtime API key', errors)
    forbid(text['control'], 'GetBearerToken()', 'control response reading/rendering bearer', errors)

    for token, label in {
        'UseUrls($"http://127.0.0.1:{DashboardPort}")': 'loopback-only dashboard bind',
        'const int DashboardPort = 3220;': 'dashboard port 3220',
        'ContentSecurityPolicy': 'dashboard CSP',
        'X-QS3D-Dashboard': 'same-origin action header',
        'mcp-bearer-token.txt': 'server-side local bearer proxy',
        'type="password"': 'masked Runtime API key input',
        "q('runtimeKey').value='';": 'Runtime API key input clearing',
        'https://chatgpt.com/': 'ChatGPT reachability probe',
    }.items(): need(text['dashboard'], token, label, errors)
    forbid(text['dashboard'], 'app.MapGet("/api/token"', 'bearer disclosure endpoint', errors)
    forbid(text['dashboard'], 'Console.WriteLine(body)', 'request-body logging', errors)

    for token, label in {
        'QS3D MCP Local Dashboard': 'scheduled task identity',
        'New-ScheduledTaskTrigger -AtLogOn': 'logon startup',
        '-AllowStartIfOnBatteries': 'laptop-safe task startup',
        'Start-ScheduledTask -TaskName $taskName': 'scheduled-task runtime verification',
        'Stop-ScheduledTask -TaskName $taskName': 'idempotent scheduled-task shutdown',
        "Get-Process -Name ([IO.Path]::GetFileNameWithoutExtension($exeName))": 'idempotent dashboard process shutdown',
        "http://127.0.0.1:3220/": 'post-install loopback verification',
        '--self-contained false': 'framework-dependent dashboard publish',
        "Microsoft.AspNetCore.App 8.x runtime is required": 'runtime prerequisite guard',
    }.items(): need(text['installer'], token, label, errors)

    for token, label in {
        '127.0.0.1:3220': 'dashboard runbook endpoint',
        'dual-tunnel': 'dual-tunnel runbook contract',
        'Windows Credential Manager': 'secret persistence runbook contract',
    }.items(): need(text['runbook'], token, label, errors)

    if errors:
        for e in errors: print("ERROR:", e)
        return 1
    print("PASS: MCP local dashboard + dual-tunnel source contract is present and fail-closed.")
    return 0

if __name__ == "__main__":
    sys.exit(main())
