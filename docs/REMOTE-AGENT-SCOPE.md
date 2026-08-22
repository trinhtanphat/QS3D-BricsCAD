# QS3D remote-agent scope boundary

Updated: 2026-08-21 (UTC+7)

This file is the **canonical scope boundary for remote / hosted / source-only agents**.

## Owner decision

Tasks that require a real BricsCAD V25 / Windows workstation are **LOCAL_ONLY**.

`docs/LOCAL-AGENT-INBOX.md` is the **single live priority/status queue** for those gates. Remote agents must register a new or materially changed LOCAL_ONLY scenario there in the same source/docs batch that introduced or exposed it. Longer local handoff documents are supporting runbooks/history, not competing queues.

Remote agents must not repeatedly re-audit, re-run, re-open or re-report these gates during normal `continue all`, broad source review, planning or implementation passes. Park them in the inbox and continue with source-safe work.

The purpose of this rule is to stop remote reviews from spending time rediscovering the same environment boundary and to keep runtime truth tied to the machine that can actually prove it.

## Hard remote-completion-before-local handoff rule

`LOCAL_ONLY` classifies the **remaining execution/evidence boundary**. It does **not** transfer ordinary repository-safe implementation to the local worker and it does **not** authorize a remote/source agent to stop coding early.

Before a LOCAL_ONLY scenario may be treated as ready for a local runner, the owning remote/source agent **MUST**:

1. finish every source-safe implementation, refactor, deterministic regression test, static guard, fixture, probe/script, documentation and handoff update that can be completed from repository source without the missing proprietary/local capability;
2. run every relevant remote-safe/static/deterministic validation available in its environment and fix any source-safe defect it exposes;
3. commit the completed remote-safe batch to the one canonical task branch and **push that exact branch/SHA to GitHub**; an uncommitted worktree, chat-only patch, stash, or local-only draft is not a valid handoff;
4. update the matching `docs/LOCAL-AGENT-INBOX.md` item in the same task branch/PR with the exact pushed source SHA, prerequisites, narrow executable scenario/commands, expected result and minimum sanitized evidence required, plus a concise summary of what remote work is already complete;
5. only after steps 1-4 are satisfied, park the remaining execution/evidence as LOCAL_ONLY and continue any other remote-safe work instead of consuming remote time retrying the unavailable runtime.

If GitHub push is genuinely unavailable, the item may be recorded as `BLOCKED` with the exact blocker, but the remote agent must not represent the code as ready for local execution until the canonical implementation is actually committed and pushed.

A compatible local agent is primarily a **sync -> run -> evidence** validator. It must first sync the exact pushed SHA named by the inbox item and must not redo already-completed remote-safe implementation merely because the runtime test is local. If local execution exposes a normal source-safe defect, capture the smallest sanitized reproduction/evidence and hand the defect back to the remote/source lane; after the remote fix is committed/pushed, local resumes validation on the new exact SHA. A local worker edits implementation code only when reproducing, implementing, or proving that fix genuinely depends on licensed/proprietary BricsCAD/Windows/private-DWG/UI/runtime capability unavailable to remote agents.

This invariant is mandatory even when older handoffs use softer wording such as `when possible`, `prepare if convenient`, or simply `needs local testing`.

## Mandatory remote inability handoff

Owner rule: **if a remote/hybrid agent cannot complete a task because the missing proof or execution requires a local machine, licensed BricsCAD V25, native Windows UI/runtime, private/customer DWG, installed proprietary dependency, local secret/certificate, or other machine-only capability, the agent must record that work in Markdown for a compatible local agent before ending the batch.**

The canonical destination is always `docs/LOCAL-AGENT-INBOX.md`.

Required behavior:

1. Search/read `docs/LOCAL-AGENT-INBOX.md` first. If an existing `LOCAL-###` already covers the same scenario, **update that item instead of creating a duplicate**.
2. If no item covers it, add the next `LOCAL-###` item with at least: `Priority`, `Status`, `Area`, `Why local`, `Scenario`, `Evidence required`, `Evidence`, `Related docs/files`, `Updated`, and the **exact source SHA** whose behavior must be qualified.
3. Complete and record all repository-safe/source-safe implementation, tests, guards, probes/scripts, docs and validation first, then commit and push the exact canonical branch/SHA so the local worker never has to reconstruct unfinished remote work.
4. Record what the remote agent already completed from source/static evidence so the local agent does not repeat remote-safe work. Keep the remaining local action narrow, executable and tied to the exact pushed SHA.
5. Mark the local item `OPEN` or `BLOCKED`; never mark `PASS` from remote evidence. `PASS` requires sanitized local evidence tied to the exact tested SHA.
6. Future remote agents must treat the parked `LOCAL-###` as **do-not-repeat remote backlog**. They may update it only when current source materially changes the required local scenario or when the owner explicitly asks for source-contract work around it.
7. In the remote completion report, reference the existing/new `LOCAL-###` rather than re-explaining and re-auditing the full local gate every turn.

This rule applies to **environment/capability blockers that a local agent can actually resolve**. Do not misclassify owner policy, engineering approval, legal/commercial decisions, or unsupported external-format scope as LOCAL_ONLY merely to hand them to a local agent; keep those under the repository's existing `POLICY_REQUIRED`, `ENGINEERING_REQUIRED`, `NEEDS_DECISION`, or `FORMAT_SCOPE_REQUIRED` boundaries.

A remote agent must not finish with only a chat note such as “needs local testing” when the local requirement is new or materially changed. The Markdown inbox update and exact pushed candidate are part of the same implementation/handoff batch.

## Current remote completion snapshot

