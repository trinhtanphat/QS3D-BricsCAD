#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
V25 = ROOT / "src" / "QS3D.BricsCAD.V25"
WATCHDOG = V25 / "McpEmbeddedServerWatchdog.cs"
ENTRY = V25 / "PluginEntry.cs"
BOOTSTRAP = V25 / "McpCloudflaredBootstrapper.cs"
SERVER = V25 / "McpEmbeddedServerV2.cs"


def require(text: str, token: str, label: str, errors: list[str]) -> None:
    if token not in text:
        errors.append(f"missing {label}: {token}")


def main() -> int:
    errors: list[str] = []
    for path in (WATCHDOG, ENTRY, BOOTSTRAP, SERVER):
        if not path.is_file():
            errors.append(f"missing file: {path.relative_to(ROOT)}")
    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    watchdog = WATCHDOG.read_text(encoding="utf-8")
    entry = ENTRY.read_text(encoding="utf-8")
    bootstrap = BOOTSTRAP.read_text(encoding="utf-8")
    server = SERVER.read_text(encoding="utf-8")

    require(watchdog, "McpEmbeddedServer.HealthEndpoint", "health endpoint probe", errors)
    require(watchdog, "FailuresBeforeRecovery = 2", "bounded recovery threshold", errors)
    require(watchdog, "McpEmbeddedServer.Stop();", "stop-before-restart", errors)
    require(watchdog, "McpEmbeddedServer.Start();", "listener restart", errors)
    require(entry, "McpEmbeddedServerWatchdog.Start();", "watchdog startup wiring", errors)
    require(entry, "McpEmbeddedServerWatchdog.Stop", "watchdog shutdown wiring", errors)
    require(server, "PreferredPort = 8765", "preferred port", errors)
    require(server, "SocketError.AddressAlreadyInUse", "address-in-use fallback", errors)
    require(bootstrap, "MaxDownloadAttempts = 3", "bounded download retries", errors)
    require(bootstrap, "SecurityProtocolType.Tls12", "TLS 1.2", errors)
    require(bootstrap, "AdoptExistingManagedBinary", "verified existing binary fallback", errors)
    require(bootstrap, "VerifyCloudflareBinary", "Authenticode verification", errors)

    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1
    print("PASS MCP lifecycle recovery + cloudflared installer resilience")
    return 0


if __name__ == "__main__":
    sys.exit(main())
