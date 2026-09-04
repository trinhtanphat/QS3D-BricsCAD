# QS3D Code embedded agent harness — implementation plan

> **For implementation agents:** execute this plan through Reservation-v2 child carriers. Use TDD for production behavior, exact-head evidence for every PR, and the current repository governance as the source of truth. Do not merge by bypassing checks, do not force-push, and do not claim licensed BricsCAD runtime evidence from remote CI.

**Parent:** #5545  
**Design:** `docs/superpowers/specs/2026-09-03-qs3d-code-embedded-agent-harness-design.md`  
**Plan carrier:** #5583  
**Plan baseline:** `main@f3106bdd8ca094d26d9bc1d0b052cf7e66ae8bf5`

## Goal

Deliver one QS3D Code product architecture through bounded child carriers: a host-neutral C# harness kernel in `QS3D.Core`, a repo-local `qs3d` CLI, a typed BricsCAD host bridge/local IPC layer, and an embedded Jarvis-style QS3D Code palette/ribbon surface. The kernel owns routing/policy/trace/lifecycle. CLI and embedded UI are clients over those shared contracts.

## Repository facts that shape the implementation

- `src/QS3D.Core/QS3D.Core.csproj` targets `netstandard2.0`; shared harness contracts must stay free of BricsCAD runtime references.
- `src/QS3D.Core/Agent/` already owns agent-facing shared contracts; add the harness underneath that boundary instead of creating a parallel Python orchestration stack.
- `tests/QS3D.Core.SmokeTests` is the established executable smoke-test harness. New core regression tests register through `SmokeTestRegistration.RunAll()`.
- V26 compiles nearly all V25 adapter/UI source through linked source in `QS3D.BricsCAD.V26.csproj`; shared BricsCAD-facing harness/UI implementation should therefore live in V25 source unless a host-major-specific entry point is required.
- Existing MCP Agent Control Center, palette coordinators, ribbon initialization, transport coordinator, desktop control, document lifecycle, and embedded-server code are integration points to extend, not duplicate.

## Cross-cutting rules for every child carrier

1. Refresh current `main`, search for an equivalent active Issue/Lane/branch/PR, and register a Reservation-v2 Issue before mutation.
2. Create a canonical branch `agent/<globally-distinct-session>/issue-<N>-<scope>` and a matching `.agent/claims/<N>-...md` claim with narrow `Expected-Paths`.
3. Write a failing regression first and record the expected RED reason before production behavior is added.
4. Implement the minimum GREEN behavior, run the focused regression, then the relevant broader static/build gates.
5. Push only the canonical branch, open one PR, remediate fixable current-lane failures on that same carrier, and require exact-head evidence.
6. Merge only through repository authorization/protected policy; never force, bypass, or replace a red carrier merely to escape CI.
7. Verify resulting `main`, close/release the reservation, and delete the merged branch where repository policy permits.
8. `LOCAL_ONLY` remains the only valid label for licensed BricsCAD V25/V26 interactive evidence that remote CI cannot produce.

---

## Child 1 — Harness Core

**Outcome:** deterministic, host-neutral routing/policy/lifecycle/trace kernel shared by every client.  
**Ownership-Key:** `qs3d-code.harness-core-v1`

### Files

Create:
- `src/QS3D.Core/Agent/Harness/HarnessSession.cs`
- `src/QS3D.Core/Agent/Harness/TaskIntent.cs`
- `src/QS3D.Core/Agent/Harness/TaskRouter.cs`
- `src/QS3D.Core/Agent/Harness/SkillDescriptor.cs`
- `src/QS3D.Core/Agent/Harness/SkillCatalog.cs`
- `src/QS3D.Core/Agent/Harness/SkillRouter.cs`
- `src/QS3D.Core/Agent/Harness/HarnessPermission.cs`
- `src/QS3D.Core/Agent/Harness/HarnessPolicy.cs`
- `src/QS3D.Core/Agent/Harness/HarnessLifecycle.cs`
- `src/QS3D.Core/Agent/Harness/HarnessTraceEvent.cs`
- `src/QS3D.Core/Agent/Harness/HarnessEngine.cs`
- `tests/QS3D.Core.SmokeTests/AgentHarnessCoreSmoke.cs`

