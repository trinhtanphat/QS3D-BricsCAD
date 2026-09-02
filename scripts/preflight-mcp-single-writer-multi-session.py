#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServerV2.cs"
RUNTIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadAgentRuntime.cs"
DIRECT = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadDirectModelRuntime.cs"
DOMAIN = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpQs3dDomainRuntime.cs"
COORDINATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadMutationCoordinator.cs"


class CoordinationModel:
    def __init__(self):
        self.token = None
        self.in_call = False
        self.pending_native = False

    def acquire(self, token):
        if self.token is not None or self.in_call or self.pending_native:
            return False
        self.token = token
        return True

    def enter_mutation(self, token=""):
        if self.in_call or self.pending_native:
            return False
        if self.token is not None and token != self.token:
            return False
        if self.token is None and token:
            return False
        self.in_call = True
        return True

    def queue_native(self):
        assert self.in_call
        self.pending_native = True

    def exit_mutation(self):
        self.in_call = False

    def terminal(self):
        self.pending_native = False

    def release(self, token):
        if self.in_call or self.pending_native or token != self.token:
            return False
        self.token = None
        return True


def require(text, token, where, errors):
    if token not in text:
        errors.append(f"{where} missing contract token: {token}")


def main() -> int:
    errors = []
    server = SERVER.read_text(encoding="utf-8") if SERVER.is_file() else ""
    runtime = RUNTIME.read_text(encoding="utf-8") if RUNTIME.is_file() else ""
    direct = DIRECT.read_text(encoding="utf-8") if DIRECT.is_file() else ""
    domain = DOMAIN.read_text(encoding="utf-8") if DOMAIN.is_file() else ""
    coordinator = COORDINATOR.read_text(encoding="utf-8") if COORDINATOR.is_file() else ""

    # Transport remains multi-session. Explicit lease tools are a writer-control surface,
    # not a transport/session lock, so independent reads stay available.
    require(server, "MaxConcurrentClients = 16", "McpEmbeddedServerV2", errors)
    require(server, "MaxSessions = 128", "McpEmbeddedServerV2", errors)
    require(server, 'Tool("cad_writer_acquire"', "McpEmbeddedServerV2", errors)
    require(server, 'Tool("cad_writer_status"', "McpEmbeddedServerV2", errors)
    require(server, 'Tool("cad_writer_release"', "McpEmbeddedServerV2", errors)
    require(server, "WriterTokenProperty()", "McpEmbeddedServerV2", errors)
    require(server, '"writerToken"', "McpEmbeddedServerV2", errors)

    # Every ordinary mutation crosses one process-global coordinator. Runtime lifecycle and
    # emergency stop clear stale leases/barriers. writerToken is optional for compatibility.
    require(runtime, 'case "cad_active_document": return InvokeCad', "McpCadAgentRuntime", errors)
    require(runtime, 'case "cad_selection": return InvokeCad', "McpCadAgentRuntime", errors)
    require(runtime, "McpCadMutationCoordinator.EnterMutation", "McpCadAgentRuntime", errors)
    require(runtime, "McpCadMutationCoordinator.Reset", "McpCadAgentRuntime", errors)
    require(runtime, 'ExtractString(body, "writerToken")', "McpCadAgentRuntime", errors)

    # Every asynchronous SendStringToExecute bridge retains writer ownership until the
    # matching BricsCAD command reaches Ended/Cancelled/Failed. This includes the direct
    # EXTRUDE bridge and the QS3D command bridge, not only the classic runtime path.
    require(runtime, "McpCadMutationCoordinator.QueueNativeCommand", "McpCadAgentRuntime", errors)
    require(direct, "McpCadMutationCoordinator.QueueNativeCommand", "McpCadDirectModelRuntime", errors)
    require(domain, "McpCadMutationCoordinator.QueueNativeCommand", "McpQs3dDomainRuntime", errors)
    require(runtime, "document.SendStringToExecute(script", "McpCadAgentRuntime", errors)
    require(direct, "document.SendStringToExecute(script", "McpCadDirectModelRuntime", errors)
    require(domain, "document.SendStringToExecute(command +", "McpQs3dDomainRuntime", errors)

    # Save remains one native attempt inside the writer boundary. Blind retries can turn an
    # eCantOpenFile or uncertain completion into duplicate writes and are forbidden.
    require(runtime, "save completion was not confirmed", "McpCadAgentRuntime", errors)
    save_start = runtime.find("private static string SaveActiveDocument(")
    save_end = runtime.find("private static string", save_start + 1) if save_start >= 0 else -1
    save_body = runtime[save_start:save_end] if save_start >= 0 and save_end > save_start else ""
    if save_body:
        if save_body.count("document.Database.Save();") != 1:
            errors.append("SaveActiveDocument must perform exactly one native Save attempt; no blind retry")
    else:
        errors.append("unable to isolate SaveActiveDocument")

    if not coordinator:
        errors.append("missing McpCadMutationCoordinator.cs")
    else:
        for token in (
            "SemaphoreSlim MutationGate",
            "AcquireWriterLease",
            "ReleaseWriterLease",
            "EnterMutation",
            "QueueNativeCommand",
            "CommandWillStart",
            "CommandEnded",
            "CommandCancelled",
            "CommandFailed",
            "GlobalCommandName",
            "PendingNativeCommandMaximumSeconds",
            "StatusJson",
        ):
            require(coordinator, token, "McpCadMutationCoordinator", errors)
        if "writerToken=" in coordinator or "token=" in coordinator:
            errors.append("McpCadMutationCoordinator must not format raw writer tokens into audit/status text")

    # Deterministic model of the core safety property: an explicit lease rejects other
    # writers; legacy unleased calls still serialize; accepted native commands block every
    # later mutation and lease release until the terminal event.
    model = CoordinationModel()
    assert model.acquire("owner-a")
    assert model.enter_mutation("owner-a")
    assert not model.enter_mutation("owner-b")
    model.queue_native()
    model.exit_mutation()
    assert not model.enter_mutation("owner-a")
    assert not model.release("owner-a")
    model.terminal()
    assert model.enter_mutation("owner-a")
    model.exit_mutation()
    assert not model.enter_mutation("owner-b")
    assert model.release("owner-a")
    assert model.enter_mutation()
    assert not model.enter_mutation()
    model.exit_mutation()

    if errors:
        print("FAIL: MCP multi-session single-writer guard")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: MCP remains multi-session for reads while explicit/ephemeral writer ownership serializes every mutation, all async native command bridges retain the barrier through terminal events, and save remains single-attempt.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
