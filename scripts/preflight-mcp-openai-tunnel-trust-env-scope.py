#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpOpenAiSecureTunnel.cs"
DOC = ROOT / "docs" / "FEATURE-RUNBOOKS" / "mcp-openai-tunnel-trust-env-scope.md"


def fail(message: str) -> None:
    print(f"FAIL: OpenAI tunnel trust env-scope guard: {message}", file=sys.stderr)
    raise SystemExit(1)


def block(text: str, start_token: str, end_token: str) -> str:
    start = text.find(start_token)
    if start < 0:
        fail(f"missing source token: {start_token}")
    end = text.find(end_token, start + len(start_token))
    if end < 0:
        fail(f"missing source boundary: {end_token}")
    return text[start:end]


def require(text: str, token: str, message: str) -> None:
    if token not in text:
        fail(message)

def main() -> int:
    if not SRC.is_file():
        fail(f"missing source: {SRC.relative_to(ROOT)}")
    source = SRC.read_text(encoding="utf-8")

    resolver = block(
        source,
        "private static string ResolveExpectedSha256Pin()",
        "private static string NormalizeClientPath",
    )
    process_read = "Environment.GetEnvironmentVariable(ExpectedSha256Environment, EnvironmentVariableTarget.Process)"
    user_read = "Environment.GetEnvironmentVariable(ExpectedSha256Environment, EnvironmentVariableTarget.User)"
    machine_read = "Environment.GetEnvironmentVariable(ExpectedSha256Environment, EnvironmentVariableTarget.Machine)"
    for token, label in (
        (process_read, "Process"),
        (user_read, "User"),
        (machine_read, "Machine"),
    ):
        require(resolver, token, f"resolver missing {label}-scope SHA pin lookup")

    positions = [resolver.find(process_read), resolver.find(user_read), resolver.find(machine_read)]
    if not (positions[0] < positions[1] < positions[2]):
        fail("SHA pin precedence must be Process -> User -> Machine")

    trust = block(
        source,
        "private static bool TryVerifyClientTrust",
        "private static bool IsSupportedTunnelClientVersion",
    )
    require(trust, "var expected = ResolveExpectedSha256Pin();", "trust verifier must use the persistent SHA pin resolver")
    if "Environment.GetEnvironmentVariable(ExpectedSha256Environment)" in trust:
        fail("trust verifier still reads process-only SHA pin directly")
    require(trust, "Sha256Regex.IsMatch(expected)", "64-hex SHA validation must remain fail-closed")
    require(trust, "string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)", "exact SHA comparison must remain fail-closed")

    if not DOC.is_file():
        fail(f"missing runbook: {DOC.relative_to(ROOT)}")
    doc = DOC.read_text(encoding="utf-8")
    for token in ("Issue: `#6431`", "Process -> User -> Machine", "WinVerifyTrust", "LOCAL_ONLY"):
        require(doc, token, f"runbook missing contract token: {token}")

    print("PASS: OpenAI tunnel trust pin survives restart via Process -> User -> Machine lookup without weakening Authenticode/SHA fail-closed checks.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