Modify:
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`

### Contracts to implement

`TaskIntent` carries normalized requested outcome plus zero-or-more bounded domains: source, CI, GitHub carrier, MCP/transport, persistence/durability, BricsCAD host/runtime, release/package, CAD inspect, CAD mutate.

`TaskRouter` is deterministic and explainable: it returns classified domains plus compact evidence/reason labels. It may use bounded token/phrase matching in V1, but must not convert repository text into executable commands.

`SkillDescriptor` contains stable id/version, trigger domains, prerequisite skill ids, required canonical-doc paths, permitted tool classes, and validation expectations. `SkillRouter` performs deterministic scoring + prerequisite closure + stable ordering; duplicate ids and dependency cycles fail closed.

`HarnessPolicy` exposes `AUTO`, `CONFIRM`, `DENY`. V1 hard-denies secret export, force-push, ruleset/CI/reservation bypass, arbitrary out-of-workspace write, and untyped destructive external operations. CAD mutation is never AUTO merely because a task contains CAD words.

`HarnessLifecycle` validates only legal state transitions:
`CREATED -> CONTEXT_RESOLVING -> READY -> RUNNING -> WAITING_PERMISSION|WAITING_EXTERNAL -> RUNNING -> COMPLETED|BLOCKED|CANCELLED|FAILED`.

`HarnessTraceEvent` is observable execution fact only; no private reasoning field. Events have session id, sequence, event kind, UTC timestamp, summary, optional source identity and redacted metadata.

`HarnessEngine` composes router + skills + policy + lifecycle and returns a deterministic initial execution snapshot. It does not call a model, GitHub, shell, filesystem, or BricsCAD directly.

### TDD RED

Add `AgentHarnessCoreSmoke.Run()` and register it. RED assertions must cover at least:
- MCP save durability task classifies `McpTransport + PersistenceDurability` and routes repository/TDD/MCP/persistence skills through prerequisite closure;
- unrelated source task does not eagerly load MCP/CAD skills;
- duplicate skill id and prerequisite cycle fail closed;
- force-push/bypass/secret export resolve `DENY`;
- CAD mutation resolves at least `CONFIRM` until a typed operation/profile authorizes it;
- illegal lifecycle transition throws/fails closed;
- trace sequence is monotonic and trace payload has no reasoning/secret field.

Run and expect RED because `QS3D.Core.Agent.Harness` contracts do not yet exist:

```powershell
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

### GREEN

Implement the minimum contracts above without new external package dependencies and without file/network/host access.

Verify:

```powershell
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
python scripts/preflight-agent-lane-collision.py
python scripts/preflight-agent-reservation-v2.py
```

Commit production + regression together after RED has been demonstrated. Push, open PR, wait for exact-head protected gates, self-remediate only current-lane failures, merge, verify `main`.

---

## Child 2 — Repo-local `qs3d` CLI + repository skill packages

**Outcome:** executable developer client over the shared Core; progressive repository skill loading is real and inspectable.  
**Ownership-Key:** `qs3d-code.cli-skills-v1`

### Files

Create:
- `src/QS3D.Code.Cli/QS3D.Code.Cli.csproj`
- `src/QS3D.Code.Cli/Program.cs`
- `src/QS3D.Code.Cli/Qs3dCliApplication.cs`
- `src/QS3D.Code.Cli/RepositorySkillLoader.cs`
- `src/QS3D.Code.Cli/ConsoleTraceRenderer.cs`
- `tests/QS3D.Code.Cli.SmokeTests/QS3D.Code.Cli.SmokeTests.csproj`
- `tests/QS3D.Code.Cli.SmokeTests/Program.cs`
- `.agent/skills/repository-lifecycle/skill.yaml`
- `.agent/skills/tdd-source/skill.yaml`
- `.agent/skills/ci-remediation/skill.yaml`
- `.agent/skills/github-lifecycle/skill.yaml`
- `.agent/skills/mcp-transport/skill.yaml`
- `.agent/skills/persistence-durability/skill.yaml`
- `.agent/skills/bricscad-host/skill.yaml`
- `.agent/skills/cad-safety/skill.yaml`
- `.agent/skills/release-local-only/skill.yaml`

