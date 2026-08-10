# QS3D-BricsCAD — current agent handoff (2026-08-10 17:10 UTC+7)

**Repository:** `trinhtanphat/QS3D-BricsCAD`  
**Canonical branch:** `main`

This is the newest short current-state delta for fast-moving work. Fetch `main` before every write. If current source conflicts with this note, current source wins.

Read this together with `docs/AGENT-HANDOFF-LATEST-2026-08-10.md`, `docs/LOCAL-V25-QUALIFICATION.md`, `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md` and `docs/LOCAL-AGENT-OPEN-WORK-ADDENDUM-2026-08-10.md`.

## 1. Product boundary

QS3D remains a clean-room **BricsCAD V25 x64 .NET plugin**. BricsCAD owns the DWG/editor/viewport/native CAD lifecycle. Do not create a standalone QS3D CAD application or duplicate BricsCAD runtime assemblies.

`BLT-like` means workflow/UX familiarity and independently implemented quantity/semantic/native behavior, not proprietary source/assets.

## 2. New source capability in the latest continue-all review

### Dependency-safe semantic untrack

`QS3DUNTRACK` / `QS3DUNTRACKFINISH` now resolve both source and generated selections through canonical semantic ownership, reject ambiguous ownership and block removal while transitive semantic dependents remain outside the selected batch. A complete dependent batch can be untracked together. CAD geometry remains untouched by untrack.

### Grid semantic reference capture

New command:

```text
QS3DGRID
```

Current contract:

- selection-only semantic capture;
- accepts only `LINE` / `ARC` sources with finite positive length;
- validates the whole selection before semantic mutation;
- reuses transactional `SemanticCaptureService` and canonical generated-output rejection;
- reuses existing `ElementCategory.Grid` + `GenericTakeoffRegenerator` (`LengthM`, `Count`);
- post-capture Workspace/UI refresh is non-fatal and must not turn a valid committed semantic capture into a reported command failure;
- does not claim native 3D Grid geometry.

Read `docs/GRID-WORKFLOW.md`.

Still not implemented/qualified: Grid bubbles/naming/renumbering, rectangular/radial systems, Grid constraints, Direct Draw Grid jig/repeat mode, structure-to-grid hosting.

### Runtime diagnostics truthfulness

`QS3DRUNTIMECHECK` reports V25/x64/version/package consistency. Package signing information shown there is explicitly **recorded metadata only**; cryptographic Authenticode publisher/timestamp verification remains the responsibility of the signed installer/release gate. Do not change this command back into a misleading `signature=signed` claim based only on JSON metadata.

### Privacy-safe support diagnostics

New command:

```text
QS3DSUPPORTBUNDLE
```

Default report may contain runtime/product/schema/category/count/dirty-state information, but intentionally excludes DWG names/paths, CAD handles, semantic IDs, Family identity, project metadata, user name, machine name, private geometry and secrets. Read `docs/SUPPORT-DIAGNOSTICS.md` and preserve `preflight-support-bundle.py`.

## 3. Local exact-SHA V25 qualification is now a first-class handoff

Canonical local runner:

```text
scripts/run-local-v25-qualification.ps1
```

Canonical runtime matrix:

```text
docs/LOCAL-V25-QUALIFICATION.md
```

The runner requires a clean exact SHA and coordinates source preflights, Core build/smoke, V25 adapter build against installed `BrxMgd.dll`/`TD_Mgd.dll`, licensed runtime probing and scoped local evidence. `-SkipRuntime` is diagnostic only and cannot qualify a customer release.

Runtime evidence belongs under the gitignored `artifacts/` tree. Use only sanitized evidence handoff files for Git commits.

Recent concurrent work also added scoped signed-package qualification support and sanitized exact-SHA evidence export. Inspect the current runner/scripts before changing their schema or status semantics.

## 4. Rebar/native replacement state

