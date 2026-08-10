# QS3D-BricsCAD — continue-all deep audit, hardening plan and implementation record

Audit date: 2026-08-10 (UTC+7)

This document records the full-project source review performed against the fast-moving `main` branch. It separates verified source fixes from capabilities that still require a licensed BricsCAD V25 runtime/private-DWG proof. GitHub Actions were intentionally not dispatched during this review because repository policy requires explicit owner approval for manual workflows.

## 1. Review plan and priority model

The audit was executed in risk order rather than UI order:

1. **P0 build / data-loss blockers** — duplicate compiled types, missing compile wiring, destructive CAD ownership, non-transactional project/install mutation.
2. **P0/P1 persistent integrity** — stored material metadata, semantic dependency graphs, project-owned object identity and recoverable dangling references.
3. **P1 native generated geometry** — Foundation/Slab/Wall/Curtain/Rebar ownership, stale state, live-solid checks, allocation limits and finite CAD math.
4. **P1 reporting / release readiness** — quantity fallback behavior, ownership parity, dependency-cycle release blocking and material/export correctness.
5. **P1 supply chain** — package hashing, Authenticode, version binding, replay/downgrade resistance and installer rollback.
6. **P1 modeless UI safety** — drawing-bound editors must not mutate a different DWG after the active document changes.
7. **P2 source/product boundaries** — command/docs parity, placeholder scan and features that must not be faked without real V25/private-DWG evidence.

The working rule throughout was fail-closed for ambiguous/destructive ownership, preserve recoverability for legacy semantic references, and never overwrite concurrent `main` changes with a stale snapshot.

## 2. Verified fixes merged during this deep pass

### PR #26 — single generated-ownership health facade

A real Core build blocker existed: two SDK-globbed source files declared the same public `GeneratedHandleOwnershipHealthService` type.

Implemented:

- kept one canonical public facade;
- routed it through the safe policy-driven ownership scanner;
- removed the duplicate type file;
- added regression coverage proving unrelated metadata such as `PreviewHandle` is not treated as a generated owner slot;
- hardened ownership preflight against duplicate facade reintroduction.

Squash: `221c2c27…`.

### PR #27 — compile the canonical facade directly

After removing the old compatibility shim, an obsolete `Directory.Build.targets` still excluded the canonical health facade from Core compilation. This was a second build blocker.

Implemented:

- removed the obsolete compile exclusion;
- required normal SDK compile inclusion;
- rewrote the former shim preflight so duplicate shims/exclusions fail the gate.

Squash: `ea257b85…`.

### PR #28 — Foundation Mesh ownership health

The native Foundation builder already had guarded selection, finite math, batch caps, ownership-before-erase and post-commit metadata. Its dedicated health service, however, used first-owner-wins logic and a hard-coded generated-slot list.

Implemented:

- order-independent `Owners + Conflicts` ownership index;
- shared `GeneratedHandleOwnershipPolicy.IsOwnerSlot` discovery;
- SourceHandles remain first-class owners;
- future generated owner slots are protected automatically;
- regression where Foundation owns a handle first and a later `GeneratedFutureMeshHandles` slot claims the same handle.

Squash: `b8013301…`.

### PR #29 — persisted Material Catalog fails closed

Normal custom-material writes rejected built-in ID/name collisions, but legacy/tampered persisted metadata could bypass that validation and shadow built-ins during catalog merge/grouping.

Implemented:

- one `EnsureDoesNotShadowBuiltIn` guard used on both write and read paths;
- persisted built-in ID collision rejection;
- persisted built-in name collision rejection;
- raw-metadata smoke tests and focused integrity preflight.

Squash: `8c56f04d…`.

### PR #30 — dependency cycles are health/release blockers

Dependency cycles previously surfaced only when regeneration attempted a topological order. A project could therefore appear healthy and fail later during regeneration.

Implemented:

- iterative, non-recursive `DependencyHealthService`;
- `DEPENDENCY_SELF_REFERENCE`;
- `DEPENDENCY_CYCLE` for exact cycle members;
- downstream dependents are not falsely labelled as cycle members;
- missing dependency targets remain the existing Model Health recovery responsibility;
- service remains inspectable even on corrupt in-memory duplicate element IDs;
- `QS3DHEALTHALL` and `QS3DRELEASECHECK` now include dependency health.

Squash: `0ca137b2…`.

### PR #31 — updater version is bound to the signed plugin

The update pipeline already checked HTTPS, package host, ZIP hash/limits/path traversal, SHA256SUMS and Authenticode signatures. The remaining replay/downgrade gap was that manifest/package metadata version fields were unsigned. An old but still-validly-signed executable payload could theoretically be relabelled with a newer unsigned version.

Implemented:

- Authenticode verification occurs before version trust;
- updater reads `QS3D.BricsCAD.V25.dll` assembly version from the verified signed assembly;
- manifest version must equal the signed plugin assembly version;
- package metadata version must equal the signed plugin assembly version;
- manifest generation derives published version from the signed staged plugin;
- signed-package finalization rejects metadata/plugin mismatch before rebuilding the ZIP;
- preflight enforces signature → signed version → metadata → installer ordering.

Squash: `cbce6257…`.

### PR #32 — transactional V25 installer rollback

Payload replacement could succeed before a later BricsCAD DemandLoad registry write failed, leaving a new payload plus partial registration.

Implemented:

- snapshot each targeted `Applications\\QS3D` registration before mutation;
- preserve Loader, LoadCtrls, Description and Commands values;
- rollback registry targets in reverse order;
- restore previous payload backup on upgrade failure;
- remove newly committed payload on fresh-install failure;
- report rollback failures without hiding the original install error;
- preflight verifies snapshot-before-mutation and rollback-before-rethrow ordering.

