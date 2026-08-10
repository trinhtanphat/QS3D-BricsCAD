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
5. `docs/REMOTE-AGENT-SCOPE.md` — **canonical remote/local execution boundary; remote agents must filter LOCAL_ONLY work out of their backlog instead of rechecking it**;
6. `docs/AGENT-HANDOFF-CURRENT-2026-08-10-2037.md` — **newest short canonical current-state delta for fast-moving source**;
7. `docs/AGENT-HANDOFF-LATEST-2026-08-10.md` — broader current-source baseline/handoff retained for detail;
8. `docs/IMPLEMENTATION-STATUS.md`;
9. `docs/PLAN.md` and `docs/COMMANDS.md`;
10. `docs/DIRECT-DRAW-WORKFLOW.md` — **owner-required BLT-style direct authoring direction**;
11. `docs/DIRECT-DRAW-P0-IMPLEMENTATION.md` — current P0 source/rollback/runtime boundary;
12. `docs/DIRECT-DRAW-P1-IMPLEMENTATION.md` — guarded GlassWall/WallPier/StructuralWall/Foundation plus current Direct Draw extension summary;
13. `docs/DIRECT-DRAW-OPENINGS.md` — **current Door/WallOpening source + Auto Host + explicit physical-cut boundary and V25 runtime checklist**;
14. `docs/LOCAL-V25-QUALIFICATION.md` — **LOCAL_ONLY execution handoff for agents with interactive Windows + licensed BricsCAD V25; remote agents do not re-run/re-audit it**;
15. `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md` — **LOCAL_ONLY remaining Curtain-panel, physical wall-junction, standard-specific rebar and production-signing gates**;
16. `docs/LOCAL-AGENT-OPEN-WORK-ADDENDUM-2026-08-10.md` — **LOCAL_ONLY/runtime/policy work including whole-command Curtain recovery, native DrawJig/repeated authoring, commercial-license policy/wiring, legal distribution and performance/UX gates**;
17. `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md` — **consolidated newest LOCAL_ONLY execution matrix for Interchange JSON, documentation, polygon mesh, Level Z-chain, Source Reconcile, Curtain, L/T/X, Direct Draw, signing/licensing and performance**;
18. `docs/DOCUMENTATION-LAYER.md` — semantic-tag and native documentation-table source/runtime boundaries;
19. `docs/INTERCHANGE-JSON.md` — read-only semantic interchange format and runtime qualification boundary;
20. `docs/INTERCHANGE-IMPORT-RESOLUTION-POLICY.md` — **explicit non-mutating collision/provenance/generated-output policy planning; never import authority**;
21. `docs/AGENT-HANDOFF-SESSION-HISTORY-2026-08-10.md` only when deeper session chronology, old branch/gate history, screenshot requirements or early implementation evidence is needed.

The session-history handoff is intentionally retained as an audit trail, but it contains historical source-status statements that can become stale as `main` evolves. When it conflicts with the current handoff or current source, current `main` wins. For product-form/hosting ambiguity, `docs/PRODUCT-BOUNDARY.md` is authoritative unless the owner explicitly changes that requirement.

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

Start local qualification from `docs/LOCAL-V25-QUALIFICATION.md`. Run `scripts/run-local-v25-qualification.ps1` against a **clean exact SHA** before manual scenario testing. For feature work that is intentionally deferred to a real V25/engineering/signing environment, also read `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md`, `docs/LOCAL-AGENT-OPEN-WORK-ADDENDUM-2026-08-10.md` and the consolidated `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`, then close only the gates for which the local agent can produce the required evidence or owner-supplied policy. Keep generated runtime evidence under `artifacts/` or another explicitly local folder; `artifacts/` is intentionally gitignored. Do not claim a customer-release qualification when the runner used `-SkipRuntime`.

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

Remote agents may still implement or strengthen source contracts, static guards, deterministic tests and local probes around those areas. If such source work changes what must be validated locally, update the local handoff with the minimum exact scenario and continue remote source work. Remote agents must never manufacture `LOCAL_PASS` from source/static evidence.

## Handoff rule

When a remote agent reaches a new task that requires local-only access, leave the repository in a runnable/testable state and document the exact local validation needed. The canonical local execution handoff is `docs/LOCAL-V25-QUALIFICATION.md`; remaining implementation/engineering/signing gates are tracked in `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md`, with additional runtime/product-policy gaps tracked in `docs/LOCAL-AGENT-OPEN-WORK-ADDENDUM-2026-08-10.md` and the current consolidated delta in `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`. Extend the appropriate handoff only when a new source change materially changes or introduces a local scenario. Do not repeatedly re-audit an already parked LOCAL_ONLY gate from a remote environment.

When a local agent finishes validation, commit only reusable source/scripts/docs and a sanitized text summary if useful; never commit proprietary BricsCAD DLLs, private fixtures, screenshots containing private drawings, signing secrets or raw machine evidence.

When adding major source capability, update `docs/AGENT-HANDOFF-CURRENT-2026-08-10-2037.md` or create a newer canonical current handoff and update this reading-order pointer. Do not make agents infer current status from an old session transcript alone.

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
