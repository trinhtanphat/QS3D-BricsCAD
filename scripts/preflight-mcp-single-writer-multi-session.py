#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServerV2.cs"
RUNTIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadAgentRuntime.cs"
DOMAIN = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpQs3dDomainRuntime.cs"
COORDINATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadMutationCoordinator.cs"


class CoordinationModel:
    def __init__(self):
        self.in_call = False
        self.pending_native = False

    def enter_mutation(self):
        if self.in_call or self.pending_native:
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


def require(text, token, where, errors):
    if token not in text:
        errors.append(f"{where} missing contract token: {token}")


def main() -> int:
    errors = []
    server = SERVER.read_text(encoding="utf-8") if SERVER.is_file() else ""
    runtime = RUNTIME.read_text(encoding="utf-8") if RUNTIME.is_file() else ""
    domain = DOMAIN.read_text(encoding="utf-8") if DOMAIN.is_file() else ""
    coordinator = COORDINATOR.read_text(encoding="utf-8") if COORDINATOR.is_file() else ""

    # Transport remains multi-session; write serialization belongs below transport so
    # independent read-only calls stay available while one writer owns CAD mutation state.
    require(server, "MaxConcurrentClients = 16", "McpEmbeddedServerV2", errors)
    require(server, "MaxSessions = 128", "McpEmbeddedServerV2", errors)
    require(runtime, 'case "cad_active_document": return InvokeCad', "McpCadAgentRuntime", errors)
    require(runtime, 'case "cad_selection": return InvokeCad', "McpCadAgentRuntime", errors)

    # All ordinary mutations cross one process-global coordinator at the common Mutation
    # boundary. Server lifecycle/emergency stop must clear stale writer/barrier state.
    require(runtime, "McpCadMutationCoordinator.EnterMutation", "McpCadAgentRuntime", errors)
    require(runtime, "McpCadMutationCoordinator.Reset", "McpCadAgentRuntime", errors)

    # Both asynchronous SendStringToExecute bridges must retain writer ownership until the
    # matching BricsCAD command reaches Ended/Cancelled/Failed.
    require(runtime, "McpCadMutationCoordinator.QueueNativeCommand", "McpCadAgentRuntime", errors)
    require(domain, "McpCadMutationCoordinator.QueueNativeCommand", "McpQs3dDomainRuntime", errors)
    require(runtime, "document.SendStringToExecute(script", "McpCadAgentRuntime", errors)
    require(domain, "document.SendStringToExecute(command +", "McpQs3dDomainRuntime", errors)

    # Save remains one native attempt inside the mutation boundary. Blind retries can turn
    # eCantOpenFile / uncertain completion into duplicate writes and are forbidden.
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

    # Deterministic model of the core safety property: only one mutation at a time, and an
    # accepted native command blocks every later mutation until its terminal event.
    model = CoordinationModel()
    assert model.enter_mutation()
    assert not model.enter_mutation()
    model.exit_mutation()
    assert model.enter_mutation()
    model.queue_native()
    model.exit_mutation()
    assert not model.enter_mutation()
    model.terminal()
    assert model.enter_mutation()
    model.exit_mutation()

    if errors:
        print("FAIL: MCP multi-session single-writer guard")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: MCP remains multi-session for reads while every CAD mutation crosses one process-global writer gate, both async native command bridges retain the barrier through terminal events, and save remains single-attempt.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
