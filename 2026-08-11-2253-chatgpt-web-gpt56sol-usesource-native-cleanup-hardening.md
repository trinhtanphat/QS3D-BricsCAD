# Work claim — UseSource native cleanup/backing-store hardening

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T22:53:00+07:00`
- Baseline main SHA: `78bb8e795606f0d84db5268c42c5904e74e628ed`
- Priority: evidence-driven remote-safe interoperability hardening for issue #84

## Reason

Issue #84's latest source-hardening checkpoint explicitly identifies a residual gap: `UseSource` Element/Catalog/All still need the same affected-target native-cleanup coverage and mid-transaction backing-store rechecks already used by FieldMerge. Today those three services compute an invalidation closure and prepare destructive native erasure, but do not fail closed on unsupported generated owner slots and do not repeat backing-store authority checks around native prepare, semantic apply, and CAD commit.

## Reserved scope

Harden the three existing `UseSource` mutation services without changing their semantic collision policies or UX:

- validate `GeneratedNativeCleanupCoverageGuard` against each service's exact computed invalidation closure before destructive work and repeat the check under the document lock;
- rebind the exact canonical project and re-resolve affected element IDs under the document lock instead of carrying pre-lock element references into destructive native work;
- recheck authoritative `.qsdb` backing-store revision before native cleanup, after native prepare/before semantic mutation, and after semantic mutation/metadata cleanup before CAD commit;
- preserve rollback-safe behavior and existing explicit rebuild boundary;
- add an auto-discovered static preflight that guards the required ordering across all three services.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Services/InterchangeUseSourceElementImportService.cs`
- `src/QS3D.BricsCAD.V25/Services/InterchangeUseSourceCatalogImportService.cs`
- `src/QS3D.BricsCAD.V25/Services/InterchangeUseSourceAllImportService.cs`
- `scripts/preflight-interchange-usesource-native-cleanup-hardening.py`
- this claim file

## Excluded scope

- No new FieldMerge policy/mode and no change to the reviewed field-precedence contract.
- No target-DWG adoption/rebinding of source CAD handles.
- No automatic Build3D/opening/rebar/curtain/grid rebuild, save, IFC/Revit/BCF/vendor format work, or UI redesign.
- No claim of licensed BricsCAD V25 runtime qualification.
- No GitHub Actions dispatch or release workflow.

## Validation plan

- Static/source review proves each service uses its own computed invalidation closure for cleanup coverage.
- Static/source review proves the destructive sequence is: document lock -> canonical project rebind/re-resolve -> locked coverage check -> backing-store recheck -> native prepare -> backing-store recheck -> semantic apply/metadata cleanup -> backing-store recheck -> CAD commit.
- Rollback snapshot is captured from the locked canonical project and remains available until native commit.
- Add an auto-discovered preflight that fails if any of the three services loses those guards/orderings.
- Re-fetch current `main` and all target blobs before writing; never force-push.
- Record source/static verification only; do not claim an executed licensed V25 run in this hosted session.

## Coordination

Recent active claims cover Material Catalog UTF-8, quantity-rule variables, revision IDs, semantic-tag PICKFIRST, updater locking, Auto Room scope and unrelated domains. No current claim/recent commit was found reserving the residual #84 UseSource native-cleanup/backing-store hardening lane.

## Completion evidence

- PR #518 merged to `main` as `b5b31697288faab398fbc384cc205d4d1fbfd92e`.
- `UseSource` Element/Catalog/All now perform cleanup-coverage checks before destructive work and repeat them against the exact recomputed closure under `DocumentLock`.
- Each mutation rebinds the exact reviewed canonical project under the lock, recomputes affected targets there, and captures semantic rollback from that locked project before native preparation.
- Each path explicitly rechecks backing-store authority at pre-native cleanup, pre-semantic apply, and pre-CAD commit phases.
- Result invalidation counts are taken from the locked native invalidation plan rather than a pre-lock closure.
- Added auto-discovered static guard `scripts/preflight-interchange-usesource-native-cleanup-hardening.py` covering the ordering and fail-closed contracts across all three services.
- Post-merge `main` blob verification: Element `ffa755b985cfcb4b2725d87ab07f510f6706bcc0`, Catalog `1d13db513700fabda40d05c5e960f55b126a1eef`, All `d33b7ec066a189825b56a3ba9b5a067326d35c9e`, preflight `6792b2f3f4e4918eaf3d38786cac9c838526194f`.
- No force-push was used. Direct fast-forward attempts that raced concurrent `main` writes were safely rejected; integration completed through the isolated PR branch while preserving concurrent commits.
- No GitHub Actions or release workflow was manually dispatched. This hosted session did not execute a licensed BricsCAD V25 runtime; runtime qualification remains LOCAL_ONLY.

## Completion condition

Completed: all three UseSource mutation paths fail closed on unsupported generated native owner slots, detect mid-transaction sidecar replacement before semantic/CAD commit, avoid carrying pre-lock mutable element references into destructive work, and include focused static regression coverage on `main`.