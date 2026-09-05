# QS3D Code embedded agent harness — architecture design

Issue: #5545  
Lane-Key: `issue-5545`  
Design baseline: `main@bcd49b26b50cb9a2027750ec14fc074b1cb6111c`  
Status: owner-approved architecture, design-phase specification

## 1. Purpose

QS3D Code is a developer-first agent harness for the `QS3D-BricsCAD` product family. It combines three delivery surfaces around one shared execution engine:

1. **C — shared harness core/API**: task routing, skill loading, policy, permissions, tool execution, execution trace, validation, GitHub carrier lifecycle and CAD-tool coordination.
2. **A — repo-local `/qs3d` CLI**: a fast developer client over the same harness core.
3. **B — embedded QS3D Code UI**: a Jarvis-style dockable/modeless panel opened from the QS3D Ribbon inside BricsCAD and hosted by the QS3D plugin.

The embedded BricsCAD panel is the primary graphical V1 surface. The CLI remains first-class, and both clients consume the same harness contracts rather than duplicating routing, permissions, GitHub behavior or skill logic.

V1 is for the repository owner/development workflow. The architecture keeps user/workspace/provider boundaries explicit so future public or external clients can be added without rewriting the core, but V1 does not implement public multi-tenant auth, billing or a standalone end-user product.

## 2. Existing repository contracts remain authoritative

QS3D Code is an orchestration layer around the repository's existing governance, not a replacement governance system.

The harness must obey the current canonical contracts, including:

- `AGENTS.md` for everyday task lifecycle;
- `docs/MAIN-WRITE-AUTHORIZATION.md` for merge authority;
- `CI_POLICY.md` for CI semantics;
- `docs/AGENT-RESERVATION-V2.md` for collision/reservation rules;
- `docs/AGENT-WORK-REGISTRATION.md` for canonical carriers;
- `docs/REMOTE-AGENT-SCOPE.md` and local qualification runbooks for `LOCAL_ONLY` boundaries;
- `docs/MCP-CANONICAL-RUNBOOK.md` when MCP/host automation is in scope.

A harness convenience feature must never bypass protected checks, forge local runtime evidence, replace a canonical carrier merely because it is stale/red, force-push protected work, or invent an alternate merge policy.

## 3. Product experience

### 3.1 BricsCAD embedded experience

The QS3D Ribbon gains a `QS3D Code` entry. Activating it opens a dockable/modeless QS3D palette that can be docked left/right, floated, resized, hidden and reopened while preserving the current developer session state where safe.

The panel exposes five logical workspaces:

- **Chat** — conversational task entry and project questions.
- **Code** — files, diffs, edits, tests and source-oriented task progress.
- **CAD** — active document/selection diagnostics and explicitly permitted CAD operations.
- **GitHub** — Issue/Lane-Key/branch/PR/CI state for the current canonical carrier.
- **Agents** — current sessions, loaded skills, execution trace, blockers and permission requests.

A typical coding task appears as a compact trace instead of raw internal reasoning:

```text
> fix MCP save durability issue

✓ classified: MCP + persistence + CI
✓ read AGENTS.md
✓ loaded mcp / persistence / ci-remediation skills
✓ found canonical issue + branch + PR
✓ regression RED
▶ implementation
○ focused tests
○ commit/push
○ exact-head CI
○ merge when protected checks are green
```

The UI may expose expandable details such as files read, commands executed, tool calls, test output and CI jobs, but it does not expose hidden chain-of-thought.

### 3.2 Repo-local CLI experience

The CLI uses the same task contract:

```text
qs3d fix "MCP save durability"
qs3d issue 5545
qs3d run --skill ci-remediation
qs3d trace <session-id>
qs3d cancel <session-id>
```

A slash-command presentation such as `/qs3d ...` may be added in clients that support slash commands, but the canonical executable contract is a normal repo-local `qs3d` command.

When a licensed BricsCAD QS3D host is active, the CLI may connect to the local harness host through authenticated local IPC for CAD-aware inspection/actions. Without a host, the CLI remains fully usable for repository/GitHub/source tasks and must fail closed for host-only actions.

## 4. High-level architecture