Before creating another broad remote backlog, read `docs/REMOTE-IMPLEMENTATION-COMPLETION-2026-08-11.md`. It is the newest repository-level classification of the current source-safe implementation wave and explains which remaining gaps are `LOCAL_ONLY`, `POLICY_REQUIRED`, `ENGINEERING_REQUIRED` or `FORMAT_SCOPE_REQUIRED`.

That snapshot is not a frozen source of truth: always fetch current `main` first, reuse newer implementations that landed after its baseline, and update issue/local handoff status when newer source closes or narrows a documented gap. Do not use the completion snapshot as permission to stop fixing a concrete reproducible source defect; use it to avoid inventing work where the remaining correctness boundary genuinely depends on local runtime, owner policy, engineering approval or external format scope.

## LOCAL_ONLY — remote agents must skip

Unless the repository owner explicitly asks a remote agent to inspect the source contract around one of these areas, remote agents must skip execution/qualification of:

- BricsCAD V25 adapter compilation against an installed/licensed V25 environment when that exact local environment is required;
- interactive `NETLOAD`, DemandLoad and BricsCAD command execution;
- native V25 `Solid3d`, Boolean, transaction, DrawJig, Editor/UCS, save/reopen or multi-DWG runtime behavior;
- private/customer DWG regression;
- real Windows desktop Ribbon/Palette/WPF rendering, Unicode/HiDPI and machine-specific UI behavior;
- Windows installer/updater runtime integration on a clean machine;
- Authenticode signing with the real private certificate/key, certificate-chain trust and trusted timestamp proof;
- production package installation/trust evidence tied to a real workstation;
- measurements that require real V25 large-model performance profiling;
- any local-only evidence already assigned in `docs/LOCAL-AGENT-INBOX.md` or its linked local runbooks.

A remote agent must **not** block completion of its remote `continue all` pass merely because one of these LOCAL_ONLY gates is still pending. This skip applies to the unavailable runtime execution/evidence only; it never excuses unfinished source-safe implementation or an unpushed candidate.

## What remote agents should do instead

Remote agents should keep working on repository tasks they can prove from source, including:

- Core/domain/geometry/persistence/reporting implementation;
- ownership, rollback, fail-closed and transaction contracts visible in source;
- deterministic CAD-independent tests and smoke harnesses;
- static preflights and policy guards;
- adapter source changes whose correctness can be reviewed statically, while leaving native runtime proof LOCAL_ONLY;
- documentation/status reconciliation with current `main`;
- installer/updater/signing validators that do not require real signing secrets;
- preparation of exact local probes/scripts for a local agent to execute later.

If a source change introduces or changes a runtime contract, finish and validate all remote-safe work first, commit/push the exact candidate, then add/update the matching `docs/LOCAL-AGENT-INBOX.md` item with the **minimum exact local scenario and evidence needed in the same batch**. Do not execute or repeatedly re-audit the local scenario remotely, and do not leave the only current record in an older long-form handoff.

## Status vocabulary

Use these meanings consistently:

- `REMOTE_DONE` — the source/static contract requested from remote work is implemented, guarded, validated as far as the remote environment permits, and committed/pushed on the exact candidate handed to local execution.
- `LOCAL_ONLY` — qualification requires the real local environment; remote agents skip that execution/evidence after finishing and pushing all source-safe work.
- `LOCAL_PASS` — may be recorded only from local evidence tied to an exact commit/SHA.
- `NOT QUALIFIED` — no valid local/engineering qualification exists yet; it is not a request for remote agents to retry it.

Remote source review may move work to `REMOTE_DONE`; it must never manufacture `LOCAL_PASS`.

## Future audit rule

For every future remote broad audit / `continue all` pass:

1. fetch latest `main`;
2. read this file, `docs/REMOTE-IMPLEMENTATION-COMPLETION-2026-08-11.md` and `docs/LOCAL-AGENT-INBOX.md` before building a backlog;
3. filter LOCAL_ONLY **execution/evidence** items out of the remote backlog only after confirming their repository-safe implementation is already complete and pushed; otherwise finish that source-safe work first;
4. do not search the repository merely to determine whether a previously parked V25/private-DWG/signing runtime test has now passed;
5. do not repeat LOCAL_ONLY gaps in every remote completion report;
6. update the inbox only when a new source change materially changes or introduces the required local scenario;
7. continue implementing all remaining source-safe gaps demonstrated by current source/issues rather than generating speculative parallel architecture.

A remote agent may mention once that local-only gates are parked, but should not spend the next audit rechecking them.

## Local-agent execution

Agents with the actual local environment own these gates. They must start from:

1. `docs/LOCAL-AGENT-INBOX.md` — choose the highest-priority compatible `OPEN`/`IN_PROGRESS` item;
2. sync/verify the **exact pushed SHA** named by that inbox item before executing runtime validation;
3. the item's linked runbook, especially `docs/LOCAL-V25-QUALIFICATION.md` for exact V25 qualification;
4. supporting detail such as `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md`, `docs/LOCAL-AGENT-OPEN-WORK-ADDENDUM-2026-08-10.md` or `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md` only as needed for that inbox item.

Only a local agent with appropriate evidence may close a LOCAL_ONLY gate. A `PASS` inbox status must include sanitized evidence tied to the exact tested SHA. Keep proprietary DLLs, private drawings, signing keys/credentials and unsanitized runtime evidence out of Git.

If local execution exposes a source-safe defect, local records the smallest sanitized evidence and returns that defect to the remote/source lane rather than spending local runtime/token budget on ordinary repository-safe engineering. Local implementation edits are reserved for fixes that genuinely require the local/proprietary environment to reproduce, implement, or prove.

## CI / release

This scope rule does not authorize CI/CD. GitHub Actions remain manual-only under `CI_POLICY.md`. `continue all`, source completion, docs changes or local handoff preparation do not authorize workflow dispatch or release publication.
