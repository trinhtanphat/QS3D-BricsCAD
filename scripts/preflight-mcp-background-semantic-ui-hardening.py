#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"
BACKGROUND = SRC / "McpBackgroundHostRuntime.cs"
SEMANTIC = SRC / "McpBackgroundSemanticUiRuntime.cs"
AGENT = SRC / "McpCadAgentRuntime.cs"
DESKTOP = SRC / "McpDesktopAutomationRuntime.cs"
RUNBOOK = ROOT / "docs" / "FEATURE-RUNBOOKS" / "mcp-background-semantic-ui.md"


def fail(message: str) -> None:
    print(f"ERROR: MCP background semantic UI hardening preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


for path in (BACKGROUND, SEMANTIC, AGENT, DESKTOP, RUNBOOK):
    if not path.is_file():
        fail(f"missing {path.relative_to(ROOT)}")

background = BACKGROUND.read_text(encoding="utf-8")
semantic = SEMANTIC.read_text(encoding="utf-8")
agent = AGENT.read_text(encoding="utf-8")
desktop = DESKTOP.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

# Semantic mutations must keep using the repository-wide mutation coordinator rather than
# introducing a second local idempotency/locking protocol.
for token in (
    "McpMutationAckLedger.ReserveOrReplay",
    "McpCadMutationCoordinator.EnterMutation",
    "McpMutationAckLedger.MarkApplied",
    "if (writerScope == null) McpMutationAckLedger.Abandon(reservation.ActionId)",
):
    if token not in agent:
        fail(f"generic mutation wrapper missing hardening dependency: {token}")

# bricscad_ui_invoke is registered by the desktop automation router, then mutation calls flow
# through McpCadAgentRuntime.Mutation(). Guard the actual registration/routing source instead of
# requiring the tool literal to be duplicated in the generic mutation wrapper.
for token in (
    '"bricscad_ui_invoke"',
    "MutationTools",
    "McpBackgroundHostRuntime.Call",
):
    if token not in desktop:
        fail(f"desktop automation route missing hardening dependency: {token}")

if "expectedDiscoveryGeneration" not in background:
    fail("background semantic invoke schema must expose expectedDiscoveryGeneration")

semantic_requirements = {
    "fresh semantic discovery recording": "RecordSemanticDiscovery",
    "fresh semantic discovery requirement": "RequireFreshSemanticDiscovery",
    "semantic discovery invalidation": "InvalidateSemanticDiscovery",
    "discovery generation output": "discoveryGeneration",
    "expected discovery generation": "expectedDiscoveryGeneration",
    "active document capture": "ActiveDocumentSnapshot",
    "active document recheck": "RequireSameActiveDocument",
    "current BricsCAD document": "Application.DocumentManager.MdiActiveDocument",
    "same target UI thread rejection": "GetCurrentThreadId",
    "provider-completed outcome": "provider-completed",
    "uncertain outcome": "uncertain",
    "provider error reason": "provider-error",
    "postcondition divergence reason": "postcondition-diverged",
    "CAD-state disclaimer": "cadStateVerified",
    "retry prohibition": "retryAllowed",
    "rediscovery requirement": "requiresRediscovery",
}
for label, token in semantic_requirements.items():
    if token not in semantic:
        fail(f"semantic runtime missing {label}: {token}")

# The UIA provider call is not cancellable once entered. Every provider attempt must invalidate
# the semantic discovery generation so the caller cannot reuse a possibly stale elementPath.
execute_index = semantic.find("ExecuteAction(element, action)")
invalidate_index = semantic.find("InvalidateSemanticDiscovery")
if execute_index < 0 or invalidate_index < 0:
    fail("provider attempt/invalidation contract is missing")

# Raw provider exception detail is forbidden on the remote surface. Keep stable bounded reason
# codes and never propagate inner UIA exceptions/messages/stacks from this runtime.
for forbidden in (
    "ex.Message",
    "ex.ToString()",
    "StackTrace",
    ", ex);",
    ",ex);",
):
    if forbidden in semantic:
        fail(f"semantic runtime can leak provider exception details: {forbidden}")

for phrase in (
    "actionId",
    "ack ledger",
    "process-global writer",
    "fresh semantic discovery",
    "expectedDiscoveryGeneration",
    "same target UI thread",
    "active document",
    "provider-completed",
    "uncertain",
    "cadStateVerified=false",
    "retryAllowed=false",
    "requiresRediscovery=true",
    "no automatic retry",
    "LOCAL_ONLY",
):
    if phrase.lower() not in runbook.lower():
        fail(f"runbook missing hardening contract phrase: {phrase}")

# Guard against accidentally adding a competing semantic-only retry/idempotency ledger.
for forbidden_pattern in (
    r"class\s+SemanticMutationLedger",
    r"class\s+SemanticActionLedger",
    r"Dictionary<[^>]+>\s+_inFlightSemantic",
):
    if re.search(forbidden_pattern, semantic):
        fail("semantic runtime must reuse the canonical mutation ledger/writer instead of adding a competing ledger")

print("MCP background semantic UI hardening source contract passed.")