```text
                          QS3D Harness Core
                   ┌───────────────────────────┐
                   │ task / skills / policy    │
                   │ tools / trace / validation│
                   │ GitHub / permissions      │
                   └─────────────┬─────────────┘
                                 │
                   ┌─────────────┼─────────────┐
                   │             │             │
                   ▼             ▼             ▼
            repo-local CLI   BricsCAD UI   future clients
               `qs3d`        QS3D Code      / local API
                                  │
                                  ▼
                         BricsCAD Host Bridge
                                  │
                     document/main-thread dispatch
                                  │
                                  ▼
                         active DWG + QS3D plugin
```

The key rule is that the embedded UI is a client, not the orchestration authority. The CLI and the UI must not implement their own independent task routers or GitHub lifecycle logic.

## 5. Module boundaries

### 5.1 Harness Kernel

The kernel owns lifecycle state and orchestration. Its public concepts are intentionally small:

- `HarnessSession` — one user-requested execution session.
- `TaskIntent` — normalized requested outcome and scope.
- `TaskPlan` — observable execution stages, not hidden reasoning.
- `HarnessEvent` — append-only observable trace events.
- `ToolRequest` / `ToolResult` — typed tool boundary.
- `PermissionRequest` — explicit policy decision boundary.
- `CancellationToken` / emergency-stop state.

The kernel must be host-neutral. It does not reference BricsCAD runtime types.

### 5.2 Task Router

The router classifies work into bounded domains such as:

- source/code change;
- CI remediation;
- GitHub carrier management;
- MCP/transport;
- persistence/durability;
- BricsCAD host/runtime;
- release/package;
- CAD inspection/mutation.

Classification selects candidate skills and tools; it does not override repository governance. Ambiguous destructive work produces a permission/clarification gate instead of guessing.

### 5.3 Skill Router and Skill Registry

Skills are modular instructions loaded on demand. V1 uses repository-owned declarative skill packages rather than one giant system prompt.

A skill declares:

- stable skill id and version;
- trigger metadata;
- required canonical docs;
- optional reference docs/examples;
- permitted tool classes;
- validation expectations;
- incompatible or prerequisite skills.

The registry supports progressive context loading: load only the minimum core policy first, then specialist skills/docs required by the task.

Initial skill families should cover at least:

- repository lifecycle / Reservation-v2;
- TDD/source implementation;
- CI remediation;
- GitHub PR/merge lifecycle;
- MCP/transport;
- persistence/durability;
- BricsCAD V25/V26 host boundaries;
- CAD inspection/mutation safety;
- release/local-only handoff.

Skills are advisory workflow modules underneath the repository's canonical authority. A stale skill cannot override `AGENTS.md`, `CI_POLICY.md` or a current specialist runbook.

### 5.4 Context Loader

The context loader resolves and records exactly what evidence a session consumes:

- current `main` SHA;
- canonical Issue/Lane-Key/branch/PR metadata;
- changed files and source excerpts;
- relevant canonical docs;
- current CI/job evidence;
- optional current BricsCAD context through the host bridge.

Every material result must remain bound to the exact source/runtime identity that produced it. Stale green CI or previous-runtime evidence cannot silently satisfy a newer candidate.

### 5.5 Tool Registry

Tools are registered by capability rather than exposed as arbitrary implementation details.

Tool families include:

- filesystem/source read-edit-diff;
- process/test/build execution;
- Git/GitHub Issue/PR/CI operations;
- MCP/HTTP diagnostics where policy permits;
- BricsCAD read/inspect;
- BricsCAD mutation;
- local host/session control.

Each tool advertises permission category, cancellation behavior, whether it is host-bound, and whether it mutates persistent state.

### 5.6 Policy and Permission Engine

The policy engine combines repository rules with a local permission profile. V1 default developer profile:

| Capability | Default |
| --- | --- |
| read repo/docs/current GitHub state | AUTO |
| run focused tests/preflights/builds | AUTO |
| edit task branch workspace | AUTO after canonical carrier is valid |
| commit/push canonical task branch | AUTO |
| create/update canonical Issue/PR | AUTO |
| merge same-task PR | only when repository authorization + protected gates allow |
| CAD read/inspect | AUTO when host/document context is valid |
| CAD mutation | policy-gated; explicit operation contract required |
| save active DWG | explicit CAD mutation permission/profile |
| reveal/copy secrets | DENY |
| force-push protected branches | DENY |
| bypass CI/rules/reservation | DENY |
| destructive unrelated external action | CONFIRM or DENY |

