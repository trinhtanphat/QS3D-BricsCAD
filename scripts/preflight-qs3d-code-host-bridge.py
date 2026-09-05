#!/usr/bin/env python3
"""Fail closed unless the QS3D Code BricsCAD host bridge keeps strict local/native boundaries."""

from __future__ import annotations

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
V25 = ROOT / "src" / "QS3D.BricsCAD.V25"
CONTRACTS = V25 / "Qs3dCodeHostContracts.cs"
BRIDGE = V25 / "Qs3dCodeHostBridge.cs"
SERVICE = V25 / "Qs3dCodeHostService.cs"
IPC = V25 / "Qs3dCodeLocalIpcServer.cs"
PLUGIN = V25 / "PluginEntry.cs"
V26_PLUGIN = ROOT / "src" / "QS3D.BricsCAD.V26" / "PluginEntry.cs"


def fail(message: str) -> None:
    print("ERROR: " + message, file=sys.stderr)
    raise SystemExit(1)


def text(path: Path) -> str:
    if not path.is_file():
        fail(f"required QS3D Code host-bridge source is missing: {path.relative_to(ROOT)}")
    try:
        return path.read_text(encoding="utf-8")
    except (OSError, UnicodeError) as exc:
        fail(f"could not read {path.relative_to(ROOT)}: {type(exc).__name__}: {exc}")


def require(source: str, token: str, message: str) -> None:
    if token not in source:
        fail(message)


def reject(source: str, pattern: str, message: str) -> None:
    if re.search(pattern, source, flags=re.IGNORECASE | re.MULTILINE):
        fail(message)


def main() -> int:
    contracts = text(CONTRACTS)
    bridge = text(BRIDGE)
    service = text(SERVICE)
    ipc = text(IPC)
    plugin = text(PLUGIN)
    v26_plugin = text(V26_PLUGIN)

    # Typed, serializable boundary: identity + operation ids only; never leak live native objects.
    for token in ("Qs3dCodeHostIdentity", "Qs3dCodeDocumentIdentity", "Qs3dCodeHostRequest", "OperationId", "PermissionClass"):
        require(contracts, token, f"host contracts lost required typed boundary token: {token}")
    reject(contracts, r"\b(DBObject|ObjectId|Transaction|Database|Document)\b", "serializable host contracts must not expose live BricsCAD/Teigha objects")

    # Every execution re-resolves the active host/document and refuses stale caller identity.
    require(bridge, "Application.DocumentManager.MdiActiveDocument", "host bridge must resolve the active document at execution time")
    require(bridge, "RejectStale", "host bridge must have an explicit stale host/document rejection path")
    require(bridge, "McpCadMutationCoordinator.EnterMutation", "host bridge mutations must reuse the existing process-global CAD writer admission")
    reject(bridge, r"new\s+(Mutex|SemaphoreSlim)\s*\(", "host bridge must not create a second independent mutation authority")

    # Emergency stop must be shared with the established runtime boundary.
    require(service, "McpCadAgentRuntime.StopAutomation", "QS3D Code emergency stop must reuse established CAD automation stop semantics")
    require(service, "McpCadAgentRuntime.AutomationStopped", "host service must observe established emergency-stop state")

    # IPC is local-only. Named pipes are admitted; TCP/HTTP listeners are not.
    require(ipc, "NamedPipeServerStream", "local IPC must use a user-local named pipe")
    require(ipc, "PipeOptions", "named-pipe creation must use explicit options")
    require(ipc, "capability", "local IPC must authenticate requests with capability/session state")
    reject(ipc, r"\b(TcpListener|HttpListener|Socket\s*\(|IPEndPoint|IPAddress\.Any)\b", "QS3D Code local IPC must never open a public/network listener")
    reject(ipc, r"Console\.(Write|WriteLine).*capabil", "capability/session tokens must never be logged")

    # Startup and teardown are symmetrical and fail-soft like the other optional host services.
    require(plugin, "Qs3dCodeHostService.Start", "V25 PluginEntry must start the QS3D Code host service")
    require(plugin, "Qs3dCodeHostService.Stop", "V25 PluginEntry must stop the QS3D Code host service")
    if plugin.index("Qs3dCodeHostService.Stop") > plugin.index("DocumentLifecycleCoordinator.Stop"):
        fail("QS3D Code host service must stop before document lifecycle teardown")

    # V26 may have a host-major entry point or consume the shared V25 entry point; either way the source must make the lifecycle explicit.
    if "Qs3dCodeHostService.Start" not in v26_plugin and "QS3D.BricsCAD.V25.PluginEntry" not in v26_plugin:
        fail("V26 PluginEntry lost explicit/shared QS3D Code host-service startup wiring")
    if "Qs3dCodeHostService.Stop" not in v26_plugin and "QS3D.BricsCAD.V25.PluginEntry" not in v26_plugin:
        fail("V26 PluginEntry lost explicit/shared QS3D Code host-service teardown wiring")

    print("PASS QS3D Code typed BricsCAD host bridge / local IPC source contract")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
