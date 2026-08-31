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
require(session, 'string.Equals(tool, "desktop_sequence", StringComparison.Ordinal)', "sequence consent-revocation protection")
require(session, "_consentGeneration != _consentGenerationAtStart", "mid-flight consent generation revalidation")
require(session, "payload was discarded", "fail-closed sensitive payload suppression")

# Completion Pack A: consent is local-only, explicitly pausable/resumable, and session-persistent after Resume.
require(session, "IdleRemaining", "legacy desktop consent idle surface")
require(session, "PauseFromLocalUser", "local desktop pause")
require(session, "ResumeFromLocalUser", "local desktop resume")
require(session, "ExpireConsentIfIdle", "idle-expiry compatibility surface")
require(session, "auto-renew", "session-persistent desktop consent wording")
require(session, "ActionId", "guarded desktop action id")
forbid(session, "ConsentIdleTimeout", "desktop consent idle timeout constant")
forbid(session, "TimeSpan.FromMinutes(10)", "10-minute desktop consent limit")
forbid(session, "Desktop consent đã EXPIRED sau 10 phút", "10-minute desktop expiry path")

runtime = text(V25 / "McpDesktopAutomationRuntime.cs")
require(runtime, "McpDesktopControlSession.RequireLocalConsent", "runtime local-consent enforcement")
require(runtime, "McpDesktopControlSession.BeginGuardedAction", "runtime active overlay scope")
require(runtime, "desktop_clipboard_read", "clipboard read tool remains present")
require(runtime, "desktop_screenshot", "screenshot tool remains present")

# Completion Pack A tool surface.
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

# Completion Pack B: one bounded single-target desktop sequence surface.
require(runtime, '"desktop_sequence"', "Approach B desktop sequence tool")
require(runtime, "MaxSequenceSteps", "sequence step cap")
require(runtime, "MaxSequenceMilliseconds", "sequence wall-clock cap")
require(runtime, "MaxSequenceDelayMilliseconds", "sequence per-step delay cap")
require(runtime, "MaxSequenceJsonCharacters", "sequence payload cap")
require(runtime, "SequenceAllowedTools", "sequence primitive allowlist")
require(runtime, "ParseSequenceSteps", "bounded sequence parser")
require(runtime, "RunSequence", "bounded sequence executor")
require(runtime, "stepsJson", "flat-transport encoded sequence payload")
require(runtime, "SequenceStep", "sequence step record")
require(runtime, "Sequence cannot include desktop_clipboard_read", "sequence clipboard-read prohibition")
require(runtime, "Sequence step arguments must not contain windowHandle", "sequence target ownership")
require(runtime, "confirmSensitiveRead=true is required for desktop_sequence screenshot steps", "sequence screenshot opt-in")
require(runtime, "Sequence screenshot is forced to the bound target window", "sequence screenshot target binding")
require(runtime, "Sequence execution is fail-fast", "sequence fail-fast contract")
require(runtime, "Sequence does not roll back completed steps", "sequence partial-execution contract")
require(runtime, "EnsureSequenceRunning", "sequence stop/duration check")
require(runtime, "EnsureSequenceStepRunning", "per-injected-input sequence stop/duration/consent check")
require(runtime, "Sequence wait-for-target timed out", "sequence wait timeout fail-fast")
forbid(runtime, '"desktop_macro"', "duplicate generic desktop macro alias")

recovery = text(V25 / "McpProjectRecoveryService.cs")
require(recovery, '"SAVETIME"', "BricsCAD autosave interval")
require(recovery, '"ISAVEBAK"', "BricsCAD BAK safety")
require(recovery, "MaxSnapshotsPerProject", "bounded recovery retention")
require(recovery, "RecoverLatestToCopy", "non-destructive restore-to-copy")
require(recovery, "FileShare.ReadWrite | FileShare.Delete", "live DWG read sharing")

# Public MCP transport must distinguish endpoint readiness from real OAuth client traffic.
embedded = text(V25 / "McpEmbeddedServerV2.cs")
require(embedded, "IsAllowedOrigin(request.Headers, publicMcpUrl)", "public-resource-aware MCP Origin validation")
require(embedded, "IsSameOriginAsPublicMcp", "exact validated public MCP Origin allowlist")
require(embedded, "LastOAuthMcpActivityUtc", "privacy-safe OAuth MCP activity timestamp")
require(embedded, "LastOAuthMcpMethod", "privacy-safe OAuth MCP activity method")
require(embedded, "LastOAuthMcpPublicUrl", "OAuth MCP activity resource binding")
require(embedded, "RecordOAuthMcpActivity", "OAuth MCP activity recorder")
require(embedded, "out bool oauthAccessToken", "legacy-bearer versus OAuth authorization distinction")

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
require(ui, "AUTO-RENEW", "desktop consent auto-renew status")
require(ui, "không còn giới hạn idle 10 phút", "desktop auto-renew guidance")
require(ui, "Action ID", "desktop action-id display")
require(ui, "Kiểm tra drawing/backup", "post-stop recovery guidance")
require(ui, '"Transport sẵn sàng"', "transport readiness status distinct from ChatGPT connectivity")
require(ui, '"ChatGPT đăng ký"', "ChatGPT registration acknowledgement status")
require(ui, '"OAuth MCP traffic"', "observed OAuth MCP traffic status")
require(ui, "HasRecentOAuthMcpActivity", "live OAuth MCP traffic calculation")
forbid(ui, "Consent tự hết hạn sau 10 phút", "stale desktop idle-expiry guidance")
forbid(ui, "Idle timeout 10 phút", "stale desktop resume timeout guidance")
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

print("PASS MCP guided onboarding + desktop Completion Packs A/B + recovery + connectivity source contract")
