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
            "\"backgroundControl\"",
            "\"foregroundControl\"",
            "\"defaultRoute\":\"background\"",
            "\"fallback\":\"explicit_only\"",
            "\"implicitForegroundFallback\":false",
            "BelongsToCurrentProcess",
            "SendMessageTimeout",
        ],
        errors,
    )

    augmenter = require_tokens(
        AUGMENTER_PATH,
        [
            "Thao tác nền · Background Control",
            "Thao tác trực tiếp · Foreground Control",
            "ưu tiên mặc định",
            "không tự chuyển sang thao tác trực tiếp",
            "McpBackgroundHostRuntime.EnableForegroundFromLocalUser()",
            "McpBackgroundHostRuntime.DisableForegroundFromLocalUser()",
            "McpBackgroundHostRuntime.IsForegroundAvailable",
            "Foreground Control · chuột / bàn phím / màn hình user",
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

    if "TrySetInteractionPolicy(\"foreground_fallback\")" in augmenter:
        errors.append(
            f"{AUGMENTER_PATH.relative_to(ROOT)} must use the local foreground helper instead of a loopback MCP policy mutation"
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