Do not add a third-party YAML runtime to `QS3D.Core`. The CLI repository adapter owns parsing of the deliberately restricted manifest schema and converts it into Core `SkillDescriptor` instances. Unknown keys, duplicate ids, paths escaping repository root, oversized manifests, malformed lists, or dependency cycles fail closed. If the strict manifest reader becomes larger than a bounded schema reader, switch the repository-owned manifests to an equivalently reviewable format only via a separate design update; do not silently add a general YAML execution surface.

### CLI V1 commands

```text
qs3d route "<prompt>"
qs3d run "<prompt>" --dry-run
qs3d run --skill <id> --dry-run
qs3d trace <session-id>
```

`route` prints classification, selected skills, canonical docs and permission classes. `run --dry-run` emits plan/trace facts but performs no GitHub, shell, filesystem mutation or CAD action. Mutating adapters remain future/host-specific boundaries.

### TDD RED

CLI smoke tests must fail before implementation and then cover:
- repository root discovery from a nested working directory;
- only required skill manifests loaded for an MCP durability prompt;
- path traversal (`../`) in required docs rejected;
- unknown/duplicate manifest id rejected;
- deterministic route output order;
- `--dry-run` performs no mutation and renders structured trace facts;
- no CLI option dumps environment variables or secret values.

RED command:

```powershell
dotnet run --project tests/QS3D.Code.Cli.SmokeTests/QS3D.Code.Cli.SmokeTests.csproj -c Release
```

GREEN verification:

```powershell
dotnet build src/QS3D.Code.Cli/QS3D.Code.Cli.csproj -c Release
dotnet run --project tests/QS3D.Code.Cli.SmokeTests/QS3D.Code.Cli.SmokeTests.csproj -c Release
dotnet run --project src/QS3D.Code.Cli/QS3D.Code.Cli.csproj -c Release -- route "fix MCP save durability and CI"
dotnet run --project src/QS3D.Code.Cli/QS3D.Code.Cli.csproj -c Release -- run "fix MCP save durability and CI" --dry-run
python scripts/preflight-agent-lane-collision.py
python scripts/preflight-agent-reservation-v2.py
```

Merge only after exact-head CI.

---

## Child 3 — BricsCAD Host Bridge + authenticated local IPC

**Outcome:** typed bridge between background harness clients and the active QS3D/BricsCAD host, with document affinity, cancellation and fail-closed mutation admission.  
**Ownership-Key:** `qs3d-code.bricscad-host-bridge-v1`

### Files

Create shared host source under V25 (automatically linked into V26 by the existing project pattern):
- `src/QS3D.BricsCAD.V25/Qs3dCodeHostBridge.cs`
- `src/QS3D.BricsCAD.V25/Qs3dCodeHostService.cs`
- `src/QS3D.BricsCAD.V25/Qs3dCodeLocalIpcServer.cs`
- `src/QS3D.BricsCAD.V25/Qs3dCodeHostContracts.cs`
- `scripts/preflight-qs3d-code-host-bridge.py`

Modify only as required:
- `src/QS3D.BricsCAD.V25/PluginEntry.cs`
- `src/QS3D.BricsCAD.V26/PluginEntry.cs`

Reuse existing document lifecycle, desktop-control/mutation admission, MCP/CAD runtime and shutdown patterns. Do not create a second independent CAD mutation authority.

### Bridge rules

- Resolve active document at execution time and attach a stable host/document identity to requests/results.
- Reject stale host/document/session identity before dispatch.
- Never expose live `DBObject`, `Transaction` or `ObjectId` across background/IPC boundaries.
- Typed read operations may include host status, active document identity, selection summary and diagnostics.
- Typed mutation requests carry operation id, document identity and permission class, then enter the existing CAD mutation/writer admission boundary.
- IPC is user-local only. Prefer named pipe/user-private state; no public network listener.
- Capability/session tokens are random local state and never committed/logged.
- Emergency stop prevents queued/new mutations from starting while preserving already-committing transaction semantics.

