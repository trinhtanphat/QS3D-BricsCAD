# V26 MSI Execution Copy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Keep the canonical BricsCAD V26 MSI generation held and immutable while preventing that held file handle from interfering with Windows Installer administrative extraction.

**Architecture:** `acquire-v26-compile-references.ps1` continues to admit and hold the canonical MSI exactly as today. When reference extraction is requested, it materializes a fresh execution-only MSI from the held admitted bytes, verifies that copy against the admitted SHA-256 before and after extraction, invokes `msiexec /a` only against the execution copy, and always removes the execution copy. Existing extracted-tree and exit-1603 validation remains fail-closed.

**Tech Stack:** Windows PowerShell 5.1, Windows Installer (`msiexec.exe`), Python 3 source preflight guards, GitHub Actions.

**Spec:** GitHub issue #5864.

## Global Constraints

- Preserve the pinned BricsCAD V26.2.07 MSI SHA-256, Authenticode signer, product identity, and canonical held-generation checks.
- Do not weaken `Get-CompleteV26ReferenceDirectory` or the existing rule that exit 1603 is tolerated only after a complete managed-reference payload exists.
- `msiexec.exe` must never receive `$admission.Path` while `$admission.Stream` is held open.
- The execution copy must be created only from `$admission.Stream`, must be an ordinary non-reparse file, must match `$admission.Sha256` before and after extraction, and must be deleted in `finally` cleanup.
- Keep changes limited to issue #5864 Expected-Paths.

---

### Task 1: Add the regression guard

**Files:**
- Create: `scripts/preflight-v26-msi-execution-copy.py`
- Test: `scripts/preflight-v26-msi-execution-copy.py`

**Interfaces:**
- Consumes: `scripts/acquire-v26-compile-references.ps1` source text.
- Produces: a fail-closed source guard that rejects invoking `msiexec` against the held canonical MSI and requires the execution-copy lifecycle.

- [ ] **Step 1: Write the failing test**

Create a source guard that requires literals/patterns for `$executionMsi`, byte publication from `$admission.Stream`, exact digest validation, `msiexec` arguments referencing `$executionMsi`, post-extraction digest validation, and cleanup. It must explicitly fail if the administrative-extraction argument block contains `$admission.Path`.

- [ ] **Step 2: Run test to verify it fails**

Run: `python scripts/preflight-v26-msi-execution-copy.py`
Expected: FAIL because current production code invokes `msiexec` using `$admission.Path` and has no execution-copy lifecycle.

- [ ] **Step 3: Commit the RED guard**

Commit only the new guard (plus this already-landed plan) before changing production code.

### Task 2: Materialize and verify an unlocked execution copy

**Files:**
- Modify: `scripts/acquire-v26-compile-references.ps1` extraction block.
- Test: `scripts/preflight-v26-msi-execution-copy.py`
- Test: `scripts/preflight-v26-msi-admin-extract-1603.py`

**Interfaces:**
- Consumes: `$admission.Path`, `$admission.Stream`, `$admission.Sha256`, and existing extraction directory validation.
- Produces: `$executionMsi`, a fresh MSI file containing exactly the held admitted bytes and used only for `msiexec /a`.

- [ ] **Step 1: Create the execution copy from held bytes**

Inside `if ($ExtractReferences)`, choose a fresh temporary `.msi` path outside the extraction directory, validate the path with `Assert-NoExistingReparseComponent`, create it with `FileMode.CreateNew` / `FileShare.None`, reset `$admission.Stream.Position = 0`, copy the held bytes, flush to disk, then reset the held stream position.

- [ ] **Step 2: Verify the execution copy before use**

Use `Get-OrdinaryFileOrNull` plus `Get-FileHash -Algorithm SHA256` to require an ordinary file whose length equals `$admission.Length` and whose SHA-256 equals `$admission.Sha256` before `msiexec` starts.

- [ ] **Step 3: Invoke Windows Installer against the execution copy only**

Build the administrative-extraction argument array with `('"' + $executionMsi + '"')`, never `$admission.Path`. Preserve `/a`, `/qn`, `TARGETDIR`, `REBOOT=ReallySuppress`, verbose logging, timeout handling, and exit-code policy.

- [ ] **Step 4: Verify after extraction and clean up**

After the process exits and before accepting output, verify the execution copy again for ordinary-file identity, length, and exact SHA-256. Remove it in a `finally` block regardless of success/failure. Keep `Assert-HeldInstallerStable -Held $admission` before and after extraction so the canonical admitted generation remains locked and verified throughout.

- [ ] **Step 5: Run focused guards**

Run:
- `python scripts/preflight-v26-msi-execution-copy.py`
- `python scripts/preflight-v26-msi-admin-extract-1603.py`

Expected: both PASS.

- [ ] **Step 6: Run aggregate source preflight**

Run: `python scripts/preflight-all.py`
Expected: PASS for all discovered feature source guards.

### Task 3: Exact-head CI and merge gate

**Files:**
- No additional production files.

**Interfaces:**
- Consumes: issue #5864 carrier head and repository CI.
- Produces: exact-head evidence that the permanent source guards and normal shared CI are green before merge.

- [ ] **Step 1: Open one draft PR for the canonical carrier after the RED guard commit**

Expected: CI demonstrates the new guard fails against the current production implementation.

- [ ] **Step 2: Push the production fix to the same carrier**

Expected: the same PR receives a fresh exact-head CI run.

- [ ] **Step 3: Verify exact-head GREEN**

Require Reservation-v2, the new execution-copy guard, aggregate feature source guards, and normal core/shared checks to pass at the final head.

- [ ] **Step 4: Merge only the verified exact head**

Use `expected_head_sha` when merging. Close issue #5864 after verifying the merge landed on current `main`.

- [ ] **Step 5: Runtime acceptance**

The next manually dispatched `QS3D Cloud V26 Preview Build & Release` must demonstrate that `v26-reference-primary` or its fallback now gets past administrative extraction on the merged SHA. If that workflow is not dispatched in this session, do not claim runtime extraction PASS; claim only source/CI completion.