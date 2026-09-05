# MCP Concurrent Transport Profile Registry Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a versioned, secret-free MCP transport profile registry that can represent multiple simultaneously enabled OpenAI/Cloudflare transport profiles while preserving legacy provider preference and the existing single-writer CAD boundary.

**Architecture:** Add a focused registry class with atomic line-oriented persistence and legacy migration. Keep `McpTransportCoordinator.SelectedProvider` as a compatibility/UI preference only; this carrier does not yet replace single-process provider supervisors. Source guards model multiple enabled profiles and ensure no CAD writer coordination is changed.

**Tech Stack:** C# V25 source shared by V26, Python preflight source guards, GitHub Actions hosted CI.

**Spec:** `docs/superpowers/specs/2026-09-02-mcp-multi-transport-registry-design.md`

## Global Constraints

- One embedded loopback MCP remains canonical.
- `McpCadMutationCoordinator` must not be modified.
- Registry must persist no API keys, bearer tokens, OAuth credentials, tunnel credentials, writer tokens, or diagnostic output.
- Legacy `provider.txt` behavior remains available as preferred UI provider compatibility.
- Hosted CI is source/build evidence only; live concurrent tunnel runtime qualification remains LOCAL_ONLY.

---

### Task 1: RED multi-profile registry source guard

**Files:**
- Create: `scripts/preflight-mcp-multi-transport-registry.py`

**Interfaces:**
- Consumes: current singleton `McpTransportCoordinator` source.
- Produces: deterministic source/model regression guard for registry behavior.

- [ ] **Step 1: Write the failing guard**

Require `McpTransportProfileRegistry.cs`, schema version 1, profile ID validation, atomic temp-file persistence, legacy migration, per-profile registration acknowledgement, secret-free status, and compatibility wording that `SelectedProvider` is a preferred UI provider rather than exclusive owner. Include a small Python model proving two OpenAI profiles plus one Cloudflare profile may all be enabled concurrently.

- [ ] **Step 2: Run CI and verify RED**

Run through the PR exact-head workflow. Expected: reservation/generic guards pass, `All discovered feature source guards` fails because the registry source and compatibility semantics do not exist yet.

- [ ] **Step 3: Commit**

Commit message: `test(mcp): require concurrent transport profile registry`.

### Task 2: Implement registry and legacy migration

**Files:**
- Create: `src/QS3D.BricsCAD.V25/McpTransportProfileRegistry.cs`
- Create: `docs/FEATURE-RUNBOOKS/mcp-multi-transport-registry.md`

**Interfaces:**
- Produces: `McpTransportProfile`, `McpTransportProfileRegistry.LoadProfiles()`, `EnsureLegacyDefaultProfile()`, `UpsertProfile()`, `RemoveProfile()`, `SetRegistrationAcknowledged()`, `IsRegistrationAcknowledged()`, and `StatusJson()`.

- [ ] **Step 1: Implement minimal model/validation**

Use 32-lowercase-hex profile IDs, bounded display names, provider enum validation, enabled/autostart/default booleans, and non-secret registration identity.

- [ ] **Step 2: Implement atomic persistence**

Write versioned line records to a temporary file in `%APPDATA%/QS3D/MCP/Transport`, flush/close, then replace/move into `profiles-v1.txt`. Unknown schema versions fail closed and are not overwritten.

- [ ] **Step 3: Implement legacy migration**

When no registry exists, create exactly one legacy-default profile from a migration-only provider resolver while leaving all existing provider metadata and secret stores untouched.

- [ ] **Step 4: Implement bounded status/registration acknowledgement**

Status must include only profile IDs/providers/booleans/sanitized display names and bounded error text. Registration acknowledgement is keyed by profile ID + non-secret identity.

- [ ] **Step 5: Document source and LOCAL_ONLY contracts**

Document that registry capability does not yet mean multiple provider processes are live; follow-up carriers own process supervisors/UI/stress qualification.

- [ ] **Step 6: Commit**

Commit message: `feat(mcp): add versioned transport profile registry`.

### Task 3: Convert singleton ownership semantics to UI preference semantics

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/McpOpenAiSecureTunnel.cs`

**Interfaces:**
- Consumes: `McpTransportProfileRegistry.EnsureLegacyDefaultProfile()`.
- Preserves: `SelectedProvider`, `SelectedProviderLabel`, `SetSelectedProvider`, and legacy onboarding call sites.

- [ ] **Step 1: Update coordinator comments/semantics**

Define `SelectedProvider` as preferred UI/onboarding provider, not transport ownership. Ensure selection changes no longer imply that other profiles must be disabled in registry state.

- [ ] **Step 2: Wire migration initialization**

After loading the legacy preference without recursion, ensure the default registry profile exists. Do not start additional provider processes in this carrier.

- [ ] **Step 3: Keep legacy autostart compatibility explicit**

Keep `TryAutoStartPreferred()` behavior for existing installs and state in code/comments that follow-up supervisor carriers replace it with enabled-profile autostart.

- [ ] **Step 4: Commit**

Commit message: `refactor(mcp): make selected transport a UI preference`.

### Task 4: GREEN verification and PR completion

**Files:**
- Verify all files in this carrier only.

**Interfaces:**
- Produces: exact-head hosted evidence for #5299.

- [ ] **Step 1: Run exact-head CI**

Expected: reservation gate, generic guard, all discovered feature guards, Core build/smokes, trusted V25 reference validation, and locked-reference V25 plugin build all pass where scheduled.

- [ ] **Step 2: Inspect PR diff**

Confirm `McpCadMutationCoordinator.cs` is absent from changed files and no secret values/paths are introduced.

- [ ] **Step 3: Reconcile latest main if needed**

If main advanced, reconcile according to repo policy and rerun exact-head CI on the new head.

- [ ] **Step 4: Merge only on green exact head**

Update issue/PR with source-ready vs LOCAL_ONLY runtime evidence. Do not claim concurrent live tunnels are complete from this foundation carrier alone.