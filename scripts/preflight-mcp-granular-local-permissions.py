#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"
AUGMENTER = SRC / "McpPersistentAgentCenterAugmenter.cs"
SESSION = SRC / "McpDesktopControlSession.cs"
BACKGROUND = SRC / "McpBackgroundHostRuntime.cs"
SETTINGS = SRC / "McpPersistentUserSettings.cs"
INSTALLER = SRC / "Updates" / "VerifiedPreviewInstaller.cs"
RUNBOOK = ROOT / "docs" / "FEATURE-RUNBOOKS" / "mcp-local-control-permission-ui.md"


def fail(message: str) -> None:
    print(f"ERROR: MCP local-control permission UI preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


for path in (AUGMENTER, SESSION, BACKGROUND, SETTINGS, INSTALLER, RUNBOOK):
    if not path.is_file():
        fail("missing required file: " + str(path.relative_to(ROOT)))

augmenter = AUGMENTER.read_text(encoding="utf-8")
session = SESSION.read_text(encoding="utf-8")
background = BACKGROUND.read_text(encoding="utf-8")
settings = SETTINGS.read_text(encoding="utf-8")
installer = INSTALLER.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

# Agent Center must expose the two real authority layers as checkboxes, not a coarse action button.
for token in (
    "PermissionPanelTag",
    "BackgroundModeCheckBoxTag",
    "DesktopForegroundToggleTag",
    "new CheckBox",
    "MCP chạy nền BricsCAD/API (không chiếm chuột/phím): BẬT",
    "Cho phép chuột / bàn phím / màn hình user",
    "Background là đường mặc định; foreground chỉ dùng khi thật sự cần thao tác desktop.",
    "RefreshDesktopPermissionPanel",
    "ToggleDesktopForegroundAccess",
    "FindTaggedCheckBox",
):
    if token not in augmenter:
        fail("Agent Center permission UI missing contract: " + token)

if "CloneActionButton(resumeButton" in augmenter:
    fail("foreground permission must no longer be rendered as a cloned action button")

# Background remains the safe/default path and foreground remains a strictly local explicit fallback.
for token in (
    "private static int _interactionPolicy = BackgroundOnly;",
    'McpDesktopControlSession.RequireLocalConsent("foreground-fallback-enable")',
):
    if token not in background:
        fail("background/foreground policy regression: " + token)

for token in (
    "DisableForegroundAccessFromLocalUser",
    'StopSession(reason, false, false, "OFF")',
    "ResumeFromLocalUser",
):
    if token not in session:
        fail("local desktop-consent regression: " + token)

# UI enable/disable still has to drive the same fail-closed policy + consent implementation.
toggle_block = augmenter.split("private static void ToggleDesktopForegroundAccess()", 1)[1].split("private static void TrySetInteractionPolicy", 1)[0]
for token in (
    'TrySetInteractionPolicy("background_only")',
    'TrySetInteractionPolicy("foreground_fallback")',
    "McpDesktopControlSession.ResumeFromLocalUser()",
    "McpDesktopControlSession.DisableForegroundAccessFromLocalUser",
    "McpAgentExperience.Error(",
):
    if token not in toggle_block:
        fail("foreground checkbox path missing fail-closed contract: " + token)
if "throw;" in toggle_block:
    fail("foreground checkbox failure must not rethrow into the WPF dispatcher")

# Runtime API key must remain durable and verified before process publication.
for token in (
    "WriteCredential(OpenAiRuntimeKeyTarget, secret);",
    "TryReadOpenAiRuntimeApiKey(out persisted)",
    "string.Equals(persisted, secret, StringComparison.Ordinal)",
    'Environment.SetEnvironmentVariable("CONTROL_PLANE_API_KEY", secret, EnvironmentVariableTarget.Process);',
):
    if token not in settings:
        fail("Runtime API-key persistence regression: " + token)

# Preview updater must remain unable to overwrite/delete credential surfaces.
for forbidden in (
    "mcp-bearer-token.txt",
    "QS3D.BricsCAD.MCP.OpenAI.RuntimeApiKey",
    "CredDelete",
    "CONTROL_PLANE_API_KEY",
):
    if forbidden in installer:
        fail("preview updater must not touch MCP credential surface: " + forbidden)

# Feature runbook must match merged credential truth and explain the two permission layers.
for phrase in (
    "Windows Credential Manager",
    "exact read-back verification",
    "không ghi plaintext",
    "background_only",
    "foreground_fallback",
    "checkbox",
    "chuột / bàn phím / màn hình",
    "no ephemeral-process-token fallback",
):
    if phrase not in runbook:
        fail("MCP permission runbook missing current permission/credential truth: " + phrase)

print("MCP local-control permission UI preflight passed.")
