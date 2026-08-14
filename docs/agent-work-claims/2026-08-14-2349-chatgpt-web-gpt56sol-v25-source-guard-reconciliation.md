# Agent work claim — V25 source-guard reconciliation after reviewed refactors

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-v25-source-guard-reconciliation`
- Registered: `2026-08-14T23:49:00+07:00`
- Baseline main SHA: `b2e7aaa08bb8b0b817058005eba4cf53fbf3360b`
- Implementation branch: `agent/chatgpt-web-gpt56sol/v25-source-guard-reconciliation-20260814`
- Integration batch: `integration/chatgpt-web-gpt56sol-v25-source-guard-reconciliation-20260814`
- Priority: repair the latest `release-v25-cloud.yml` aggregate feature-gate failure on current reviewed source without weakening production contracts

## Reserved scope

Reconcile stale auto-discovered Python preflight/source guards with production and workflow contracts that have already been deliberately reviewed and integrated: centralized V25 host teardown, shared V25/V26 runtime-major diagnostics helper, reviewed FieldMerge coordinator exposure, commercial signed-only V25 release hardening, scalar revision ownership, preview-version validation helpers, updater lifecycle/order, signing EKU verification, and release publication/runtime integrity. Fix production code only if a guard exposes an actual current semantic regression rather than token/shape drift.

## Expected surfaces

- `scripts/preflight-auto-update.py`
- `scripts/preflight-bricscad-v26.py`
- `scripts/preflight-customer-release.py`
- `scripts/preflight-document-lifecycle.py`
- `scripts/preflight-interchange-field-merge-execution.py`
- `scripts/preflight-interchange-field-merge.py`
- `scripts/preflight-netload-existing-project-startup.py`
- `scripts/preflight-plan-to-3d-finish-workflow.py`
- `scripts/preflight-preview-release-diagnostics.py`
- `scripts/preflight-project-context-drawing-identity-touch-order.py`
- `scripts/preflight-project-tools.py`
- `scripts/preflight-reference-wall-ribbon.py`
- `scripts/preflight-release-asset-integrity.py`
- `scripts/preflight-release-signed-runtime.py`
- `scripts/preflight-ribbon-initialization-retry.py`
- `scripts/preflight-ribbon-quantity-reference-parity.py`
- `scripts/preflight-runtime-diagnostics-readonly.py`
- `scripts/preflight-signing-eku.py`
- `scripts/preflight-signing.py`
- `scripts/preflight-update-install-ux.py`
- `scripts/preflight-v25-host-lifecycle-native-readiness.py`
- `scripts/preflight-v25-preview-release-sync.py`
- current production/workflow/helper files read by those guards only when needed to distinguish stale guard shape from a real defect
- this claim file for coordination/closeout evidence

## Excluded scope

- No edit to `scripts/preflight-ui-production-polish.py` or `src/QS3D.BricsCAD.V26/PluginEntry.cs`; those are reserved by PR #1380.
- No edit to `.github/workflows/release-v25-cloud.yml`, `.github/workflows/ci.yml`, `scripts/test-v25-package-verifier.ps1`, or `scripts/verify-v25-package.ps1`; those are reserved by PR #1370.
- No unrelated feature, geometry, persistence, multicore, WPF shell, private-DWG, native UI, LOCAL_ONLY, or licensed-runtime work.
- No weakening/removal/skipping of aggregate gates merely to obtain a green run.
- No force-push or independent implementation landing to `main`.

## Validation plan

- Re-fetch current `main` and re-check overlapping ACTIVE/BLOCKED claims before implementation writes.
- For every failure from V25 cloud run #202 (`31819991099`), compare the failing guard with the exact current production/workflow contract and classify it as stale-guard drift or true defect.
- Prefer semantic/helper-aware assertions over exact whitespace/call-expression tokens when production intentionally centralizes lifecycle logic.
- Preserve fail-closed runtime-major, source/product/tag, signer/timestamp, publication integrity, FieldMerge authorization/native-cleanup, revision ownership, update lifecycle, and preview ordinal requirements.
- Publish one coherent implementation on the dedicated agent branch, reconcile against fresh `main`, assemble/review the dedicated integration candidate, then perform the single policy-compliant `main` landing.
- Use the owner-authorized fresh V25 cloud CI request as exact-SHA evidence; if the new run exposes another in-scope deterministic defect, diagnose/fix and continue the loop without reusing stale CI evidence.

## Coordination

Latest run #202 failed 23 auto-discovered feature guards before Core/V25 build. Current source already uses centralized `TeardownHostServices`/`TryCleanup`, `NativeRuntimeAssembliesMatch`, reviewed FieldMerge coordinator authorization, and the merged commercial signed-only V25 release design. PR #1370 and PR #1380 retain ownership of their exact overlapping surfaces and are not duplicated by this lane.

## Completion condition

The reserved stale guards/real defects are reconciled without weakening their safety intent; the implementation is represented in the reviewed integration result and current `main`; a fresh `release-v25-cloud.yml` run for the resulting exact current candidate advances past the repaired aggregate source-guard gate (and any newly exposed in-scope failure is handled in the same loop); actual CI evidence is recorded and this claim is then marked `COMPLETED` or explicitly `BLOCKED` only for a genuine external/non-repository gate.