Preserve recent cross-layer atomicity hardening across generated rebar/mesh families. Semantic ownership/audit state is updated inside the same pre-CAD-commit logical operation and restored from project snapshots if the native transaction fails before commit. Post-commit UI refresh failures must remain non-fatal.

Shape Rebar now records the canonical audit event:

```text
geometry.rebar.shape
```

alongside its generated handle/count/mode/stale update before the CAD transaction commits. Static audit guards cover generated rebar/mesh families.

The standards-neutral fabrication qualification gate is evidence/provenance validation only. It is not an engineering-code compliance engine. Standard-specific hooks/laps/anchorage/bend rules require an explicit approved governing standard + revision and engineering sign-off.

## 5. Curtain boundary remains intentionally truthful

Individual GlassWall host and Curtain LINE/path frame replacement families have guarded transaction/project rollback contracts. However:

```text
QS3DCURTAIN3D
```

still orchestrates multiple independent native phases. A later phase can fail after an earlier phase legitimately committed; current source reports `Curtain 3D PARTIAL COMMIT` rather than pretending whole-command rollback.

Do not remove the warning. Whole-command completion needs either a shared native transaction orchestration or a persisted ownership-safe compensation/recovery journal proven on real V25. See `docs/LOCAL-AGENT-OPEN-WORK-ADDENDUM-2026-08-10.md`.

Panel-by-panel native backing glass also remains a local/native architecture gate; do not add it as an unrelated best-effort third transaction.

## 6. Floor / Level model clarification

Do not add a duplicate `LevelDefinition` merely for visual parity with another product.

Current Core already has:

- `FloorDefinition` with stable ID/name/elevation;
- `ActiveFloorId`;
- element `FloorId`;
- dirty propagation for Relations + Quantity + Geometry when a referenced Floor elevation changes;
- deletion guards for active/referenced Floors.

Future top/bottom level-reference semantics should extend this existing model with explicit migration/dependency behavior.

## 7. UI integration still needing latest-blob/local/UI-agent work

Keep these commands distinct:

- `QS3DRUNTIMECHECK` — customer/runtime diagnostic;
- `QS3DRUNTIMEPROBE` — automation probe used by the local runtime harness.

A Full Domain Hub snapshot still used `QS3DRUNTIMEPROBE` behind a user-facing `Kiểm tra runtime V25` button. On the latest UI source, wire that user-facing action to `QS3DRUNTIMECHECK` while preserving the automation probe command. Also expose `QS3DSUPPORTBUNDLE` in the diagnostics/release section.

Do this only after fetching the latest large XAML/UI files; do not overwrite concurrent UI work with a stale whole-file blob.

## 8. Remaining local / native / policy gates

Source-only agents must not fake completion of:

- exact-current-SHA V25 build/NETLOAD/DemandLoad/runtime proof;
- Direct Draw/planar-UCS/ESC/UNDO/repeat authoring and future DrawJig behavior;
- Curtain whole-command recovery and panel-by-panel native glass;
- physical ownership-safe L/T/X/Multi wall-junction solids;
- representative private-DWG save/reopen/multi-DWG regression;
- Unicode/HiDPI and large-model performance;
- clean install/upgrade/uninstall;
- actual Authenticode certificate/timestamp/package trust evidence;
- commercial-license enforcement until owner supplies real SKU/seat/trial/binding/offline/rotation policy;
- standard-specific fabrication-grade rebar until governing standard/revision + engineering inputs exist;
- legal/public/source distribution model until owner/legal policy is chosen.

Canonical details:

- `docs/LOCAL-V25-QUALIFICATION.md`
- `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md`
- `docs/LOCAL-AGENT-OPEN-WORK-ADDENDUM-2026-08-10.md`

## 9. CI / release rule

GitHub Actions remain manual-only. `continue all`, source review, commits, merges, docs or handoff updates do **not** authorize workflow dispatch or GitHub Release publication.

A separate explicit owner instruction is required for CI/build/runtime/release execution. Never weaken BricsCAD `SECURELOAD`, Windows trust or signature validation to force a package/runtime test to pass.
