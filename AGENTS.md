# Agent Collaboration Policy

This repository is expected to have multiple agents working concurrently. Every agent must protect other agents' work and choose tasks that match its actual execution environment.

## Locked product form: BricsCAD plugin

QS3D is a **BricsCAD V25 x64 .NET plugin**, not a standalone CAD desktop executable. BricsCAD is required at runtime; the native BricsCAD viewport/database/editor remain the CAD host. `QS3D.BricsCAD.V25` builds as a library/DLL and is loaded by DemandLoad or `NETLOAD`.

`BLT-like`, `BLT-style`, `BLT3D-familiar`, “giống BLT” and similar wording refer to clean-room **workflow/UX familiarity only**. Do not reinterpret those phrases, modeless/full-screen window wording, or the CAD-independent `QS3D.Core` layer as a requirement for `QS3D.exe` or a QS3D-owned CAD engine.

The canonical boundary is `docs/PRODUCT-BOUNDARY.md`. Changing the product into a standalone application requires a new explicit owner requirement and coordinated architecture/build/release changes; agents must never infer that change on their own.

## Mandatory handoff reading order

Before starting substantive work, read in this order:

1. `AGENTS.md` (this file);
2. `docs/PRODUCT-BOUNDARY.md` — **canonical product/hosting boundary**;
3. `CI_POLICY.md`;
4. fetch the latest `main`;
5. `docs/AGENT-WORK-REGISTRATION.md` and every `ACTIVE` / `BLOCKED` file under `docs/agent-work-claims/` — **canonical pre-work reservation contract; the claim commit must already be visible on `origin/main` before implementation or qualification starts**;
6. `docs/REMOTE-AGENT-SCOPE.md` — **canonical remote/local execution boundary; remote agents must filter LOCAL_ONLY work out of their backlog instead of rechecking it**;
7. `docs/AGENT-HANDOFF-CURRENT-2026-08-10-2306.md` — **newest short canonical current-state delta for Rule/Regen Preview, Health baseline/diff, privacy-safe diagnostics and current source/product logic**;
8. `docs/AGENT-HANDOFF-CURRENT-2026-08-10-2037.md` — previous fast-moving source delta retained for concurrent persistence/interchange/documentation context;
9. `docs/AGENT-HANDOFF-LATEST-2026-08-10.md` — broader current-source baseline/handoff retained for detail;
10. `docs/IMPLEMENTATION-STATUS.md`;
11. `docs/PLAN.md`, `docs/SOURCE-PRODUCT-PLAN-2026-08-10.md` and `docs/COMMANDS.md`;
12. `docs/COMMANDS-PREVIEW-DIAGNOSTICS.md` — Rule Preview, Regen Preview, privacy-safe Diagnostic Summary and guarded Core Apply boundaries;
13. `docs/DIRECT-DRAW-WORKFLOW.md` — **owner-required BLT-style direct authoring direction**;
14. `docs/DIRECT-DRAW-P0-IMPLEMENTATION.md` — current P0 source/rollback/runtime boundary;
15. `docs/DIRECT-DRAW-P1-IMPLEMENTATION.md` — guarded GlassWall/WallPier/StructuralWall/Foundation plus current Direct Draw extension summary;
16. `docs/DIRECT-DRAW-OPENINGS.md` — **current Door/WallOpening source + Auto Host + explicit physical-cut boundary and V25 runtime checklist**;
17. `docs/LOCAL-AGENT-INBOX.md` — **single live priority queue for every LOCAL_ONLY gate; remote agents must register new/changed local scenarios here in the same batch, and local agents start here**;
18. `docs/LOCAL-V25-QUALIFICATION.md` — **LOCAL_ONLY execution runbook for agents with interactive Windows + licensed BricsCAD V25; remote agents do not re-run/re-audit it**;
19. `docs/LOCAL-PREVIEW-DIAGNOSTIC-QUALIFICATION-2026-08-10.md` — **LOCAL_ONLY exact-SHA qualification for read-only previews, privacy-safe diagnostic export and future guarded Apply confirmation/Undo/session behavior**;
20. `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md` — **LOCAL_ONLY remaining Curtain-panel, physical wall-junction, standard-specific rebar and production-signing detail**;
21. `docs/LOCAL-AGENT-OPEN-WORK-ADDENDUM-2026-08-10.md` — **LOCAL_ONLY/runtime/policy detail including whole-command Curtain recovery, native DrawJig/repeated authoring, commercial-license policy/wiring, legal distribution and performance/UX gates**;
22. `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md` — **consolidated LOCAL_ONLY execution detail for Interchange JSON, documentation, polygon mesh, Level Z-chain, Source Reconcile, Curtain, L/T/X, Direct Draw, signing/licensing and performance**;
23. `docs/DOCUMENTATION-LAYER.md` — semantic-tag and native documentation-table source/runtime boundaries;
24. `docs/INTERCHANGE-JSON.md` — read-only semantic interchange format and runtime qualification boundary;
25. `docs/INTERCHANGE-IMPORT-RESOLUTION-POLICY.md` — **explicit collision/provenance/generated-output policy planning and execution boundaries**;
26. `docs/AGENT-HANDOFF-SESSION-HISTORY-2026-08-10.md` only when deeper session chronology, old branch/gate history, screenshot requirements or early implementation evidence is needed.

