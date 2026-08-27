# Preview 10228 Beam Behavior Matrix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** NETLOAD the official `v0.1.0-preview.10228` V25 package and prove or disprove M1-M8 of the Beam formwork behavior matrix with exact licensed-host evidence.

**Architecture:** Keep the released product DLL immutable. Retarget the existing `issue-4093` evidence carrier to the new official release identity, then use an ignored companion NETLOAD probe to execute the exact product `BeamFormworkQuantityPolicy` against a live diagonal native `Solid3d` in BricsCAD V25.2.10. The product policy supplies classification, rule gating, directed deductions, and aggregate projection; the companion emits sanitized per-cell values, while the outer run verifies host identity and restores scoped host state.

**Tech Stack:** PowerShell 5.1, .NET Framework 4.8/C#, BricsCAD V25 managed APIs, exact packaged `QS3D.BricsCAD.V25.dll` and `QS3D.Core.dll`, Git/GitHub CLI.

**Spec:** `docs/LOCAL-V25-BEAM-FORMWORK-MATRIX.md`

## Global Constraints

- Runtime candidate is exactly `v0.1.0-preview.10228` / `7dacdce17a6403d19681732ca7bad22cdb6f1499`.
- Official ZIP SHA-256 is `EC7385FC6085A838B94F84FC20B77E61E728952CC3A580FEC695031280FBC39E`.
- Packaged V25 adapter SHA-256 is `010F729470B0644CD0ECBFF7395F4DCFAE39E81AA1B230C7219AC18C11C1340A`.
- The released product DLL must not be rebuilt, replaced, patched, or instrumented.
- Host must be licensed interactive BricsCAD V25.2.10 x64 and start from zero pre-existing `bricscad.exe` processes.
- Expected M1-M8 values remain fixed: `7.0710678`, `9.1923881`, Top `0`, End/Other `0`, `6.7710678`, `8.8023881`, and Aggregate equals Detail within `1e-6 m2`.
- Raw paths, license data, registry material, private/customer DWGs, raw handles, and unrelated desktop content remain ignored/local.
- No write or merge to `main`; continue the existing `issue-4093` branch/PR and stop before merge.

---

### Task 1: Synchronize and retarget the existing evidence carrier

**Files:**
- Modify: `docs/LOCAL-V25-BEAM-FORMWORK-MATRIX.md`
- Modify: `docs/agent-work-claims/2026-08-26-gpt56sol-issue4093-preview10223-beam-matrix.md`
- Modify: `scripts/test-local-v25-beam-formwork-matrix-evidence.ps1`
- Create: `docs/superpowers/plans/2026-08-27-preview10228-beam-matrix.md`

**Interfaces:**
- Consumes: official GitHub release metadata and extracted package hashes.
- Produces: one fail-closed evidence verifier pinned to `.10228` plus a current runbook/claim on Lane-Key `issue-4093`.

- [x] **Step 1: Merge current `origin/main` into the canonical task branch**

Run: `git merge --no-edit origin/main`

Expected: merge succeeds on `agent/gpt56sol/issue-4093-beam-preview10223-matrix` without semantic conflict; no write to `main`.

- [x] **Step 2: Prove the retargeted verifier fails against the old `.10223` identity**

Run the current verifier against a strict UTF-8 fixture containing the old pinned tag/source/hash values and otherwise PASS-shaped M1-M8 data.

Expected after retarget: nonzero exit with `previewTag mismatch`.

- [x] **Step 3: Retarget immutable identity strings without changing the numerical contract**

Replace `.10223`, source `1363f9be...`, ZIP `A83BC92A...`, and DLL `3F0156A8...` with `.10228`, source `7dacdce1...`, ZIP `EC7385FC...`, and DLL `010F7294...`. Keep the `1e-6` tolerance and every M1-M8 expected value unchanged.

- [x] **Step 4: Run verifier contract/preflight checks**

Run: `python scripts/preflight.py`

Expected: all discovered repository gates PASS; no relaxed Beam value, identity, attestation, cleanup, or blocker check.

### Task 2: Build an ignored companion runtime probe

**Files:**
- Create ignored: `artifacts/issue4093/preview10228/probe/QS3D.BeamMatrix.V25.csproj`
- Create ignored: `artifacts/issue4093/preview10228/probe/BeamMatrixCommands.cs`
- Create ignored: `artifacts/issue4093/preview10228/run-beam-matrix-v25.ps1`

**Interfaces:**
- Consumes: exact packaged product/Core DLLs and installed BricsCAD V25 managed references.
- Produces: command `QS3DBEAMMATRIX10228` and a sanitized raw result for M1-M8.

- [x] **Step 1: Implement the companion command without copying product policy code**