Squash: `55f168e3…`.

### PR #34 — modeless project editors are active-DWG safe

Floor Level already required its bound document to remain active before mutation. Zone, Family and Material windows could still mutate their originally bound Project while the user had switched to another DWG.

Implemented:

- Zone Save/Delete/Activate/Assign/Inspect active-DWG guards;
- Family Duplicate/Rename/Delete/property mutation/Activate/Assign active-DWG guards;
- Material Save/Delete/Apply/Export active-DWG guards;
- Refresh/New remain UI-only and available;
- focused preflight checks each mutation handler guards before resolving or mutating the project/CAD state.

Squash: `fbfef5dd…`.

## 3. Concurrent hardening preserved from other agents

A concurrent full-repository batch was preserved instead of overwritten. It centralizes generated ownership in Core and improves semantic capture/release safety:

- `GeneratedHandleOwnershipPolicy` is the shared source of truth for `PhysicalOpeningCutSolidHandle` plus `Generated*Handle` / `Generated*Handles` slots;
- semantic capture rejects QS3D-generated output as new semantic input;
- single and multi-selection capture are transactional;
- room-finish batches restore project state on failure;
- semantic selection, B4D exclusion, ownership health, BOM release and Release Readiness consume the same ownership contract;
- future generated owner slots are protected without maintaining parallel family lists.

The destructive Rebar and Curtain guards were re-audited after this merge. They dynamically protect foreign/future owner slots through the Core policy while granting erase authority only to their own explicitly supported output slots. No additional destructive-ownership defect was verified there.

## 4. Intentional non-changes

### Recoverable dangling semantic references

`.qsdb` loading intentionally remains permissive for dangling Family/Floor/Zone/Active/DependsOn references so older or partially damaged projects can still open and be repaired through Health/UI. `ModelHealthService` reports the corresponding missing/invalid references. These were **not** converted to hard load failures.

Irrecoverable structural corruption such as duplicate IDs, invalid numbers, malformed XML/catalog records and unsafe persistence boundaries remains fail-closed.

### Wall junction geometry

Physical multi-owner L/T/X/Multi wall-solid union/reconciliation is not implemented by guessing. The current safe path remains source-centerline planning/snap plus ownership-aware invalidation/rebuild.

### Curtain geometry

Curved/open-POLYLINE native frame overlays and panel-by-panel backing glass remain explicit product/runtime work. No fake straight-frame approximation was introduced.

### Rebar detailing

Current generated bars/loops/mesh are bounded semantic/native geometry, not a claim of fabrication-grade hooks, bend radii, anchorage or jurisdiction-specific code detailing without explicit configured rules.

## 5. Source-level completion findings

The current indexed repository scan found no `NotImplementedException` and no source `TODO` placeholders. This does **not** mean the product is runtime-certified; it means no obvious placeholder implementation was found in the indexed source during this pass.

Command/reference documentation is substantially aligned with the current command surface. Packaging generates `COMMANDS.txt` from source `[CommandMethod]` declarations, reducing manual command-manifest drift.

Current source preflights now include focused gates for:

- canonical generated-ownership compilation and shared policy;
- Foundation ownership ordering/future slots;
- persisted Material Catalog integrity;
- dependency self/cycle health and release blocking;
- secure updater signed-version binding;
- transactional installer rollback;
- modeless Floor/Zone/Family/Material active-DWG mutation safety;
- existing geometry, recognition, schedules, ownership, release-readiness and manual-only workflow contracts.

`scripts/preflight-all.py` auto-discovers `preflight-*.py` gates.

## 6. Supply-chain status after hardening

The updater/installer source path now includes:

- HTTPS-only update manifest/package path;
- package-host allowlist;
- ZIP size, expanded-size, entry-count and traversal guards;
- SHA-256 package and per-file verification;
- Authenticode signer pinning for executable payloads;
- signed plugin assembly-version binding to manifest/package metadata;
- downgrade/same-version policy;
- BricsCAD-process closure requirement before update/install;
- transactional payload + DemandLoad registry rollback;
- no SECURELOAD weakening.

What remains is operational proof/provisioning, not an excuse to label the current head production-signed without evidence: production code-signing certificate/key custody, timestamp/publication infrastructure and a real signed-package exercise still need an explicitly approved environment.

## 7. Validation boundary

This continue-all pass does **not** claim that the final `main` head has been compiled or executed in licensed BricsCAD V25. The available execution environment cannot provide the exact installed V25 managed assemblies/runtime, and GitHub Actions were not dispatched because the repository is manual-only by policy.

Before calling a final SHA V25-certified, run the owner-approved gate on that exact SHA:

1. compile Core and `QS3D.BricsCAD.V25` Release/x64/net48 against installed BricsCAD V25 assemblies;
2. run Core smoke + aggregate Python preflights;
3. DemandLoad/NETLOAD in licensed BricsCAD V25;
4. command, Ribbon, palette and modeless multi-DWG regression;
5. Unicode/HiDPI 100/125/150/200% UI checks;
6. native wall/opening/Curtain/structure/rebar/Slab/Wall/Foundation geometry checks;
7. representative private-DWG save/reopen/reload regression;
8. schedule/XLSX/BBS/BQ/Release Check traceability;
9. signed package/install/update/rollback exercise when production signing infrastructure is available.

Source-level review can make this gate safer and more deterministic, but it cannot substitute for the real BricsCAD runtime.

## 8. Merge discipline used

All reviewed changes were made on focused branches, compared against moving `main`, then squash-merged server-side without force. Concurrent changes were preserved. When a snapshot or connector operation became stale, the patch was re-read/reconciled rather than overwriting newer work.

GitHub Actions were not dispatched in this continue-all implementation pass.
