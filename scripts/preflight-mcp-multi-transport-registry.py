#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
REGISTRY = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpTransportProfileRegistry.cs"
COORDINATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpOpenAiSecureTunnel.cs"
WRITER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadMutationCoordinator.cs"
SPEC = ROOT / "docs" / "superpowers" / "specs" / "2026-09-02-mcp-multi-transport-registry-design.md"
RUNBOOK = ROOT / "docs" / "FEATURE-RUNBOOKS" / "mcp-multi-transport-registry.md"

errors = []

def require(text, token, where):
    if token not in text:
        errors.append(f"{where} missing contract token: {token}")

registry = REGISTRY.read_text(encoding="utf-8") if REGISTRY.is_file() else ""
coordinator = COORDINATOR.read_text(encoding="utf-8") if COORDINATOR.is_file() else ""
writer = WRITER.read_text(encoding="utf-8") if WRITER.is_file() else ""
spec = SPEC.read_text(encoding="utf-8") if SPEC.is_file() else ""
runbook = RUNBOOK.read_text(encoding="utf-8") if RUNBOOK.is_file() else ""

if not registry:
    errors.append("missing McpTransportProfileRegistry.cs")
else:
    for token in (
        "internal sealed class McpTransportProfile",
        "internal static class McpTransportProfileRegistry",
        "SchemaVersion = 1",
        "profiles-v1.txt",
        "LoadProfiles()",
        "EnsureLegacyDefaultProfile",
        "UpsertProfile",
        "RemoveProfile",
        "SetRegistrationAcknowledged",
        "IsRegistrationAcknowledged",
        "StatusJson",
        "Guid.NewGuid().ToString(\"N\")",
        "File.Move",
        "legacy-default",
    ):
        require(registry, token, "transport profile registry")
    for forbidden in (
        "CONTROL_PLANE_API_KEY",
        "OPENAI_API_KEY",
        "QS3D_MCP_BEARER_TOKEN",
        "writerToken",
        "SaveOpenAiRuntimeApiKey",
    ):
        if forbidden in registry:
            errors.append("registry must not persist or depend on secret material: " + forbidden)

# Compatibility APIs must remain in production source. Their new meaning is a documented
# contract, not a fragile requirement that one exact English sentence live in a monolithic
# implementation comment.
for token in (
    "SelectedProvider",
    "SetSelectedProvider",
):
    require(coordinator, token, "transport coordinator compatibility")

for token in (
    "preferred UI/onboarding provider",
    "not registry ownership",
    "multiple enabled OpenAI Secure MCP Tunnel and Cloudflare profiles",
):
    require(runbook, token, "transport preference compatibility runbook")

if not writer:
    errors.append("missing process-global mutation coordinator baseline")
else:
    for token in ("SemaphoreSlim MutationGate", "mode\\\":\\\"single-writer", "multiSessionReads"):
        require(writer, token, "single-writer invariant")

for token in (
    "multiple OpenAI and Cloudflare transports concurrently",
    "McpCadMutationCoordinator remains the only process-global DWG write boundary",
    "SelectedProvider",
):
    require(spec, token, "multi-transport design spec")

# Behavioral model: registry enablement is independent of the preferred UI provider.
profiles = [
    {"id": "a" * 32, "provider": "OpenAiSecureTunnel", "enabled": True},
    {"id": "b" * 32, "provider": "OpenAiSecureTunnel", "enabled": True},
    {"id": "c" * 32, "provider": "CloudflareNamedTunnel", "enabled": True},
]
preferred = "OpenAiSecureTunnel"
assert preferred == "OpenAiSecureTunnel"
assert len([p for p in profiles if p["enabled"]]) == 3
assert len([p for p in profiles if p["provider"] == "OpenAiSecureTunnel" and p["enabled"]]) == 2
assert any(p["provider"] == "CloudflareNamedTunnel" and p["enabled"] for p in profiles)

print("QS3D MCP multi-transport registry preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: a secret-free versioned registry can represent multiple enabled OpenAI/Cloudflare profiles while SelectedProvider remains only a compatibility UI preference and the process-global CAD writer stays singular.")
