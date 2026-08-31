#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpTransportAgentCenterAugmenter.cs"


def require(text, needle, description):
    if needle not in text:
        raise SystemExit("FAIL: " + description + " (missing: " + needle + ")")


def main():
    source = SOURCE.read_text(encoding="utf-8")

    require(
        source,
        'private const string PendingChatGptTunnelLabel = "ChatGPT Tunnel chưa xác nhận";',
        "the old ambiguous status must be recognized for compatibility",
    )
    require(
        source,
        'private const string WaitingChatGptTunnelTrafficLabel = "ChatGPT Tunnel · chờ MCP traffic";',
        "the waiting state must describe missing traffic rather than a broken tunnel",
    )
    require(
        source,
        "RefreshChatGptTunnelTrafficEvidence();",
        "the always-on transport augmenter must evaluate MCP traffic evidence",
    )
    require(
        source,
        'typeof(McpEmbeddedServer).GetField("Sessions", BindingFlags.NonPublic | BindingFlags.Static)',
        "auto-confirm must observe initialized embedded MCP sessions",
    )
    require(
        source,
        "provider == McpTransportProvider.OpenAiSecureTunnel",
        "auto-confirm must be scoped to the OpenAI Secure Tunnel provider",
    )
    require(
        source,
        "&& McpOpenAiSecureTunnelManager.IsRunning",
        "auto-confirm must require the OpenAI tunnel client to be running",
    )
    require(
        source,
        "if (!sawNewSession || McpTransportCoordinator.IsChatGptRegistrationAcknowledged()) return;",
        "readiness by itself must never acknowledge ChatGPT registration",
    )
    require(
        source,
        "McpTransportCoordinator.MarkChatGptRegistrationAcknowledged();",
        "a newly observed MCP session must persist the current tunnel acknowledgement",
    )
    require(
        source,
        "text.Text = WaitingChatGptTunnelTrafficLabel;",
        "Agent Center must replace the ambiguous pending label with a traffic-waiting label",
    )

    if "McpOpenAiSecureTunnelManager.IsReady" in source:
        raise SystemExit(
            "FAIL: Agent Center augmenter must not poll /readyz to auto-confirm ChatGPT traffic; "
            "a real initialized MCP session is required."
        )

    print("PASS: ChatGPT Tunnel waits for real initialized MCP traffic and auto-confirms only after a new session.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