### TDD / remote verification

Because licensed BricsCAD assemblies/runtime are not general remote evidence, start with a static/source regression that is RED until the bridge exists and proves structural safety invariants. `scripts/preflight-qs3d-code-host-bridge.py` must assert both V25/V26 startup/cleanup wiring, no TCP listener in the local IPC class, typed operation identifiers, stale-document rejection path, no `DBObject`/`ObjectId` in serializable contracts, and reuse of the existing mutation admission path.

RED then GREEN:

```powershell
python scripts/preflight-qs3d-code-host-bridge.py
python scripts/preflight-agent-lane-collision.py
python scripts/preflight-agent-reservation-v2.py
```

Where licensed SDK references are available, also compile exact candidate source:

```powershell
dotnet build src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj -c Release -p:Platform=x64
dotnet build src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj -c Release -p:Platform=x64
```

If those licensed references are unavailable to the execution environment, report that build/runtime evidence as `LOCAL_ONLY`; do not replace it with fabricated PASS evidence.

### LOCAL_ONLY qualification

On exact candidate binaries in licensed V25 and V26:
- open host, verify background bridge startup does not block UI;
- inspect active document + selection;
- change documents and prove stale identity is rejected;
- authorize one bounded mutation and verify writer/mutation admission;
- exercise emergency stop before queued mutation starts;
- save/reopen if mutation durability is in the scenario;
- shut down and prove pipe/service cleanup leaves no stale listener/session.

---

## Child 4 — Embedded QS3D Code palette + Ribbon entry

**Outcome:** primary graphical V1 surface inside BricsCAD, driven by shared harness contracts and host bridge rather than a second router.  
**Ownership-Key:** `qs3d-code.embedded-ui-v1`

### Files

Create shared V25 source/UI (linked by V26):
- `src/QS3D.BricsCAD.V25/Qs3dCodePaletteCoordinator.cs`
- `src/QS3D.BricsCAD.V25/UI/Qs3dCodePanel.xaml`
- `src/QS3D.BricsCAD.V25/UI/Qs3dCodePanel.xaml.cs`
- `src/QS3D.BricsCAD.V25/Ribbon/Qs3dCodeRibbonAugmenter.cs`
- `scripts/preflight-qs3d-code-embedded-ui.py`

Modify only necessary integration points:
- `src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs`
- `src/QS3D.BricsCAD.V25/PluginEntry.cs`
- `src/QS3D.BricsCAD.V26/PluginEntry.cs`

V1 uses native WPF first. A WebView2 renderer is explicitly deferred unless an isolated follow-up carrier proves package/runtime compatibility; critical controls must never depend on a rich renderer.

### UI contract

Provide five logical workspaces: `Chat`, `Code`, `CAD`, `GitHub`, `Agents`. Initial implementation may render them as tabs with shared session/status models rather than fully featured editors.

Required always-native controls:
- current session/task status;
- host/document connection status;
- permission prompt surface;
- compact observable trace list;
- Cancel / Emergency Stop;
- reload/reopen surface.

The Ribbon button opens or focuses one palette instance. Repeated clicks must not create duplicate modeless windows. Palette disposal/shutdown follows existing coordinator patterns and document switching must not retain stale live CAD objects.

### RED / GREEN verification

`preflight-qs3d-code-embedded-ui.py` starts RED and then verifies source wiring: ribbon augmentation is registered/reset, one palette coordinator owns lifetime, five logical workspace labels exist, emergency stop is wired through the host service, no direct arbitrary BricsCAD mutation is performed from XAML/code-behind, and V26 receives the shared source through its existing link pattern.

```powershell
python scripts/preflight-qs3d-code-embedded-ui.py
python scripts/preflight-qs3d-code-host-bridge.py
python scripts/preflight-agent-lane-collision.py
python scripts/preflight-agent-reservation-v2.py
```

Compile V25/V26 where licensed references exist; otherwise keep compile/runtime qualification `LOCAL_ONLY` per repository policy.

### LOCAL_ONLY qualification