The local-agent inbox is the live priority index. Longer `LOCAL-*` documents are runbooks/history/detail and must not become competing live queues. If they conflict on current priority/status, `docs/LOCAL-AGENT-INBOX.md` wins; current source still wins for implementation truth.

The session-history handoff is intentionally retained as an audit trail, but it contains historical source-status statements that can become stale as `main` evolves. When it conflicts with the current handoff or current source, current `main` wins. For product-form/hosting ambiguity, `docs/PRODUCT-BOUNDARY.md` is authoritative unless the owner explicitly changes that requirement.

## Mandatory work registration

Every agent must reserve substantive repository work before implementation. This includes source, tests, scripts, documentation batches, local V25 qualification, packaging and release preparation.

1. Fetch and integrate the latest `origin/main`.
2. Read `docs/AGENT-WORK-REGISTRATION.md` and inspect every `ACTIVE` or `BLOCKED` claim under `docs/agent-work-claims/`.
3. Choose a scope that does not overlap an existing reservation.
4. Add one uniquely named Markdown claim containing the exact scope, expected files/surfaces, exclusions, baseline SHA, validation plan and agent identity.
5. Commit and push that claim to `origin/main` **without any implementation changes**.
6. Verify the claim commit is reachable from current `origin/main`; only then begin the reserved work.

An unpushed local claim, chat message, private branch or draft patch does not reserve work. If the intended scope expands, update and push the claim before touching the added scope. `ACTIVE` and `BLOCKED` claims remain reserved; agents may not assume a quiet or old claim is abandoned. A scope becomes available only after the claim is explicitly `COMPLETED` or `RELEASED`, or the repository owner coordinates a takeover.

Read-only orientation needed to choose a lane is allowed before registration, but do not edit files, run a substantive qualification lane or create material runtime artifacts until the reservation is published. The registration commit is an intentional exception to request-scoped batching because its purpose is to become visible before the implementation batch begins.

`docs/LOCAL-AGENT-INBOX.md` remains the product/runtime gate queue; it does not identify which agent currently owns a task. Work claims record temporary agent ownership and must reference the relevant `LOCAL-###` item when applicable.

## Mandatory sync discipline

Before starting a code change:

1. refresh/fetch the latest `main`;
2. inspect recent commits and changed files relevant to the task;
3. base the work on the current branch head, not on an older snapshot from the beginning of the conversation/session.

Before every commit/push to `main`:

1. refresh the current `main` again;
2. verify whether another agent has pushed since the last sync;
3. if `main` moved, rebase/reapply/merge the intended patch onto the latest head without discarding newer work;
4. review the final diff so the commit contains only the intended changes.

