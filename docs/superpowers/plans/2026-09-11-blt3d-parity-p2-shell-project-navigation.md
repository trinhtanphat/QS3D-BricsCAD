# BLT3D Parity P2 Shell / Project Navigation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bind the already-qualified QS3D shell/workspace/Ribbon and Project/Zone/Floor/Family workflows into the P1 parity evidence model without rewriting the BricsCAD UI.

**Architecture:** Keep the current V25/V26-shared Ribbon/workspace implementation authoritative. P2 expands the checked-in parity manifest with granular shell/project rows, adds one host-neutral workflow catalog built on `FeatureId` + `ParityWorkflowRegistry`, and adds an auto-discovered source guard/runbook that ties those evidence claims to existing qualified guards. Evidence advances only to `CommandWired`; full V25/V26 parity remains intentionally unclaimed.

**Tech Stack:** C# `netstandard2.0`, existing `QS3D.Core.Features.FeatureId`, `ParityManifest`, `ParityWorkflowRegistry`, `QS3D.Core.SmokeTests`, Python preflight guards, UTF-8 TSV, BricsCAD V25/V26 shared source.

**Spec:** `docs/superpowers/specs/2026-09-10-blt3d-full-parity-design.md`

## Global Constraints

- `QS3D-BricsCAD` remains a Windows x64 BricsCAD-hosted plugin; no standalone CAD engine is introduced.
- Reuse existing Ribbon/workspace/project/family implementations; do not create duplicate UI or a second semantic project database.
- Reuse `QS3D.Core.Features.FeatureId`; do not create a second feature identity system.
- Keep `# catalog-complete=false`; P2 must not claim full BLT3D parity.
- P2 evidence is source-level `CommandWired`, not `V25V26ParityPass`.
- Zone/Floor/Family semantic mutation bindings require `ActiveDocument | Project | AtomicMutation | Audit`.
- `project.setup` is a bootstrap/infrastructure workflow and may run before a semantic Project exists; it must at least require an active document.
- No BLT3D binary/resource, credential, signing material, license algorithm, or runtime dependency is introduced.
- Merge only with fresh exact-head protected preflight + core GREEN, zero unresolved review threads, and no Reservation-v2 collision.
- No force push, bypass, or branch-protection override.

---### Task 1: Record honest P2 manifest evidence

**Files:**
- Modify: `tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs`
- Modify: `docs/BLT3D-PARITY-MANIFEST.tsv`

**Interfaces:**
- Consumes: `ParityManifestParser.Parse(IEnumerable<string>)`, `ParityManifest.GetRequired(FeatureId)`.
- Produces: manifest rows `shell.start`, `shell.workspace`, `shell.ribbon`, `project.setup`, `project.zone`, `project.floor`, `project.family` at `CommandWired`.

- [ ] **Step 1: Write the failing manifest smoke**

Add `P2ShellProjectEvidenceRules()` to `ParityManifestSmoke.Run()`. It must load the repository TSV, require the seven exact feature IDs, require each row's `WorkflowKey` to equal its canonical ID, require `EvidenceStage.CommandWired`, and assert `CatalogComplete == false`.

- [ ] **Step 2: Run to prove RED**

Run:
```powershell
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```
Expected: runtime failure because the P1 seed still has `shell.start` / `project.setup` at `ReferenceCaptured` and does not yet contain all five new granular P2 rows.

- [ ] **Step 3: Update the manifest minimally**

Keep the first line exactly `# catalog-complete=false`. Advance `shell.start` and `project.setup` to `CommandWired`; add `shell.workspace`, `shell.ribbon`, `project.zone`, `project.floor`, and `project.family` with matching workflow keys and `CommandWired`. Do not advance `bim.authoring` or unrelated domains.

- [ ] **Step 4: Run to prove GREEN**

Run the same deterministic Core smoke command. Expected: `ALL PASS`.

- [ ] **Step 5: Commit**

```bash
git add docs/BLT3D-PARITY-MANIFEST.tsv tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs
git commit -m "feat(parity): record P2 shell project evidence #6385"
```

---
### Task 2: Add the canonical P2 workflow catalog

**Files:**
- Create: `src/QS3D.Core/Features/ParityShellProjectNavigationCatalog.cs`
- Modify: `tests/QS3D.Core.SmokeTests/ParityWorkflowRegistrySmoke.cs`

**Interfaces:**
- Consumes: `FeatureId`, `ParityWorkflowBinding`, `ParityWorkflowRegistry`.
- Produces: `ParityShellProjectNavigationCatalog.CreateRegistry()` and seven canonical `FeatureId` fields.

- [ ] **Step 1: Write the failing workflow smoke**

Add `P2ShellProjectBindings()` to `ParityWorkflowRegistrySmoke.Run()`. It must require exactly seven bindings and verify:
- `shell.start`: `Infrastructure`, surfaces `Ui | Launcher`, requirements `None`.
- `shell.workspace`: `ReadOnly`, surface `Ui`, requirement `ActiveDocument`.
- `shell.ribbon`: `ReadOnly`, surface `Ui`, requirements `None`.
- `project.setup`: `Infrastructure`, surface `Ui`, requirement `ActiveDocument`.
- `project.zone`, `project.floor`, `project.family`: `SemanticMutation`, surface `Ui`, requirements exactly `ActiveDocument | Project | AtomicMutation | Audit`.

