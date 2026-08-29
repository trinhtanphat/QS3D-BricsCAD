#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
V25 = ROOT / "src" / "QS3D.BricsCAD.V25"
V26 = ROOT / "src" / "QS3D.BricsCAD.V26"


def text(path: Path) -> str:
    if not path.exists():
        raise SystemExit(f"missing required source: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require(haystack: str, needle: str, label: str) -> None:
    if needle not in haystack:
        raise SystemExit(f"missing {label}: {needle}")


def forbid(haystack: str, needle: str, label: str) -> None:
    if needle in haystack:
        raise SystemExit(f"forbidden {label}: {needle}")


session = text(V25 / "McpDesktopControlSession.cs")
require(session, "RequireLocalConsent", "local desktop consent gate")
require(session, "BeginGuardedAction", "visible desktop action scope")
require(session, "WH_KEYBOARD_LL", "global low-level keyboard hook")
require(session, "DoubleEscapeWindow", "double-Esc timing window")
require(session, "McpCadAgentRuntime.StopAutomation", "emergency-stop epoch integration")
require(session, "McpDesktopControlOverlayWindow", "blue overlay")
require(session, "_consentGeneration", "desktop consent generation")
require(session, "IsSensitiveReadTool", "sensitive desktop-read classification")
require(session, '"desktop_clipboard_read"', "clipboard read consent-revocation protection")
require(session, '"desktop_screenshot"', "screenshot consent-revocation protection")
require(session, "_consentGeneration != _consentGenerationAtStart", "mid-flight consent generation revalidation")
require(session, "payload was discarded", "fail-closed sensitive payload suppression")

# Completion Pack A: consent is local-only, explicitly pausable/resumable and idle-expiring.
require(session, "ConsentIdleTimeout", "10-minute desktop consent idle timeout")
require(session, "TimeSpan.FromMinutes(10)", "10-minute desktop consent value")
require(session, "IdleRemaining", "desktop consent idle countdown")
require(session, "PauseFromLocalUser", "local desktop pause")
require(session, "ResumeFromLocalUser", "local desktop resume")
require(session, "ExpireConsentIfIdle", "synchronous fail-closed idle expiry")
require(session, "ActionId", "guarded desktop action id")

runtime = text(V25 / "McpDesktopAutomationRuntime.cs")
require(runtime, "McpDesktopControlSession.RequireLocalConsent", "runtime local-consent enforcement")
require(runtime, "McpDesktopControlSession.BeginGuardedAction", "runtime active overlay scope")
require(runtime, "desktop_clipboard_read", "clipboard read tool remains present")
require(runtime, "desktop_screenshot", "screenshot tool remains present")

# Completion Pack A tool surface. Approach B macro/sequence remains intentionally absent.
require(runtime, '"desktop_mouse_drag"', "bounded desktop drag tool")
require(runtime, '"desktop_wait_for_window"', "bounded wait-for-window tool")
require(runtime, "WaitForWindow", "wait-for-window routing")
require(runtime, "MouseDrag", "drag routing")
require(runtime, "RequirePointInsideWindow", "exact target point validation")
require(runtime, "cropX", "screenshot crop x")
require(runtime, "cropY", "screenshot crop y")
require(runtime, "cropWidth", "screenshot crop width")
require(runtime, "cropHeight", "screenshot crop height")
require(runtime, "CHARACTER", "alphanumeric key audit redaction")
forbid(runtime, '"desktop_sequence"', "Approach B desktop sequence tool")
forbid(runtime, '"desktop_macro"', "Approach B desktop macro tool")

recovery = text(V25 / "McpProjectRecoveryService.cs")
require(recovery, '"SAVETIME"', "BricsCAD autosave interval")
require(recovery, '"ISAVEBAK"', "BricsCAD BAK safety")
require(recovery, "MaxSnapshotsPerProject", "bounded recovery retention")
require(recovery, "RecoverLatestToCopy", "non-destructive restore-to-copy")
require(recovery, "FileShare.ReadWrite | FileShare.Delete", "live DWG read sharing")

experience = text(V25 / "McpAgentExperience.cs")
require(experience, "MaxEvents", "bounded local event timeline")
require(experience, "DetermineOnboarding", "onboarding state machine")
require(experience, "CloudflaredMissing", "cloudflared prerequisite state")
require(experience, "ChatGptRegistrationRequired", "ChatGPT registration state")
require(experience, "ActionId", "timeline action id")
require(experience, "DurationMilliseconds", "timeline action duration")
require(experience, "TerminalState", "timeline action terminal state")
require(experience, "StartDesktopAction", "desktop action timeline start")
require(experience, "CompleteDesktopAction", "desktop action timeline completion")

ui = text(V25 / "McpAgentControlCenter.cs")
for tab in ("Kết nối", "Agent", "Backup & khôi phục", "Nâng cao"):
    require(ui, tab, f"Control Center tab {tab}")
require(ui, "Mở ChatGPT", "system-browser ChatGPT action")
require(ui, "Đăng nhập Cloudflare", "provider-browser Cloudflare action")
require(ui, "OAuth", "OAuth-first ChatGPT guidance")
require(ui, "Quick Tunnel · test only", "test-only Quick Tunnel wording")
require(ui, "Pause desktop", "local pause control")
require(ui, "Resume desktop", "local resume control")
require(ui, "Idle còn", "desktop idle countdown copy")
require(ui, "Action ID", "desktop action-id display")
require(ui, "Kiểm tra drawing/backup", "post-stop recovery guidance")
forbid(ui, "MessageBox.Show", "blocking Agent Center MessageBox")

first_run = text(V25 / "McpFirstRunExperience.cs")
require(first_run, "McpToastNotificationWindow", "first-run toast window")
require(first_run, "cloudflared", "cloudflared toast guidance")

v25_entry = text(V25 / "PluginEntry.cs")
for needle in ("McpProjectRecoveryService.Start", "McpFirstRunExperience.Start", "McpProjectRecoveryService.Stop", "McpDesktopControlSession.Shutdown"):
    require(v25_entry, needle, "V25 lifecycle integration")

v26_entry = text(V26 / "PluginEntry.cs")
for needle in ("McpEmbeddedServer.Start", "McpCloudflareAccountTunnelManager.TryAutoStart", "McpProjectRecoveryService.Start", "McpFirstRunExperience.Start"):
    require(v26_entry, needle, "V26 MCP lifecycle parity")

print("PASS MCP guided onboarding + desktop Completion Pack A + recovery source contract")
