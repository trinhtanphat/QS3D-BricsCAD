#!/usr/bin/env python3
"""Focused source guard for OpenAI Secure MCP Tunnel runtime self-healing."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpOpenAiSecureTunnel.cs"
DOC = ROOT / "docs" / "FEATURE-RUNBOOKS" / "mcp-openai-tunnel-self-heal.md"


def block(text: str, signature: str, next_signature: str | None = None) -> str:
    start = text.find(signature)
    if start < 0:
        return ""
    if next_signature:
        end = text.find(next_signature, start + len(signature))
        if end > start:
            return text[start:end]
    return text[start:]


def main() -> int:
    errors: list[str] = []
    if not SRC.is_file():
        print(f"ERROR: missing {SRC.relative_to(ROOT)}")
        return 1

    source = SRC.read_text(encoding="utf-8")

    required = [
        "WatchdogPeriodMilliseconds",
        "UnreadyRestartThreshold",
        "RestartBackoffBaseSeconds",
        "RestartBackoffMaxSeconds",
        "Timer? _watchdogTimer",
        "int _watchdogBusy",
        "int _consecutiveUnready",
        "int _restartAttempt",
        "DateTime _nextRestartUtc",
        "EnsureWatchdogStarted()",
        "StopWatchdog()",
        "WatchdogTick",
        "ShouldWatchdogRun",
        "TryRecoverTunnel",
        "ComputeRestartBackoff",
        "Start(SavedTunnelId, string.Empty, out message)",
    ]
    for token in required:
        if token not in source:
            errors.append(f"missing tunnel self-heal token: {token}")

    start = block(source, "public static bool Start(", "public static void TryAutoStart()")
    if "EnsureWatchdogStarted();" not in start:
        errors.append("successful Start must arm the watchdog")

    stop = block(source, "public static void Stop()", "public static void StopForHostShutdown()")
    if "StopWatchdog();" not in stop:
        errors.append("explicit Stop must disable watchdog before stopping the child process")

    shutdown = block(source, "public static void StopForHostShutdown()", "public static void OpenPlatformTunnels()")
    if "StopWatchdog();" not in shutdown:
        errors.append("host shutdown must disable watchdog before stopping the child process")

    tick = block(source, "private static void WatchdogTick", "private static TimeSpan ComputeRestartBackoff")
    for token in (
        "Interlocked.CompareExchange(ref _watchdogBusy, 1, 0)",
        "ReadText(AutoStartFile) != \"1\"",
        "McpTransportCoordinator.SelectedProvider != McpTransportProvider.OpenAiSecureTunnel",
        "_consecutiveUnready < UnreadyRestartThreshold",
        "DateTime.UtcNow < _nextRestartUtc",
        "TryRecoverTunnel",
    ):
        if token not in tick:
            errors.append(f"watchdog must be bounded/fail-closed: {token}")

    recover = block(source, "private static void TryRecoverTunnel", "private static TimeSpan ComputeRestartBackoff")
    for token in (
        "StopProcessOnly();",
        "Start(SavedTunnelId, string.Empty, out message)",
        "_restartAttempt",
        "_nextRestartUtc",
    ):
        if token not in recover:
            errors.append(f"recovery must restart through verified saved configuration: {token}")

    if "Thread.Sleep(" in tick:
        errors.append("watchdog callback must not block a timer thread with Thread.Sleep")
    if "runtimeApiKey" in recover or "CONTROL_PLANE_API_KEY=" in recover:
        errors.append("watchdog recovery must not serialize or inject a raw key; Start must resolve saved/env credentials")

    if not DOC.is_file():
        errors.append(f"missing runbook: {DOC.relative_to(ROOT)}")
    else:
        doc = DOC.read_text(encoding="utf-8")
        for token in (
            "Lane-Key: `issue-5156`",
            "502",
            "connector_info",
            "bounded watchdog",
            "unexpected process exit",
            "persistent unready",
            "explicit Stop",
            "host shutdown",
            "Windows Credential Manager",
            "LOCAL_ONLY",
        ):
            if token not in doc:
                errors.append(f"runbook missing contract token: {token}")

    if errors:
        print("FAIL: OpenAI MCP tunnel self-heal guard")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: OpenAI MCP tunnel self-heal guard")
    return 0


if __name__ == "__main__":
    sys.exit(main())
