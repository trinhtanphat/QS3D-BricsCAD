# LOCAL_ONLY runtime ownership and reporting clarification

Owner clarification: remote/hosted/source-only agents must **not** repeatedly execute, re-audit, re-check, or routinely report licensed BricsCAD visual/runtime validation during normal `continue all`, broad review, source-fix, CI, PR, merge, or release passes.

The authoritative operating rules remain `AGENTS.md`, `docs/REMOTE-AGENT-SCOPE.md`, `docs/LOCAL-AGENT-INBOX.md`, and `docs/AGENT-PROMPT-TO-RELEASE-CONTRACT.md`. This note records the owner-facing reporting interpretation so future remote sessions do not turn LOCAL_ONLY evidence into recurring remote work or recurring status noise.

## Required behavior

- Licensed BricsCAD V25/V26 interactive runtime, real Windows UI/palette screenshots, private-DWG qualification, native host behavior, and other machine-only evidence are owned by compatible **local agents**.
- Owner-facing LOCAL/runtime status is reported by compatible **local-machine agents** when they are actually executing, awaiting, or reporting that local work.
- Once a LOCAL_ONLY scenario is parked in `docs/LOCAL-AGENT-INBOX.md`, remote agents treat it as `DO_NOT_RETRY_REMOTE` and continue source-safe work.
- Remote agents must not poll GitHub/source merely to discover whether that local runtime gate has since passed. A compatible local agent updates the inbox/evidence when it actually executes the scenario.
- Remote/hybrid/source-only completion reports **omit the LOCAL/runtime status line entirely by default**. Do not print `LOCAL_ONLY/PARKED`, `PENDING_LOCAL`, or a generic local-evidence reminder merely because a local scenario exists.
- A remote agent may mention a local gate only when the owner explicitly asks about local validation/status, or when that exact local evidence is an explicit current acceptance/blocking gate for the owner request.
- A LOCAL_ONLY gate does not block a remote `continue all` pass, branch/PR completion, or green-then-merge flow unless the specific task acceptance explicitly requires that local evidence before merge.
- Remote/static/CI evidence must never be promoted to `LOCAL_PASS`. Only a compatible local agent with real evidence tied to the exact tested SHA may record `LOCAL_PASS`.
- If a remote source change materially changes the local scenario, update the canonical local inbox item in the same task branch/PR, then stop remote execution and routine owner-facing reporting of that local gate again.

## Mandatory reporting-contract interpretation for remote agents

For remote/hybrid/source-only agents, this rule is the required exception to any generic `Local/runtime evidence` field in `AGENTS.md` or `docs/AGENT-PROMPT-TO-RELEASE-CONTRACT.md`:

- do **not** query, rerun, or re-audit a parked LOCAL_ONLY gate merely to fill a status field;
- do **not** emit a routine `Local/runtime`, `LOCAL_ONLY/PARKED`, or `PENDING_LOCAL` line in owner-facing reports;
- omit the local-runtime field entirely unless the owner explicitly asks for local status or the exact local gate is an explicit current blocker/acceptance requirement;
- do **not** use a parked local gate as the overall `Prompt result`, `Remaining blocker`, or reason to withhold an otherwise eligible remote PR/merge solely because the local gate remains open;
- if the local gate is explicitly task-gating and must be mentioned by a remote agent, state only the exact gate/blocker needed for the current request and do not expand the full local qualification matrix;
- a recorded `LOCAL_PASS` still requires real compatible-local evidence tied to the exact tested SHA and is normally reported by the compatible local agent.

This interpretation keeps `docs/REMOTE-AGENT-SCOPE.md` / `docs/LOCAL-AGENT-INBOX.md` as the execution boundary while assigning routine owner-facing local status to the agents that actually have the required environment.

## Local-agent reporting

Compatible local-machine agents should report the exact local evidence they actually executed, including the tested SHA and outcome required by the relevant runbook/inbox item. They may use `LOCAL_PASS`, `PENDING_LOCAL`, or an exact failure/blocker only when supported by real local execution state.

Remote agents do not echo that local status in unrelated routine reports after it has been handed off.

This clarification does not weaken any local qualification requirement; it only assigns execution and owner-facing local/runtime reporting to the agents that actually have the required environment.
