#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServerV2.cs"
RUNTIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadAgentRuntime.cs"
COORDINATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadMutationCoordinator.cs"


class CoordinationModel:
    def __init__(self):
        self.token = None
        self.pending = False
        self.in_call = False

    def acquire(self, token):
        if self.token is not None or self.pending:
            return False
        self.token = token
        return True

    def enter(self, token=""):
        if self.in_call or self.pending:
            return False
        if self.token is not None and token != self.token:
            return False
        if self.token is None and token:
            return False
        self.in_call = True
        return True

    def queue_native(self):
        assert self.in_call
        self.pending = True

    def exit(self):
        self.in_call = False

    def terminal(self):
        self.pending = False

    def release(self, token):
        if self.pending or token != self.token:
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
    coordinator = COORDINATOR.read_text(encoding="utf-8") if COORDINATOR.is_file() else ""

    require(server, "MaxConcurrentClients = 16", "McpEmbeddedServerV2", errors)
    require(server, "MaxSessions = 128", "McpEmbeddedServerV2", errors)
    require(server, 'Tool("cad_writer_acquire"', "McpEmbeddedServerV2", errors)
    require(server, 'Tool("cad_writer_status"', "McpEmbeddedServerV2", errors)
    require(server, 'Tool("cad_writer_release"', "McpEmbeddedServerV2", errors)
    require(server, "WriterTokenProperty()", "McpEmbeddedServerV2", errors)
    require(server, '\"writerToken\"', "McpEmbeddedServerV2", errors)
    require(server, "AcquireWriterLease", "McpEmbeddedServerV2", errors)
    require(server, "ReleaseWriterLease", "McpEmbeddedServerV2", errors)

    require(runtime, "McpCadMutationCoordinator.EnterMutation", "McpCadAgentRuntime", errors)
    require(runtime, "McpCadMutationCoordinator.QueueNativeCommand", "McpCadAgentRuntime", errors)
    require(runtime, "McpCadMutationCoordinator.Reset", "McpCadAgentRuntime", errors)
    require(runtime, "writerToken", "McpCadAgentRuntime", errors)
    require(runtime, "save completion was not confirmed", "McpCadAgentRuntime", errors)

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
            "StatusJson",
        ):
            require(coordinator, token, "McpCadMutationCoordinator", errors)
        if "writerToken=" in coordinator or "token=" in coordinator:
            errors.append("McpCadMutationCoordinator must not format raw writer tokens into audit/status text")

    save_start = runtime.find("private static string SaveActiveDocument(")
    save_end = runtime.find("private static string", save_start + 1) if save_start >= 0 else -1
    save_body = runtime[save_start:save_end] if save_start >= 0 and save_end > save_start else ""
    if save_body:
        if save_body.count("document.Database.Save();") != 1:
            errors.append("SaveActiveDocument must perform exactly one native Save attempt; no blind retry")
    else:
        errors.append("unable to isolate SaveActiveDocument")

    model = CoordinationModel()
    assert model.acquire("owner-a")
    assert model.enter("owner-a")
    model.queue_native()
    model.exit()
    assert not model.enter("owner-a")
    assert not model.enter("owner-b")
    assert not model.release("owner-a")
    model.terminal()
    assert model.enter("owner-a")
    model.exit()
    assert not model.enter("owner-b")
    assert model.release("owner-a")
    assert model.enter()
    model.exit()

    if errors:
        print("FAIL: MCP multi-session single-writer guard")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: MCP remains multi-session for reads while DWG mutations use one explicit/ephemeral writer gate, async native commands retain the barrier, and save is single-attempt.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