The UI may provide a developer `Elevated` profile, but elevated mode still cannot supersede repository hard-deny rules such as force/bypass behavior.

### 5.7 Trace Engine

The trace engine emits structured observable events such as:

- `session.started`;
- `task.classified`;
- `skill.loaded`;
- `context.read`;
- `tool.started/completed/failed`;
- `permission.requested/resolved`;
- `test.red/green`;
- `git.commit/push`;
- `github.ci.started/completed`;
- `cad.operation.started/completed`;
- `session.blocked/completed/cancelled`.

Trace records execution facts and summaries, not private chain-of-thought. Secrets and sensitive payloads are redacted before persistence or UI display.

### 5.8 Validation and CI Remediation Engine

Validation is stage-aware:

1. focused regression or source preflight;
2. broader relevant local/static checks;
3. branch push and exact-head CI;
4. canonical PR protected checks;
5. mergeability/freshness/collision verification;
6. merge under repository authorization;
7. refresh and verify resulting `main`;
8. issue/reservation cleanup.

Known fixable current-lane failures feed back into the same task session and canonical carrier. The engine must not create replacement carriers merely to escape red CI.

### 5.9 Provider boundaries

External integrations sit behind interfaces:

- `IModelProvider` — model/chat completion backend;
- `IRepositoryProvider` — Git/GitHub repository operations;
- `IProcessRunner` — controlled process execution;
- `ICadHostBridge` — active BricsCAD/QS3D context;
- `ISecretProvider` — non-exportable local credential references;
- `ISessionStore` — trace/session state.

V1 may provide only one implementation for several interfaces, but the core is not allowed to hard-code secrets, one API endpoint, or one UI client into the orchestration model.

## 6. BricsCAD embedded host architecture

### 6.1 Palette hosting

QS3D Code is opened by a QS3D Ribbon command/button and hosted using the existing BricsCAD modeless/dockable palette pattern already used by QS3D.

The BricsCAD-facing shell remains native WPF so palette lifetime, focus, document switching and disposal follow known host behavior.

The rich content layer is renderer-separated from host orchestration. V1 may use a modern embedded renderer (for example a WebView2-backed panel) where package/runtime compatibility is proven, with a native-WPF fallback surface for critical controls and degraded mode. The harness core must not depend on a particular renderer.

Critical controls that must remain available even if the rich renderer fails include:

- session cancel/emergency stop;
- connection/host status;
- permission prompt;
- current task status;
- reopen/reload UI.

### 6.2 CAD Host Bridge

Background agent code and UI code must never call arbitrary BricsCAD APIs directly.

All CAD access goes through a typed bridge with two classes of operations:

- **read/inspect** — active document identity, selection summary, entity snapshot, command/document state, health/diagnostics;
- **mutation** — narrowly typed QS3D/CAD actions with explicit document affinity and permission requirements.

The bridge is responsible for:

- resolving the active document at execution time;
- rejecting stale document/session identity;
- dispatching onto the required BricsCAD UI/document context;
- acquiring the appropriate document/database lock/transaction boundary;
- cancellation before mutation where safe;
- returning serializable results to the harness;
- never leaking live `DBObject`, transaction or `ObjectId` references across async/background boundaries.

Persistent entity references use the repository's normal drawing identity/fingerprint + Handle rules rather than runtime `ObjectId` identity.

### 6.3 Responsiveness and process isolation

Long-running model calls, GitHub polling, source search, builds and CI monitoring run off the BricsCAD UI thread.

Only the minimal native host interaction executes on the host-required thread/context. A model/network stall therefore must not freeze BricsCAD.

The harness supports cooperative cancellation. `Emergency Stop` stops new tool dispatch, cancels cancellable background work and prevents queued CAD mutations from starting. It does not corrupt an already-committing native/persistence boundary; such operations must finish or fail according to their existing transaction contract.

### 6.4 V25/V26 compatibility

Shared harness contracts must be target-compatible with both host lanes. BricsCAD-specific adapters remain version-aware:

- V25: `.NET Framework 4.8` host adapter;
- V26: `.NET 8 Windows` host adapter.

