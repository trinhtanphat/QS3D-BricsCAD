# QS3D remote-agent scope boundary

Updated: 2026-08-11 (UTC+7)

This file is the **canonical scope boundary for remote / hosted / source-only agents**.

## Owner decision

Tasks that require a real BricsCAD V25 / Windows workstation are **LOCAL_ONLY**.

`docs/LOCAL-AGENT-INBOX.md` is the **single live priority/status queue** for those gates. Remote agents must register a new or materially changed LOCAL_ONLY scenario there in the same source/docs batch that introduced or exposed it. Longer local handoff documents are supporting runbooks/history, not competing queues.

Remote agents must not repeatedly re-audit, re-run, re-open or re-report these gates during normal `continue all`, broad source review, planning or implementation passes. Park them in the inbox and continue with source-safe work.

The purpose of this rule is to stop remote reviews from spending time rediscovering the same environment boundary and to keep runtime truth tied to the machine that can actually prove it.

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

A remote agent must **not** block completion of its remote `continue all` pass merely because one of these LOCAL_ONLY gates is still pending.

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

If a source change introduces or changes a runtime contract, add/update the matching `docs/LOCAL-AGENT-INBOX.md` item with the **minimum exact local scenario and evidence needed in the same batch**. Do not execute or repeatedly re-audit the local scenario remotely, and do not leave the only current record in an older long-form handoff.

## Status vocabulary

Use these meanings consistently:

- `REMOTE_DONE` — the source/static contract requested from remote work is implemented and guarded.
- `LOCAL_ONLY` — qualification requires the real local environment; remote agents skip it.
- `LOCAL_PASS` — may be recorded only from local evidence tied to an exact commit/SHA.
- `NOT QUALIFIED` — no valid local/engineering qualification exists yet; it is not a request for remote agents to retry it.

Remote source review may move work to `REMOTE_DONE`; it must never manufacture `LOCAL_PASS`.

## Future audit rule

For every future remote broad audit / `continue all` pass:

1. fetch latest `main`;
2. read this file, `docs/REMOTE-IMPLEMENTATION-COMPLETION-2026-08-11.md` and `docs/LOCAL-AGENT-INBOX.md` before building a backlog;
3. filter LOCAL_ONLY items out of the remote backlog;
4. do not search the repository merely to determine whether a previously parked V25/private-DWG/signing runtime test has now passed;
5. do not repeat LOCAL_ONLY gaps in every remote completion report;
6. update the inbox only when a new source change materially changes or introduces the required local scenario;
7. continue implementing all remaining source-safe gaps demonstrated by current source/issues rather than generating speculative parallel architecture.

A remote agent may mention once that local-only gates are parked, but should not spend the next audit rechecking them.

## Local-agent execution

Agents with the actual local environment own these gates. They must start from:

1. `docs/LOCAL-AGENT-INBOX.md` — choose the highest-priority compatible `OPEN`/`IN_PROGRESS` item;
2. the item's linked runbook, especially `docs/LOCAL-V25-QUALIFICATION.md` for exact V25 qualification;
3. supporting detail such as `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md`, `docs/LOCAL-AGENT-OPEN-WORK-ADDENDUM-2026-08-10.md` or `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md` only as needed for that inbox item.

Only a local agent with appropriate evidence may close a LOCAL_ONLY gate. A `PASS` inbox status must include sanitized evidence tied to the exact tested SHA. Keep proprietary DLLs, private drawings, signing keys/credentials and unsanitized runtime evidence out of Git.

## CI / release

This scope rule does not authorize CI/CD. GitHub Actions remain manual-only under `CI_POLICY.md`. `continue all`, source completion, docs changes or local handoff preparation do not authorize workflow dispatch or release publication.
