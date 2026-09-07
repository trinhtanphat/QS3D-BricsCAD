#!/usr/bin/env python3
"""Fail-closed source guard for issue #6063 OpenAI tunnel supervisor warm-up stability."""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SUPERVISOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpTransportSupervisor.cs"
text = SUPERVISOR.read_text(encoding="utf-8")
errors = []


def require(haystack: str, needle: str, label: str) -> None:
    if needle not in haystack:
        errors.append(f"{label}: missing contract token {needle!r}")


def forbid(haystack: str, needle: str, label: str) -> None:
    if needle in haystack:
        errors.append(f"{label}: forbidden stale contract token {needle!r}")


for token in (
    "private const int OpenAiUnreadyProbeThreshold = 3;",
    "private static int _openAiUnreadyProbeCount;",
    "private static void PublishStartedProviderState(",
    "private static bool TryFailoverIfBudgetExhausted(",
):
    require(text, token, "OpenAI supervisor warm-up")

run = re.search(
    r"private static void RunOneIteration\(string reason\)\s*\{(?P<body>.*?)\n        \}\n\n        private static void PublishStartedProviderState",
    text,
    re.DOTALL,
)
if not run:
    errors.append("RunOneIteration: unable to isolate supervisor iteration body")
else:
    body = run.group("body")
    for token in (
        "active == McpTransportProvider.OpenAiSecureTunnel && IsProviderRunning(active)",
        "_openAiUnreadyProbeCount = Math.Min(_openAiUnreadyProbeCount + 1, OpenAiUnreadyProbeThreshold);",
        "if (_openAiUnreadyProbeCount < OpenAiUnreadyProbeThreshold)",
        "_health = McpTransportHealth.Starting;",
        "_restartCount = Math.Min(_restartCount + 1, 30);",
        "TryFailoverIfBudgetExhausted(active, attempt, reason, error)",
        "StopProvider(active);",
        "PublishStartedProviderState(active, false);",
    ):
        require(body, token, "RunOneIteration warm-up/restart")

    running_at = body.find("active == McpTransportProvider.OpenAiSecureTunnel && IsProviderRunning(active)")
    start_at = body.find("TryStartProvider(active, out error)")
    if running_at < 0 or start_at < 0 or running_at > start_at:
        errors.append("RunOneIteration: running-but-unready OpenAI admission must happen before provider restart")

    forbid(
        body,
        "_health = _failoverCount > 0 ? McpTransportHealth.FailedOver : McpTransportHealth.Ready;\n                    _restartCount = 0;",
        "RunOneIteration launch-is-ready regression",
    )

publish = re.search(
    r"private static void PublishStartedProviderState\(.*?\)\s*\{(?P<body>.*?)\n        \}\n\n        private static bool TryFailoverIfBudgetExhausted",
    text,
    re.DOTALL,
)
if not publish:
    errors.append("PublishStartedProviderState: missing health-gated start publication helper")
else:
    body = publish.group("body")
    require(body, "var healthy = IsProviderHealthy(provider);", "start publication")
    require(body, "_health = healthy", "start publication")
    require(body, "McpTransportHealth.Starting", "start publication")
    require(body, "if (healthy)", "start publication")
    require(body, "_restartCount = 0;", "start publication")
    require(body, "_openAiUnreadyProbeCount = 0;", "start publication")

failover = re.search(
    r"private static bool TryFailoverIfBudgetExhausted\(.*?\)\s*\{(?P<body>.*?)\n        \}\n\n        private static bool TryStartProvider",
    text,
    re.DOTALL,
)
if not failover:
    errors.append("TryFailoverIfBudgetExhausted: missing shared restart-budget/failover helper")
else:
    body = failover.group("body")
    require(body, "attempt < MaxRestartAttempts", "restart budget")
    require(body, "TryGetFallbackProvider(active, out fallback)", "restart budget")
    require(body, "PublishStartedProviderState(fallback, true);", "fallback readiness publication")

# The upstream tunnel-client contract uses /readyz. This issue fixes supervisor lifecycle churn,
# not the canonical readiness route; do not paper over the live symptom by changing probe semantics.
require(text, "McpOpenAiSecureTunnelManager.IsRunning && McpOpenAiSecureTunnelManager.IsReady", "OpenAI readiness proof")

if errors:
    print("MCP OpenAI supervisor warm-up preflight FAILED:")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("PASS: OpenAI tunnel warm-up is bounded before restart and launch alone cannot publish READY.")
print("NOTE: live OpenAI/ChatGPT tunnel qualification remains LOCAL_ONLY.")