No V25 host binary is relabeled or reused as V26 evidence. Shared source is allowed only where the existing repository's V25/V26 linking pattern and compile gates permit it.

## 7. CLI and local IPC

The repo-local CLI is an external process so it can continue source/GitHub work without loading BricsCAD.

When an active QS3D Code host is available, CLI-to-host communication uses local-only authenticated IPC. The preferred V1 transport is a user-scoped named pipe with:

- random per-session capability token exchanged through user-private local state;
- same-user access restriction where supported;
- protocol version handshake;
- explicit host instance/document identity;
- no network listener required for local CLI integration;
- fail-closed behavior when host identity changes or the pipe is stale.

The IPC protocol carries typed harness/CAD messages, not arbitrary remote code execution strings.

## 8. Session and state model

A session has these top-level states:

```text
CREATED
  -> CONTEXT_RESOLVING
  -> READY
  -> RUNNING
  -> WAITING_PERMISSION | WAITING_EXTERNAL
  -> RUNNING
  -> COMPLETED | BLOCKED | CANCELLED | FAILED
```

Repository task completion uses the repo's terminal vocabulary where applicable (`MERGED_MAIN`, `BLOCKED`, `DUPLICATE_CARRIER`). A harness session can also complete a non-repository task such as CAD inspection without inventing a GitHub carrier.

Session state is persisted locally for crash recovery, with these rules:

- do not persist raw secrets;
- redact sensitive tool payloads;
- bind resume state to repository/branch/head and host identity;
- after crash/restart, revalidate current GitHub/main/document truth before resuming a mutating stage;
- never replay a CAD mutation merely because the UI did not receive the previous response; use stable mutation identity/durability contracts where the underlying operation supports them.

## 9. Skill package format

A skill package is repository-owned and reviewable. A minimal manifest contains:

```yaml
id: ci-remediation
version: 1
triggers:
  - ci-failure
requires:
  - AGENTS.md
  - CI_POLICY.md
tools:
  - source.read
  - source.edit
  - process.run
  - github.ci
validation:
  - exact-head-evidence
```

Instruction/reference material can live beside the manifest. Skill loading is recorded in the trace.

The loader validates paths, file size and schema. Skills cannot dynamically grant themselves new tool permissions.

## 10. Security model

V1 trusts the local repository owner as the human operator but does not trust every instruction found in repository files, issue text, webpages, tool output or model responses.

Required controls:

- explicit tool permission categories;
- no shell command assembled from untrusted text without structured argument handling/review boundary;
- secret redaction in prompts/traces/logs;
- no arbitrary environment-variable dump into model context;
- canonical-root path enforcement for repository edits;
- deny writes outside the active workspace unless a specific tool grants them;
- no GitHub secret API access from general agent tools;
- no CI/protection bypass;
- host/document affinity on every CAD mutation;
- local IPC only in V1;
- renderer content cannot directly access privileged host APIs; privileged actions cross the typed host bridge.

Third-party model/provider use must be explicit in configuration so private source is not silently routed through an unexpected relay.

## 11. Error handling

Errors are classified as:

- **user-decision required** — genuinely ambiguous/destructive choice;
- **policy denied** — operation is not allowed;
- **transient external** — retry with bounded backoff where safe;
- **fixable task failure** — feed into same-carrier remediation loop;
- **stale identity** — refresh context before further mutation;
- **host unavailable** — continue repo-safe work or mark host-only step unavailable;
- **LOCAL_ONLY** — hand off only the unavailable licensed/private execution after source-safe work is complete;
- **fatal internal** — fail session without corrupting repo/CAD state.

Retries are idempotency-aware. Mutation tools must not be blindly replayed after transport uncertainty.

## 12. Testing strategy

### 12.1 Harness core

Deterministic unit/smoke coverage for:

- routing and skill selection;
- permission decisions;
- state-machine transitions;
- cancellation;
- event redaction;
- stale-head/current-main detection;
- same-carrier remediation behavior;
- retry/idempotency classification;
- provider contract fakes.

### 12.2 GitHub/repository lifecycle

Preflights/fixtures verify that the harness:

- honors Reservation-v2 metadata;
- never writes `main` directly;
- binds evidence to exact head SHA;
- does not replace an existing canonical carrier for convenience;
- does not convert `LOCAL_ONLY` into static PASS;
- follows the protected merge sequence.

