#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
EXPERIENCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpAgentExperience.cs"


def main() -> int:
    if not EXPERIENCE.is_file():
        print("ERROR: missing", EXPERIENCE.relative_to(ROOT))
        return 1

    text = EXPERIENCE.read_text(encoding="utf-8")
    errors: list[str] = []

    required = {
        "pending OAuth phase": "ChatGptOAuthTrafficPending",
        "OAuth activity freshness": "OAuthMcpActivityFreshness",
        "OAuth activity classifier": "IsRecentOAuthMcpActivity(publicUrl)",
        "registration before OAuth": "if (!registered)",
        "OAuth gate before pending": "if (!IsRecentOAuthMcpActivity(publicUrl))",
        "pending phase after registration": "return Snapshot(McpOnboardingPhase.ChatGptOAuthTrafficPending",
        "pending title": "Đã đăng ký · chờ ChatGPT OAuth traffic",
        "pending traffic explanation": "chưa quan sát authenticated OAuth MCP request",
        "ready requires recent OAuth": "return Snapshot(McpOnboardingPhase.Ready",
        "OAuth timestamp source": "McpEmbeddedServer.LastOAuthMcpActivityUtc",
        "OAuth public URL binding": "McpEmbeddedServer.LastOAuthMcpPublicUrl",
        "OAuth activity URL binding": "string.Equals(McpEmbeddedServer.LastOAuthMcpPublicUrl, publicUrl",
    }
    for label, token in required.items():
        if token not in text:
            errors.append(f"MCP connectivity onboarding state missing {label}: {token}")

    oauth_gate = text.find("if (!IsRecentOAuthMcpActivity(publicUrl))")
    pending_state = text.find("return Snapshot(McpOnboardingPhase.ChatGptOAuthTrafficPending")
    ready_state = text.find("return Snapshot(McpOnboardingPhase.Ready", pending_state + 1 if pending_state >= 0 else 0)
    if oauth_gate < 0 or pending_state < 0 or ready_state < 0 or not (oauth_gate < pending_state < ready_state):
        errors.append("MCP connectivity onboarding state must gate pending then Ready behind recent OAuth activity")

    forbidden = {
        "transport-only ready wording": "Embedded MCP + Named Tunnel + đăng ký ChatGPT đã được người dùng xác nhận.",
    }
    for label, token in forbidden.items():
        if token in text:
            errors.append(f"MCP connectivity onboarding state still contains forbidden {label}: {token}")

    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    print("PASS MCP connectivity onboarding state: transport != registration != live OAuth traffic")
    return 0


if __name__ == "__main__":
    sys.exit(main())
