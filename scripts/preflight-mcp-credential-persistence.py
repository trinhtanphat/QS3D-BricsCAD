#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(rel: str) -> str:
    path = ROOT / rel
    if not path.is_file():
        raise SystemExit(f"FAIL: missing credential-persistence source: {rel}")
    return path.read_text(encoding="utf-8")


def require(text: str, needle: str, rel: str) -> None:
    if needle not in text:
        raise SystemExit(f"FAIL: {rel} missing credential-persistence contract: {needle}")


def forbid(text: str, needle: str, rel: str) -> None:
    if needle in text:
        raise SystemExit(f"FAIL: {rel} contains forbidden credential-persistence behavior: {needle}")


def main() -> int:
    server_rel = "src/QS3D.BricsCAD.V25/McpEmbeddedServerV2.cs"
    settings_rel = "src/QS3D.BricsCAD.V25/McpPersistentUserSettings.cs"
    augmenter_rel = "src/QS3D.BricsCAD.V25/McpPersistentAgentCenterAugmenter.cs"
    installer_rel = "src/QS3D.BricsCAD.V25/Updates/VerifiedPreviewInstaller.cs"

    server = read(server_rel)
    settings = read(settings_rel)
    augmenter = read(augmenter_rel)
    installer = read(installer_rel)

    # Active MCP bearer: publish only after durable same-directory write + read-back verification.
    for needle in (
        "private static void PersistBearerTokenAtomically(string path, string token)",
        "FileOptions.WriteThrough",
        "File.Replace(tempPath, path, null, true)",
        "File.Move(tempPath, path)",
        "var verified = File.ReadAllText(path, Encoding.UTF8).Trim();",
        "if (!ConstantTimeEquals(verified, token))",
        "PersistBearerTokenAtomically(path, generated);",
        "_bearerToken = generated;",
        '_tokenSource = "generated verified token file";',
    ):
        require(server, needle, server_rel)
    forbid(server, '"ephemeral process token"', server_rel)

    persist_call = server.find("PersistBearerTokenAtomically(path, generated);")
    publish_call = server.find("_bearerToken = generated;")
    if min(persist_call, publish_call) < 0 or persist_call >= publish_call:
        raise SystemExit("FAIL: active MCP bearer must be durably persisted/verified before publication")

    # OpenAI Runtime API key: CredWrite is not enough; exact re-read verification precedes env publication.
    for needle in (
        "WriteCredential(OpenAiRuntimeKeyTarget, secret);",
        "string persisted;",
        "if (!TryReadOpenAiRuntimeApiKey(out persisted)",
        "!string.Equals(persisted, secret, StringComparison.Ordinal)",
        "Runtime API key persistence verification failed.",
        'Environment.SetEnvironmentVariable("CONTROL_PLANE_API_KEY", secret, EnvironmentVariableTarget.Process);',
    ):
        require(settings, needle, settings_rel)

    write_index = settings.find("WriteCredential(OpenAiRuntimeKeyTarget, secret);")
    verify_index = settings.find("if (!TryReadOpenAiRuntimeApiKey(out persisted)")
    env_index = settings.find('Environment.SetEnvironmentVariable("CONTROL_PLANE_API_KEY", secret, EnvironmentVariableTarget.Process);')
    if min(write_index, verify_index, env_index) < 0 or not (write_index < verify_index < env_index):
        raise SystemExit("FAIL: Runtime API key must be written, re-read verified, then published to process environment")

    forbid(augmenter, "Tunnel vẫn có thể chạy trong phiên hiện tại", augmenter_rel)
    require(augmenter, "Key mới không được dùng cho tunnel trong phiên này", augmenter_rel)

    # Preview apply may replace only the verified product payload; credentials live outside this surface.
    require(installer, 'new[] { "QS3D.BricsCAD.V25.dll", "QS3D.Core.dll" }', installer_rel)
    for forbidden in (
        "mcp-bearer-token.txt",
        "McpPersistentUserSettings",
        "CredDelete",
        "Credential Manager",
        "CONTROL_PLANE_API_KEY",
        "QS3D_MCP_BEARER_TOKEN",
    ):
        forbid(installer, forbidden, installer_rel)

    print("PASS: MCP bearer persistence is fail-closed and verified before publication; Runtime API keys are re-read verified before use; preview apply cannot overwrite credential state.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
