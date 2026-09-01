#!/usr/bin/env python3
"""Source preflight for explicit Background/Foreground BricsCAD control capabilities."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RUNTIME_PATH = ROOT / "src/QS3D.BricsCAD.V25/McpBackgroundHostRuntime.cs"
AUGMENTER_PATH = ROOT / "src/QS3D.BricsCAD.V25/McpPersistentAgentCenterAugmenter.cs"


def require_tokens(path: Path, tokens: list[str], errors: list[str]) -> str:
    text = path.read_text(encoding="utf-8")
    for token in tokens:
        if token not in text:
            errors.append(f"{path.relative_to(ROOT)} missing required token: {token}")
    return text


def main() -> int:
    errors: list[str] = []

    runtime = require_tokens(
        RUNTIME_PATH,
        [
            "BACKGROUND CONTROL:",
            "internal static bool IsForegroundPolicyEnabled",
            "internal static bool IsForegroundAvailable",
            "internal static void EnableForegroundFromLocalUser()",
            "internal static void DisableForegroundFromLocalUser()",
            "McpDesktopControlSession.RequireLocalConsent(\"foreground-local-enable\")",
            "McpDesktopControlSession.RequireLocalConsent(toolName ?? \"foreground-global-interaction\")",
            'case "desktop_screenshot":',
            'case "desktop_clipboard_read":',
            r'\"backgroundControl\"',
            r'\"foregroundControl\"',
            r'\"defaultRoute\":\"background\"',
            r'\"fallback\":\"explicit_only\"',
            r'\"implicitForegroundFallback\":false',
            "BelongsToCurrentProcess",
            "SendMessageTimeout",
        ],
        errors,
    )

    augmenter = require_tokens(
        AUGMENTER_PATH,
        [
            'private const string RuntimeKeyLabelPrefix = "Runtime API key";',
            "CreatePermissionCheckBox",
            "Background Control · BricsCAD/API trong nền: BẬT",
            "Foreground Control · chuột / bàn phím / màn hình user",
            "chỉ checkbox local này mới cấp quyền desktop trực tiếp",
            "Thao tác nền · Background Control",
            "Thao tác trực tiếp · Foreground Control",
            "ưu tiên mặc định",
            "không tự chuyển sang thao tác trực tiếp",
            "McpBackgroundHostRuntime.EnableForegroundFromLocalUser()",
            "McpBackgroundHostRuntime.DisableForegroundFromLocalUser()",
            "McpBackgroundHostRuntime.IsForegroundAvailable",
            "DisableForegroundAccessFromLocalUser",
        ],
        errors,
    )

    forbidden_runtime = [
        "McpDesktopAutomationRuntime.Call(",
        "SendInput(",
        "SetCursorPos(",
        "SetForegroundWindow(",
        "Process.Start(",
        "CreateProcess(",
        "cmd.exe",
        "powershell",
        "pwsh",
    ]
    for token in forbidden_runtime:
        if token in runtime:
            errors.append(f"{RUNTIME_PATH.relative_to(ROOT)} contains forbidden background-control token: {token}")

    set_policy_start = runtime.find("private static string SetPolicy(")
    text_snapshot_start = runtime.find("private static string TextSnapshot(", set_policy_start + 1)
    set_policy = runtime[set_policy_start:text_snapshot_start] if set_policy_start >= 0 and text_snapshot_start > set_policy_start else ""
    for token in (
        'if (mode == "foreground_fallback")',
        'throw new InvalidOperationException("Foreground Control can only be enabled by the local Agent Center checkbox.");',
    ):
        if token not in set_policy:
            errors.append("remote interaction-policy setter must reject foreground enable and preserve local-checkbox-only grant: " + token)
    for token in (
        'McpDesktopControlSession.RequireLocalConsent("foreground-fallback-enable")',
        "Interlocked.Exchange(ref _interactionPolicy, ForegroundFallback);",
    ):
        if token in set_policy:
            errors.append("remote interaction-policy setter must not arm Foreground Control: " + token)

    forbidden_augmenter = [
        'TrySetInteractionPolicy("foreground_fallback")',
        "WireResumeForegroundSync",
    ]
    for token in forbidden_augmenter:
        if token in augmenter:
            errors.append(
                f"{AUGMENTER_PATH.relative_to(ROOT)} contains forbidden implicit-foreground token: {token}"
            )

    if errors:
        print("FAIL: MCP dual-control capability guard")
        for error in errors:
            print(f" - {error}")
        return 1

    print("PASS: MCP dual-control capability guard")
    return 0


if __name__ == "__main__":
    sys.exit(main())
