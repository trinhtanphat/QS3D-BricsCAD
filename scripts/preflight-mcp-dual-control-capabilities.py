#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"
BACKGROUND = SRC / "McpBackgroundHostRuntime.cs"
CENTER = SRC / "McpAgentControlCenter.cs"
AUGMENTER = SRC / "McpPersistentAgentCenterAugmenter.cs"


def fail(message: str) -> None:
    print(f"ERROR: MCP dual-control preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


for path in (BACKGROUND, CENTER, AUGMENTER):
    if not path.is_file():
        fail(f"missing {path.relative_to(ROOT)}")

background = BACKGROUND.read_text(encoding="utf-8")
center = CENTER.read_text(encoding="utf-8")
augmenter = AUGMENTER.read_text(encoding="utf-8")

required_background = {
    "background descriptor family": "BACKGROUND CONTROL:",
    "background capability object": "backgroundControl",
    "foreground capability object": "foregroundControl",
    "default background route": "defaultRoute",
    "explicit-only fallback": "explicit_only",
    "implicit fallback disabled": "implicitForegroundFallback",
    "local foreground enable helper": "EnableForegroundFromLocalUser",
    "local foreground disable helper": "DisableForegroundFromLocalUser",
    "local consent gate": 'McpDesktopControlSession.RequireLocalConsent("foreground-local-enable")',
    "local consent state": "McpDesktopControlSession.IsEnabled",
    "same-process target validation": "BelongsToCurrentProcess",
    "bounded window message": "SendMessageTimeout",
}
for label, token in required_background.items():
    if token not in background:
        fail(f"missing {label}: {token}")

required_agent_surface = {
    "background card": '"Thao tác nền · Background Control"',
    "foreground card": '"Thao tác trực tiếp · Foreground Control"',
    "background status row": '"Background control"',
    "foreground status row": '"Foreground control"',
    "background preferred copy": "ưu tiên mặc định",
    "no silent fallback copy": "không tự chuyển sang thao tác trực tiếp",
    "local foreground enable sync": "McpBackgroundHostRuntime.EnableForegroundFromLocalUser()",
    "local foreground disable sync": "McpBackgroundHostRuntime.DisableForegroundFromLocalUser()",
    "resume synchronization": "SynchronizeForegroundEnableFromLocalUser",
    "pause/emergency synchronization": "SynchronizeForegroundDisableFromLocalUser",
}
for label, token in required_agent_surface.items():
    if token not in augmenter:
        fail(f"missing {label}: {token}")

# Preserve the legacy compatibility policy contract while exposing the richer capability state.
for token in (
    '"background_only"',
    '"foreground_fallback"',
    '"mode"',
    '"globalInputAllowed"',
    '"defaultMode"',
    '"processScoped"',
):
    if token not in background:
        fail(f"legacy interaction-policy contract regressed: {token}")

# Background code must fail closed instead of stealing cursor/focus/keyboard or launching processes.
for forbidden in (
    "McpDesktopAutomationRuntime.Call(",
    "SendInput(",
    "SetCursorPos(",
    "SetForegroundWindow(",
    "Process.Start(",
    "CreateProcess(",
    "cmd.exe",
    "powershell.exe",
    "pwsh.exe",
):
    if forbidden.lower() in background.lower():
        fail(f"background runtime contains forbidden foreground/execution fallback: {forbidden}")

# Agent Center must not remotely enable desktop consent through an MCP tool.
if 'InvokeControlTool("cad_agent_resume"' in center:
    fail("Agent Center foreground consent must remain local-only")

# Local UI synchronization must fail closed: restoring background_only and revoking foreground policy.
fail_closed = augmenter.split("private static void FailClosedForegroundAccess", 1)[1].split("private static void TrySetInteractionPolicy", 1)[0]
if 'TrySetInteractionPolicy("background_only")' not in fail_closed:
    fail("foreground synchronization failure must restore background_only")
if "McpBackgroundHostRuntime.DisableForegroundFromLocalUser()" not in fail_closed:
    fail("foreground synchronization failure must revoke the local foreground policy")
if "McpAgentExperience.Error(" not in fail_closed:
    fail("foreground synchronization failure must emit a bounded local error")

print("PASS MCP dual foreground/background control source contract")