On exact V25/V26 candidate binaries:
- Ribbon `QS3D Code` opens/focuses the dockable/modeless palette;
- dock/float/resize/hide/reopen without duplicate windows;
- BricsCAD remains responsive while a background session is waiting;
- switching active DWG updates host/document identity;
- trace renders observable facts only, not hidden reasoning;
- permission prompt blocks mutation until resolved;
- Cancel/Emergency Stop prevents queued mutations;
- plugin unload/BricsCAD exit tears down palette + IPC cleanly.

---

## Child 5 — Provider/repository/process adapters and end-to-end execution loop

**Outcome:** turn the deterministic kernel from a planner/router into a usable coding-agent execution session while keeping model/provider/repository/process boundaries explicit.  
**Ownership-Key:** `qs3d-code.execution-adapters-v1`

This child starts only after Core + CLI + Host Bridge are merged so it binds to stable contracts. It must not hard-code API keys or provider credentials.

### Candidate files

Create under a new adapter boundary chosen from current main at carrier start:
- `src/QS3D.Code.Cli/Execution/IModelProvider.cs` or the equivalent shared Core provider interface if not already present;
- `src/QS3D.Code.Cli/Execution/RepositoryWorkspaceAdapter.cs`;
- `src/QS3D.Code.Cli/Execution/ProcessRunner.cs`;
- `src/QS3D.Code.Cli/Execution/SessionStore.cs`;
- `src/QS3D.Code.Cli/Execution/ExecutionCoordinator.cs`;
- dedicated adapter smoke tests.

Before mutation, narrow exact paths after inspecting then-current merged Core/CLI contracts and update the child Reservation-v2 Issue. Do not pre-reserve broad directories merely because this plan names candidate files.

### Execution contract

The coordinator implements observable stages, not hidden reasoning:
`resolve context -> route skills -> plan stages -> permission -> tool request/result -> validation -> external wait -> remediation loop -> terminal state`.

Repository writes are canonical-root-bound; process execution accepts executable + structured argument list rather than shell-concatenated untrusted text. GitHub mutation is performed only through an explicitly configured repository provider and only after carrier validity is established. Secrets are represented by non-exportable local references; environment dumps are forbidden.

A model adapter is pluggable. It may be absent in default tests; deterministic fake providers drive regressions. No third-party relay/provider is silently selected.

### TDD

Use deterministic fake model/repository/process providers to prove:
- task stage ordering;
- permission blocks tool dispatch;
- cancellation stops future dispatch;
- stale repo head forces context re-resolution rather than using old green evidence;
- process args remain structured;
- secret values are redacted from trace/session persistence;
- retry does not duplicate a completed stable-id mutation/tool operation;
- fixable CI failure re-enters remediation on the same carrier rather than creating a replacement carrier.

Exact test project/files are selected from then-current merged CLI structure and listed in the child Issue before mutation.

---

## Integration / release gate after all children

Do not collapse the child carriers into a mega-PR. When all implementation PRs are merged:

1. refresh `main` and run repository-level static/preflight suites required by changed paths;
2. run Core + CLI smoke tests from `main`;
3. verify the final Core API remains `netstandard2.0` and free of BricsCAD references;
4. verify V26 still shares intended V25 host/UI source and no V25 binary is used as V26 evidence;
5. verify no committed token/key/provider secret and no public IPC listener;
6. perform licensed V25/V26 end-to-end qualification locally on exact final candidate and attach only real evidence;
7. file bounded follow-up issues for renderer/provider enhancements rather than broadening already-merged carriers.

Expected remote-capable commands from final `main`:

```powershell
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
dotnet build src/QS3D.Code.Cli/QS3D.Code.Cli.csproj -c Release
dotnet run --project tests/QS3D.Code.Cli.SmokeTests/QS3D.Code.Cli.SmokeTests.csproj -c Release
python scripts/preflight-qs3d-code-host-bridge.py
python scripts/preflight-qs3d-code-embedded-ui.py
python scripts/preflight-agent-lane-collision.py
python scripts/preflight-agent-reservation-v2.py
```

The normal terminal state for repository implementation children is `MERGED_MAIN`. `BLOCKED` is valid only when no safe authorized action remains. `DUPLICATE_CARRIER` is valid when another active owner/session already owns the same semantic work.
