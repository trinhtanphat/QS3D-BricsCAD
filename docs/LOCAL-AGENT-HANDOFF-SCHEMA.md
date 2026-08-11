# QS3D local-agent handoff schema

**Updated:** 2026-08-11 (UTC+7)

This file is a **schema/contract only**. It is not a second work queue. `docs/LOCAL-AGENT-INBOX.md` remains the single live queue for LOCAL_ONLY work.

## Mandatory rule

When a remote/non-local agent cannot complete, execute, reproduce, validate, or prove a task because the required local machine, licensed BricsCAD V25 runtime, Windows UI, private/customer DWG, signing credential, installed proprietary dependency, hardware, secret, or other machine-only capability is unavailable, the agent must register the irreducible local residue in `docs/LOCAL-AGENT-INBOX.md` before ending the same source/docs batch.

A chat-only note, `NOT TESTED` sentence, speculative reminder, or retry by another equivalent remote agent is not a valid handoff. Remote agents must perform all source-safe implementation/tests/probes they can first, search the inbox for an existing matching item, update that item instead of creating a duplicate, and then continue other remote-safe work.

Every `OPEN`, `IN_PROGRESS`, or `BLOCKED` item is `DO_NOT_RETRY_REMOTE` unless current source materially changes the scenario, the owner explicitly requests a fresh source investigation, or the agent actually gains the missing local capability.

## Required format for new or materially changed handoffs

Every new inbox item, and every material update that changes what a local agent must prove, must preserve these fields:

```text
## LOCAL-NNN — concise title

- Priority: P0 | P1 | P2
- Status: OPEN | IN_PROGRESS | PASS | BLOCKED
- Source-side status: REMOTE_DONE | REMOTE_PARTIAL | NOT_STARTED
- Remote disposition: DO_NOT_RETRY_REMOTE
- Area: subsystem / command / workflow
- Why local: exact machine/runtime/resource requirement
- Blocker: exact reason a non-local agent cannot execute/prove the remaining work
- Source SHA: exact source/main SHA whose behavior must be qualified
- Scenario: minimum executable local steps / failure injection / probe
- Expected result: objective pass condition
- Evidence required: exact artifacts/measurements/logs/state needed to prove PASS
- Evidence: PENDING_LOCAL | sanitized evidence tied to the exact tested SHA
- Related source/docs: source files, commands, scripts, issue/PR/commit, canonical runbook
- Updated: YYYY-MM-DD
```

Existing legacy inbox items do not need mechanical rewriting just to adopt this template. When an existing item is materially updated, add the missing fields that are relevant to the changed scenario rather than creating a duplicate item.

## Source-side completion boundary

`REMOTE_DONE` means the source/static contract is implemented and guarded as far as the remote environment can prove. It does not mean `LOCAL_PASS`.

`REMOTE_PARTIAL` means source-safe work remains and must be completed before handing only the irreducible native/runtime residue to local.

`NOT_STARTED` is allowed only when the required work itself is truly local-only. Do not use it to avoid repository work that can be implemented or statically validated remotely.

## Local PASS rule

Only a compatible local agent with the required environment may change an item to `PASS`. Evidence must identify the exact tested SHA and enough sanitized objective proof to reproduce the conclusion. Source review, static preflight, mock tests, remote builds, `-SkipRuntime`, or verbal confirmation cannot manufacture `LOCAL_PASS`.

Never commit proprietary BricsCAD DLLs, private/customer DWGs, signing keys, credentials, secrets, or unsanitized runtime captures.