The command creates one native 0.30 m x 0.50 m x `sqrt(50)` m `Solid3d`, rotates its longitudinal axis to `(5,5)`, enumerates live BREP face IDs/areas, seeds two directed Side deductions of `0.15 m2` and one Bottom deduction of `0.09 m2`, and invokes the exact loaded product assembly via reflection:

```csharp
Apply(Document, ProjectState, string, QuantityGeometryExplanation, QuantityCalculationRuleSet)
ApplyExactQuantity(ProjectElement, QuantityGeometryExplanation)
ReadLiveFaceKinds(Document, QuantityGeometryExplanation, ICollection<string>)
```

It records M1-M8 only from those in-host production method results and independently checks the native classification counts `Side=2`, `End=2`, `Top=1`, `Bottom=1`.

- [x] **Step 2: Build against net48 and exact runtime assemblies**

Run MSBuild on the ignored probe project with references to installed `BrxMgd.dll` / `TD_Mgd.dll` and the extracted `.10228` product/Core DLLs.

Expected: `0 warnings / 0 errors`; probe hash recorded locally.

- [x] **Step 3: Self-check the outer runner fail-closed paths before launch**

Run the runner with a deliberately wrong plugin digest and then with a fake pre-existing process marker.

Expected: both stop before BricsCAD launch and create no PASS evidence.

### Task 3: Execute the exact licensed V25 runtime matrix

**Files:**
- Generate ignored: `artifacts/issue4093/preview10228/runtime/raw-matrix.json`
- Generate ignored: `artifacts/issue4093/preview10228/runtime/sanitized-beam-matrix.json`

**Interfaces:**
- Consumes: exact `.10228` package and ignored companion probe.
- Produces: exact-host identity, M1-M8 numeric/classification results, and cleanup facts.

- [x] **Step 1: Snapshot and isolate scoped host state**

Require `bricscad.exe=0`; snapshot the V25 QS3D Loader/LoadCtrls/DemandLoad tree, `CurProfile`, profile inventory, and scoped QS3D UI state; use a nonce profile and prevent installed QS3D preload.

- [x] **Step 2: NETLOAD exact product, then companion, and run the matrix**

The BricsCAD script runs `NETLOAD` for `QS3D.BricsCAD.V25.dll`, NETLOADs the
companion, and runs `QS3DBEAMMATRIX10228`. The in-host companion verifies the
exact loaded product path, ProductVersion and SHA-256 before exercising M1-M8.
It intentionally does not call the UI-oriented `QS3DRUNTIMEPROBE`, because two
fail-closed attempts showed that opening the unrelated palette hit a host WPF
layout exception before the Beam command.

Expected: loaded product hash `010F7294...1340A`, BricsCAD `25.2.10` x64, and all eight cells exercised in the live host.

- [x] **Step 3: Restore and prove cleanup**

Close only the test-owned host, restore every snapshotted Loader/DemandLoad/profile/UI value, remove the nonce profile and disposable drawing, and sample until `bricscad.exe=0` remains stable.

Expected: all five cleanup booleans are true and `knownBlockers` is empty.

### Task 4: Verify, publish the task branch, and stop before merge

**Files:**
- Modify: `docs/agent-work-claims/2026-08-26-gpt56sol-issue4093-preview10223-beam-matrix.md`
- Optionally modify: `docs/LOCAL-V25-BEAM-FORMWORK-MATRIX.md` with sanitized exact-run evidence.

**Interfaces:**
- Consumes: sanitized runtime JSON and cleanup read-back.
- Produces: evidence-backed branch/PR handoff for `issue-4093`.

- [x] **Step 1: Run the strict verifier**

Run: `powershell -ExecutionPolicy Bypass -File scripts/test-local-v25-beam-formwork-matrix-evidence.ps1 -EvidencePath artifacts/issue4093/preview10228/runtime/sanitized-beam-matrix.json`

Expected for PASS: exactly `LOCAL_PASS / BEAM_BEHAVIOR_MATRIX` with preview `.10228`, source `7dacdce1...`, plugin `010F7294...`, BricsCAD `25.2.10`, and `cells=8`.

- [x] **Step 2: Re-fetch, inspect the final diff, and commit one coherent lane update**

Run: `git fetch origin main`, `git diff --check`, focused verifier/preflight tests, and `git diff origin/main...HEAD`.

Expected: only the current issue-4093 plan/runbook/verifier/claim summary is tracked; raw runtime artifacts remain ignored.

- [ ] **Step 3: Push/update PR #4094 and stop before merge**

Push only `agent/gpt56sol/issue-4093-beam-preview10223-matrix`, update Issue #4093/PR #4094 with the sanitized verdict, and do not merge.
