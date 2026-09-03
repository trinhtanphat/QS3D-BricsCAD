#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServerV2.cs"


def fail(message: str) -> None:
    print(f"ERROR: MCP listener-generation preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


server = SERVER.read_text(encoding="utf-8")

requirements = {
    "captured listener thread": "new Thread(() => ServeLoop(listener))",
    "listener-bound loop signature": "private static void ServeLoop(TcpListener listener)",
    "ownership helper": "OwnsListener(listener)",
    "exact listener ownership": "ReferenceEquals(_listener, listener)",
    "bounded stop join": "thread.Join(1000)",
    "exact accept target": "client = listener.AcceptTcpClient();",
}
for label, token in requirements.items():
    if token not in server:
        fail(f"missing {label}: {token}")

start = server.find("private static void ServeLoop(TcpListener listener)")
end = server.find("private static void HandleClient", start)
if start < 0 or end < 0:
    fail("could not isolate ServeLoop")
serve_loop = server[start:end]
if "var listener = _listener;" in serve_loop:
    fail("ServeLoop must not re-read the replaceable global listener")
if serve_loop.count("OwnsListener(listener)") < 3:
    fail("ServeLoop must validate exact listener ownership before accept/retry failure paths")
if "Thread.Abort" in server:
    fail("listener shutdown must not use forceful thread abort")

for preserved in (
    "new TcpListener(IPAddress.Loopback, port)",
    "MaxConcurrentClients = 16",
    "MaxHeaderBytes = 64 * 1024",
    "MaxBodyBytes = 1024 * 1024",
):
    if preserved not in server:
        fail(f"existing transport bound changed or disappeared: {preserved}")

print("MCP listener-generation preflight passed; each ServeLoop is pinned to its exact owned listener.")