For longer tasks, repeat this sync periodically instead of waiting until the end. Assume another agent can commit at any time.

Never force-push over concurrent work, reset `main` backwards, or silently revert another agent's changes unless the repository owner explicitly requests that exact operation.

## Request-scoped commit batching

The repository owner explicitly prefers **coherent commits scoped to the owner request**, not a stream of tiny commits.

- Treat one owner request or `continue all` batch as the default commit unit.
- Accumulate related source implementation, regression/smoke/static guards, documentation and canonical handoff updates, review the combined diff, then commit the coherent batch.
- **Do not commit merely because one file or one small fix is finished.** Avoid file-by-file, test-by-test and docs-after-code commit chains for one request.
- Split a request into more than one commit only when the parts are genuinely independent and separately revertable/risky, when integrating an already-existing independent PR (prefer squash), or when concurrent movement of `main` makes separate conflict-safe integration necessary.
- If another agent lands overlapping work while a batch is in progress, review and reuse the winning implementation instead of committing a duplicate.
- Immediately before the final batch commit, sync `main` again. If it moved, reapply/rebase the whole intended batch onto the new head and never force-push stale history.

Commit messages should describe the request-level capability or safety outcome, not the last individual file touched.

## Divide work by execution capability

### Agents with local-machine access

If an agent has permission and tooling to operate a real/local machine, that agent should prioritize work that genuinely requires that local environment, especially:

- BricsCAD V25 installation/runtime access;
- real `NETLOAD` / DemandLoad and interactive plugin validation;
- Windows desktop/UI interaction and screenshots;
- local licensed/proprietary dependencies that cannot be stored in GitHub;
- private DWG fixtures or files that exist only on the local machine;
- runner registration, environment variables, installed SDK/runtime inspection;
- reproducing machine-specific crashes, DPI/layout issues, file-lock behavior, or native CAD behavior.

Start every local pass from `docs/LOCAL-AGENT-INBOX.md`: choose the highest-priority compatible `OPEN`/`IN_PROGRESS` item, then follow its linked runbook. For exact V25 qualification, continue with `docs/LOCAL-V25-QUALIFICATION.md` and run `scripts/run-local-v25-qualification.ps1` against a **clean exact SHA** before manual scenario testing. For the preview/diagnostic workflow also read `docs/LOCAL-PREVIEW-DIAGNOSTIC-QUALIFICATION-2026-08-10.md`. The remaining historical/detail handoffs are supporting material for the inbox item, not separate queues. Close only gates for which the local agent can produce the required evidence or owner-supplied policy. Keep generated runtime evidence under `artifacts/` or another explicitly local folder; `artifacts/` is intentionally gitignored. Do not claim a customer-release qualification when the runner used `-SkipRuntime`.

Do not spend scarce local-machine access on ordinary repository editing, documentation cleanup, broad source review, or other tasks that remote agents can perform equally well unless those tasks directly unblock local validation.

### Remote / hybrid online agents

Remote or hybrid agents should handle work that does not require the real local BricsCAD machine, including:

- GitHub source review and implementation;
- core/domain/persistence/reporting/test code;
- static analysis and code-quality fixes;
- Markdown/documentation/planning;
- workflow/policy review without dispatching Actions;
- Git history inspection and multi-agent integration;
- preparing scripts, tests, patches, and runtime probes for a local agent to execute later.

`docs/REMOTE-AGENT-SCOPE.md` is authoritative for remote backlog filtering. A remote agent must **skip**, rather than repeatedly re-check, qualification already classified `LOCAL_ONLY`, including real V25/Windows runtime, NETLOAD/DemandLoad, private-DWG, native UI/performance, clean-machine installer and real Authenticode private-key/timestamp gates. During broad `continue all` or source audits, do not search merely to see whether those local gates have become PASS, do not reopen them as remote backlog, and do not block remote completion on them.

