# LOCAL_ONLY runtime reporting clarification

Owner clarification: remote/hosted/source-only agents must **not** repeatedly execute, re-audit, or re-check licensed BricsCAD visual/runtime validation during normal `continue all`, broad review, source-fix, CI, PR, or merge passes.

The authoritative operating rules remain `AGENTS.md`, `docs/REMOTE-AGENT-SCOPE.md`, and `docs/LOCAL-AGENT-INBOX.md`. This note records the owner-facing reporting interpretation so future remote sessions do not turn LOCAL_ONLY evidence into recurring remote work.

## Required behavior

- Licensed BricsCAD V25/V26 interactive runtime, real Windows UI/palette screenshots, private-DWG qualification, native host behavior, and other machine-only evidence are owned by compatible **local agents**.
- Once a LOCAL_ONLY scenario is parked in `docs/LOCAL-AGENT-INBOX.md`, remote agents treat it as `DO_NOT_RETRY_REMOTE` and continue source-safe work.
- Remote agents must not poll GitHub/source merely to discover whether that local runtime gate has since passed. A compatible local agent updates the inbox/evidence when it actually executes the scenario.
- Remote completion reports should reference the parked LOCAL item once when relevant instead of repeating `PENDING_LOCAL` as an active remote blocker every turn.
- A LOCAL_ONLY gate does not block a remote `continue all` pass, branch/PR completion, or green-then-merge flow unless the specific task acceptance explicitly requires that local evidence before merge.
- Remote/static/CI evidence must never be promoted to `LOCAL_PASS`. Only a compatible local agent with real evidence tied to the exact tested SHA may record `LOCAL_PASS`.
- If a remote source change materially changes the local scenario, update the canonical local inbox item in the same task branch/PR, then stop remote execution of that local gate again.

## Mandatory reporting-contract interpretation for remote agents

For remote/hybrid/source-only agents, this rule is the required interpretation of the generic `Local/runtime evidence` field in `docs/AGENT-PROMPT-TO-RELEASE-CONTRACT.md`:

- do **not** query, rerun, or re-audit a parked LOCAL_ONLY gate merely to fill that field;
- render the field as `➖ Local/runtime: LOCAL_ONLY/PARKED — owned by local agents; not rechecked remotely` when the local gate is relevant but not part of the remote execution scope;
- do **not** use `PENDING_LOCAL` as the overall `Prompt result`, `Remaining blocker`, or reason to withhold an otherwise eligible remote PR/merge solely because a parked local gate remains open;
- use `PENDING_LOCAL` only when the owner/task explicitly makes that local evidence the current completion gate for this prompt, or when a compatible local agent is actually executing/awaiting that gate;
- a recorded `LOCAL_PASS` still requires real compatible-local evidence tied to the exact tested SHA.

This interpretation resolves the generic reporting template without weakening the local qualification requirement and keeps `docs/REMOTE-AGENT-SCOPE.md` / `docs/LOCAL-AGENT-INBOX.md` as the execution boundary.

## Reporting shorthand

For remote-agent reports, use this compact disposition when relevant:

```text
➖ Local/runtime: LOCAL_ONLY/PARKED — owned by local agents; not rechecked remotely.
```

Do not repeatedly expand the full licensed-runtime matrix in remote reports. When a specific `LOCAL-###` item exists, reference that identifier instead.

This clarification does not weaken any local qualification requirement; it only assigns execution and repeated status checking to the agents that actually have the required environment.
