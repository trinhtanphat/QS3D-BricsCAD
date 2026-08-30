# Remote-agent scope boundary

This file defines what remote/hosted/source-only agents may complete and what must be handed to a compatible local machine.

## Principle

`LOCAL_ONLY` describes the **remaining execution/evidence boundary**. It does not mean remote agents should stop repository-safe implementation early.

## Remote agents complete first

Before a task is handed to local execution, the remote/source owner must finish every repository-safe item it can prove from source, including:

- implementation/refactor work;
- deterministic tests and smoke coverage;
- static/source guards;
- documentation and narrow handoff updates;
- available remote-safe validation;
- coherent commit + push of the exact canonical branch/SHA.

The local worker should not have to reconstruct unfinished source work.

## LOCAL_ONLY examples

Execution/evidence is LOCAL_ONLY when it genuinely requires resources unavailable to the remote session, such as:

- licensed BricsCAD V25/V26 interactive runtime;
- native Windows UI/palette behavior;
- private/customer DWG fixtures;
- installed proprietary host dependencies not available remotely;
- real signing keys/certificates;
- machine-specific performance/hardware behavior.

## Canonical local queue

`docs/LOCAL-AGENT-INBOX.md` is the live queue for LOCAL_ONLY work.

When a new or materially changed local scenario is exposed:

1. finish all remote-safe work first;
2. push the exact candidate;
3. create/update the matching inbox item with exact SHA, prerequisites, narrow steps, expected result and minimum sanitized evidence;
4. stop retrying that unavailable runtime remotely.

Only a compatible local agent with real evidence tied to the exact tested SHA may record `LOCAL_PASS`.

## Mandatory remote inability handoff

A remote agent must not finish with only a chat note when irreducible local evidence remains.

For the same scenario/candidate:

- reuse the matching `docs/LOCAL-AGENT-INBOX.md` item and update that item instead of creating a duplicate;
- bind the handoff to the exact source SHA;
- record the required local evidence and keep it in the same batch as the source/docs change when that handoff was newly introduced or materially changed;
- treat the inbox as a do-not-repeat remote backlog once the scenario is classified `DO_NOT_RETRY_REMOTE`;
- never mark `PASS` from remote evidence; only compatible local execution may establish `LOCAL_PASS`.

## Merge interaction

A parked LOCAL_ONLY item does not block repository-safe completion or same-task merge unless that exact local evidence is explicitly part of the current task's acceptance criteria.

Do not turn every ordinary remote completion report into a repeated LOCAL_ONLY status dump.

## CI boundary

Automatic shared branch/PR validation follows `CI_POLICY.md`. It is not manual-only.

Remote agents may observe and remediate the automatic CI for their own carrier. Unrelated manual workflow dispatch/re-run/cancel and release operations remain separately controlled by `CI_POLICY.md`.

## Source-safe defects found locally

If local runtime exposes an ordinary source-safe defect:

1. local captures the smallest sanitized reproduction/evidence;
2. the defect returns to the canonical source lane;
3. remote/source agent fixes and pushes the new exact candidate;
4. local resumes the same runtime scenario on the new SHA.

Local implementation edits should be reserved for fixes whose reproduction/implementation/proof genuinely depends on the local proprietary environment.

## Status vocabulary

- `REMOTE_DONE` — all repository-safe work is complete/pushed for the exact candidate.
- `LOCAL_ONLY` — remaining execution/evidence requires local capability.
- `LOCAL_PASS` — verified only by compatible local execution on the exact SHA.
- `NOT QUALIFIED` — required local/engineering proof does not yet exist.

Remote/static evidence must never be promoted to `LOCAL_PASS`.