Remote agents may still implement or strengthen source contracts, static guards, deterministic tests and local probes around those areas. If such source work changes what must be validated locally, add or update the matching `docs/LOCAL-AGENT-INBOX.md` item **in the same source/docs batch** with the minimum exact scenario/evidence required, then continue remote source work. Do not park a new local gate only in prose elsewhere. Remote agents must never manufacture `LOCAL_PASS` from source/static evidence.

## Mandatory unavailable-work handoff

If an agent cannot complete, execute, reproduce, or prove a task because its environment lacks the required local machine, licensed BricsCAD V25 runtime, private DWG/fixture, Windows UI, signing credential, hardware, installed dependency, or other non-repository resource, the agent **must not leave that work only in chat and must not repeatedly retry it from another equivalent remote/non-local agent**.

Instead, before ending the same work batch, the agent must:

1. classify the blocked part as `LOCAL_ONLY` when local execution can resolve it;
2. add or update the matching item in `docs/LOCAL-AGENT-INBOX.md` with the exact scenario, prerequisite, expected result and minimum evidence required;
3. reference an existing detailed runbook rather than creating a duplicate live queue; create or extend supporting Markdown only when the inbox item genuinely needs more execution detail;
4. leave all source-safe implementation, deterministic tests, probes and scripts ready for the local agent whenever possible;
5. continue with other remote-safe work instead of stopping the whole `continue all` pass;
6. treat the parked inbox item as owned by a compatible local agent until source changes materially alter the scenario or real local evidence is posted.

Once a task is recorded in `docs/LOCAL-AGENT-INBOX.md`, subsequent remote/non-local agents must **read and skip that execution gate rather than rediscovering, re-auditing, re-running, or re-reporting the same inability**. They may only change the item when new source materially changes the required local scenario, or when they can add a concrete source-side prerequisite/probe that reduces the local work. Lack of local capability is a handoff condition, not a reason for repeated remote attempts.

## Handoff rule

When a remote agent reaches a new task that requires local-only access, leave the repository in a runnable/testable state and register the exact scenario in `docs/LOCAL-AGENT-INBOX.md`, which is the canonical live priority/status index. `docs/LOCAL-V25-QUALIFICATION.md` is the canonical exact-V25 execution runbook; preview/diagnostic runtime work is detailed in `docs/LOCAL-PREVIEW-DIAGNOSTIC-QUALIFICATION-2026-08-10.md`; remaining historical implementation/engineering/signing details live in `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md`, `docs/LOCAL-AGENT-OPEN-WORK-ADDENDUM-2026-08-10.md` and `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`. Extend detailed handoffs only when useful, but always update the inbox when a new/changed local scenario affects current work. Do not repeatedly re-audit an already parked LOCAL_ONLY gate from a remote environment.

When a local agent finishes validation, update the matching inbox item with `PASS` only when sanitized evidence is tied to the exact tested SHA. Commit only reusable source/scripts/docs and a sanitized text summary if useful; never commit proprietary BricsCAD DLLs, private fixtures, screenshots containing private drawings, signing secrets or raw machine evidence.

When adding major source capability, update `docs/AGENT-HANDOFF-CURRENT-2026-08-10-2306.md` or create a newer canonical current handoff and update this reading-order pointer. Do not make agents infer current status from an old session transcript alone.

## GitHub Actions / release

Follow `CI_POLICY.md` strictly:

- all workflows are `workflow_dispatch` only;
- do not add automatic/event-driven triggers;
- do not dispatch or re-run Actions because code/docs were changed, committed, pushed, merged, reviewed, handed off, or because the owner said `continue all`;
- CI/build/runtime/release runs require a **separate explicit owner request**;
- preparing `.github/workflows/release-v25.yml` does not authorize running it;
- publishing a GitHub Release is allowed only when the owner explicitly requests a release and the manual workflow receives `confirm_release=RELEASE`;
- use `scripts/preflight-ci-manual-only.py` as the strict repository guard against accidental automatic CI/CD triggers.

For an approved build/release operation, read `docs/MANUAL-BUILD-RELEASE.md` first and resolve the exact commit/tag before dispatching anything.