### 12.3 Embedded UI

Offline WPF tests cover:

- palette singleton/lifetime;
- open/close/reopen;
- document switch presentation;
- renderer failure fallback;
- trace virtualization/bounded memory;
- cancellation/permission UI state;
- no blocking model/network work on the dispatcher thread.

### 12.4 CAD bridge

Source/compile guards verify:

- no live host object crosses async boundaries;
- document affinity is rechecked at execution time;
- mutation paths are typed and permission classified;
- V25/V26 compile compatibility.

Licensed BricsCAD validation remains `LOCAL_ONLY` and must separately prove palette interaction, document switching, real selection inspection, guarded mutation, save/reopen behavior where required and clean shutdown on exact candidate binaries.

## 13. Delivery decomposition

This architecture is one product outcome but too large for one implementation mega-PR. After this design is approved, implementation should use bounded child carriers under #5545 with explicit non-overlapping ownership keys and paths.

Recommended sequence:

1. **Core contracts + trace + permissions + skill registry** — host-neutral foundation.
2. **Repo-local CLI + repository/GitHub adapter** — proves A+C without BricsCAD UI dependency.
3. **BricsCAD host bridge + session host service** — typed CAD boundary and local IPC.
4. **Embedded QS3D Code palette shell + Ribbon command** — native host/lifetime layer.
5. **Rich Chat/Code/CAD/GitHub/Agents UI** — Jarvis-style experience over the same event/API contracts.
6. **Integrated self-remediation/CI workflow + end-to-end hardening** — exact-head lifecycle and recovery.
7. **Licensed V25/V26 local qualification** — exact candidate runtime evidence for embedded behavior.

Each child carrier must independently satisfy Reservation-v2 and repository CI rules. The umbrella design Issue does not grant overlapping path ownership to future implementation lanes.

## 14. V1 acceptance criteria

V1 is successful when all of the following are true:

1. A QS3D Ribbon/button opens a dockable/modeless `QS3D Code` panel inside real BricsCAD through the QS3D plugin.
2. The panel remains responsive while model/GitHub/build/CI work runs asynchronously.
3. The panel shows structured Chat/Code/CAD/GitHub/Agents state and expandable execution trace.
4. The repo-local `qs3d` CLI can start and observe the same task model used by the embedded UI.
5. Task/skill/policy/tool/trace logic lives in one shared harness core rather than duplicated client logic.
6. The harness can resolve or create a canonical Reservation-v2 carrier and follow the repo lifecycle without direct-main writes or bypasses.
7. Fixable exact-head CI failures can feed back into the same canonical session/carrier for remediation.
8. CAD access is only through the typed host bridge with document affinity and permission enforcement.
9. CLI can detect/connect to an active host through local authenticated IPC, and fails closed when no valid host exists.
10. Emergency stop prevents new queued privileged/CAD actions and cancels safe background work.
11. Trace/session persistence redacts secrets and revalidates identities before mutating after resume.
12. V25 and V26 source/compile gates pass for shared host changes; licensed runtime claims are made only from actual local qualification.

## 15. Explicit V1 non-goals

V1 does not include:

- a public multi-tenant SaaS service;
- billing/subscriptions;
- arbitrary third-party plugin marketplace;
- remote Internet control of a user's BricsCAD instance;
- bypass-permissions mode that overrides repository hard-deny rules;
- a standalone CAD engine or replacement for BricsCAD;
- automatic promotion of static evidence to licensed runtime PASS;
- a second independent GitHub/Reservation governance system.

## 16. Design decisions locked by this specification

- Build A+B+C as one architecture, not three separate products.
- The shared harness core is authoritative for routing/execution state.
- The QS3D Code GUI is primarily embedded inside BricsCAD through the QS3D plugin.
- The CLI is external and first-class, with optional local IPC into an active host.
- BricsCAD access is through a typed guarded host bridge only.
- Long-running agent work stays off the BricsCAD UI thread.
- V1 is developer-first and local-first, but provider/workspace/user boundaries remain explicit for future evolution.
- Repository governance remains authoritative and cannot be weakened by the harness.
- Implementation is decomposed into bounded child carriers rather than one mega-PR.