- [ ] **Step 2: Run to prove RED**

Run:
```powershell
dotnet build tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release --no-restore
```
Expected: compile failure because `ParityShellProjectNavigationCatalog` does not exist.

- [ ] **Step 3: Implement the minimal host-neutral catalog**

Create a static class in `QS3D.Core.Features` with seven canonical `FeatureId` values and `CreateRegistry()` returning exactly the seven bindings above. Keep all workflow keys equal to the normalized feature IDs; do not reference BricsCAD/Teigha/WPF types.

- [ ] **Step 4: Run to prove GREEN**

Run the build command, then the full deterministic Core smoke command. Both must pass.

- [ ] **Step 5: Commit**

```bash
git add src/QS3D.Core/Features/ParityShellProjectNavigationCatalog.cs tests/QS3D.Core.SmokeTests/ParityWorkflowRegistrySmoke.cs
git commit -m "feat(parity): bind P2 shell project workflows #6385"
```

---
### Task 3: Lock P2 source evidence and runbook

**Files:**
- Create: `docs/FEATURE-RUNBOOKS/blt3d-parity-p2-shell-project-navigation.md`
- Create: `scripts/preflight-blt3d-parity-p2-shell-project-navigation.py`

**Interfaces:**
- Consumes: checked-in manifest, P2 workflow catalog, parity smokes, existing P2 workspace/project/family preflight scripts.
- Produces: an auto-discovered fail-closed source guard for the P2 evidence contract.

- [ ] **Step 1: Write the runbook**

Document that P2 reuses the existing BricsCAD viewport/Ribbon/workspace implementation, enumerate the seven P2 parity IDs, record `CommandWired` as the maximum stage claimed by this carrier, and list the 12 baseline guards already exercised before implementation.

- [ ] **Step 2: Write the source guard**

The Python guard must fail unless:
- the manifest remains `# catalog-complete=false`;
- all seven exact rows exist at `CommandWired` with workflow key equal to FeatureId;
- the P2 catalog contains all seven IDs;
- Zone/Floor/Family catalog source contains `SemanticMutation`, `ActiveDocument`, `Project`, `AtomicMutation`, and `Audit` requirements;
- both parity smoke files invoke their P2 verification methods;
- the runbook records the existing 12 baseline guard names.

- [ ] **Step 3: Run the guard and existing baseline guards**

Run the new guard plus:
`preflight-blt3d-bim-workspace.py`, `preflight-blt3d-workspace.py`, `preflight-project-ribbon-actions.py`, `preflight-project-setup-floor-reference.py`, `preflight-family-manager-qs-quick-workflow.py`, `preflight-workspace-family-command-affinity.py`, `preflight-workspace-footer-context.py`, `preflight-workspace-document-context.py`, `preflight-project-floor-zone-canonical-reference.py`, `preflight-project-floor-zone-mutation-integrity.py`, `preflight-family-level-manager-publication.py`, and `preflight-family-level-manager-single-instance-veto-safe.py`.
Expected: 13/13 PASS.

- [ ] **Step 4: Commit**

```bash
git add docs/FEATURE-RUNBOOKS/blt3d-parity-p2-shell-project-navigation.md scripts/preflight-blt3d-parity-p2-shell-project-navigation.py
git commit -m "test(parity): lock P2 shell project evidence #6385"
```

---
### Task 4: Verify, freshen, publish, and merge P2

**Files:**
- Verify only: all seven reserved P2 paths.

**Interfaces:**
- Produces: one protected-main P2 merge with exact-head evidence; no extra feature scope.

- [ ] **Step 1: Run complete local verification**

Run `git diff --check`, the new P2 guard, all 12 baseline P2 guards, `dotnet build tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release`, and full deterministic Core smoke. Record terminal exit codes.

- [ ] **Step 2: Verify reservation scope**

Parse Issue #6385 `Expected-Paths`; compare against `git diff --name-only $(git merge-base HEAD origin/main)..HEAD`. Require zero paths outside the seven reserved paths.

- [ ] **Step 3: Freshen against protected main**

Fetch `origin/main`. If it advanced, merge it normally (`git merge --no-edit origin/main`), never rebase/force-push, and rerun the complete verification from Step 1.

- [ ] **Step 4: Push and open the PR**

Push the exact branch non-force. Open a PR that closes #6385 and repeats the Reservation-v2 identity/paths plus RED/GREEN/local verification evidence.

- [ ] **Step 5: Merge only after protected exact-head GREEN**

Require protected preflight + core terminal success, zero unresolved review threads, exact head unchanged, and current-main freshness/coordinator policy satisfied. Merge with an expected-head guard or allow the repository coordinator to merge automatically after the same protected gates.

- [ ] **Step 6: Post-merge verification**

Confirm #6385 is CLOSED / COMPLETED, the merge commit is reachable from `main`, all seven P2 paths are present, and `# catalog-complete=false` remains intact. Then begin P3 on a new Reservation-v2 carrier; never reuse P2 ownership for P3 files.
