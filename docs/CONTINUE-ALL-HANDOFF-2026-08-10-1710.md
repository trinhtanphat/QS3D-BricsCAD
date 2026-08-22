# QS3D continue-all handoff — 2026-08-10 17:10 UTC+7

This is a delta handoff for the current continue-all review. Always fetch current `main` before editing because concurrent agents are active.

## Source work completed in this continuation

### Semantic untrack safety

PR #71 was reviewed and squash-merged to `main`.

`QS3DUNTRACK` / `QS3DUNTRACKFINISH` now:

- resolve selected source or generated CAD handles through the canonical semantic/generated ownership resolver;
- reject ambiguous ownership;
- block untracking when transitive semantic dependents remain outside the selected batch;
- allow a complete dependent batch to be untracked together;
- preserve CAD geometry by design;
- have Core smoke/static regression coverage.

### Exact-SHA local V25 qualification

New canonical local runner:

```text
scripts/run-local-v25-qualification.ps1
```

Canonical local runbook:

```text
docs/LOCAL-V25-QUALIFICATION.md
```

The runner requires a clean exact Git SHA and coordinates source preflights, Core build/smoke, the V25 adapter build against installed BricsCAD assemblies, the licensed `NETLOAD` runtime probe and local evidence output. `-SkipRuntime` is diagnostic only and cannot qualify a customer release.

`artifacts/` is intentionally ignored so runtime screenshots/markers/reports/private evidence are not accidentally committed.

`AGENTS.md` now directs local-capable agents to this runner/runbook. `preflight-local-v25-qualification.py` protects the handoff contract.

### Privacy-safe support diagnostics

New customer/support command:

```text
QS3DSUPPORTBUNDLE
```

It exports only support-relevant version/runtime/schema/count information. It intentionally does not export DWG paths, CAD handles, semantic IDs, Family identity, project metadata, user name or machine name. `preflight-support-bundle.py` fail-closes if those sensitive identity surfaces are reintroduced into the default bundle.

Detailed contract: `docs/SUPPORT-DIAGNOSTICS.md`.

## Concurrent source hardening observed and preserved

Do not revert newer concurrent `main` work. Recent batches include:

- straight/curved physical opening boolean cross-layer atomicity;
- all generated rebar family replacement atomicity and post-commit UI isolation;
- DCS-correct Zoom Selected;
- duplicate semantic owner fail-closed handling;
- fabrication qualification health gating;
- premium/modeless UI hardening;
- Curtain per-host/per-frame transaction hardening and explicit partial multi-phase reporting.

Current source wins if a commit after this handoff changes any of those areas.

## Superseding boundary: aggregate QS3DCURTAIN3D

Current source now places the guarded nested host/frame builder transactions inside one command-level outer native transaction and restores a semantic snapshot when it aborts. The historical `Curtain 3D PARTIAL COMMIT` source behavior described by this older handoff no longer applies.

Do not advertise runtime qualification until the exact-SHA V25 phase-failure matrix below passes.

A local V25 agent must explicitly test:

1. LINE host succeeds, later path-host/frame phase is forced to fail;
2. frame LINE succeeds, path frame is forced to fail;
3. Health All / Curtain Frame Health clearly identifies the resulting state;
4. recovery by targeted host/frame rebuild is deterministic;
5. no foreign/ambiguous output is erased during recovery.

Only after the real BricsCAD transaction behavior is understood should an agent consider a single aggregate transaction/orchestrator. Do not fake aggregate rollback by deleting already-committed native objects after the fact without an ownership-safe reversible design.

## Important remaining boundary: diagnostics Hub wiring

`QS3DRUNTIMECHECK` is the customer/runtime diagnostic command. `QS3DRUNTIMEPROBE` is the automation probe used by the local runtime script.

At this snapshot, the Full Domain Hub still labels a button `Kiểm tra runtime V25` while its Tag points to `QS3DRUNTIMEPROBE`. This should be corrected on the latest UI source to:

```text
Kiểm tra runtime V25 -> QS3DRUNTIMECHECK
```

and the same `KIỂM TRA / RELEASE` section should expose:

```text
Xuất Support Bundle -> QS3DSUPPORTBUNDLE
```

This turn did not replace the large `DomainHubWindow.xaml` through a stale whole-file GitHub contents write because that file is actively modified by concurrent UI agents and the available remote write surface has no safe small-hunk patch operation. A working-copy/local or UI agent should make the two-line UI integration after fetching latest `main`, then add/update a static UI wiring preflight.

Do not remove `QS3DRUNTIMEPROBE`; the local automated runtime gate depends on it.

## Level / Grid clarification

Do not create a second level system.

Current Core already has:

- `FloorDefinition` with stable ID/name/elevation;
- `ActiveFloorId` and element `FloorId`;
- `ElementCategory.Grid`.

The remaining BLT-style work is to evolve the existing model safely:

- first-class Grid authoring/management only after its semantic/quantity behavior is defined;
- explicit bottom/top Floor/Level references plus offsets for categories that need them;
- deterministic dependency invalidation when a referenced floor elevation changes;
- persistence migration without guessing absolute elevations;
- UI filtering/selection by the existing Floor IDs.

Do not add `LevelDefinition` merely to duplicate `FloorDefinition` unless an explicit migration/semantic distinction is designed first.

## Product work that remains source/runtime scoped

### Must be performed by an agent with interactive licensed BricsCAD V25

Use `docs/LOCAL-V25-QUALIFICATION.md` for the complete matrix. Especially:

- exact current SHA adapter compile;
- `NETLOAD` and DemandLoad;
- Direct Draw P0/P1 and planar-UCS matrix;
- Door/Opening unique/no/ambiguous host and physical cuts;
- Room Auto / HT_PHÒNG;
- Curtain aggregate partial-phase recovery;
- all generated rebar families;
- save/reopen/Save As/multi-DWG;
- BQ/BBS/Excel Locate;
- `QS3DRUNTIMECHECK` and `QS3DSUPPORTBUNDLE` runtime behavior;
- Unicode/HiDPI/large-DWG performance;
- clean install/upgrade/uninstall;
- signed package flow when approved signing credentials exist;
- representative private DWGs kept outside Git.

### Product/architecture work not safe to invent from source screenshots alone

- physical multi-owner wall-solid L/T/X/Multi union/unmerge/rebuild ownership model;
- transient thickness/profile `DrawJig` and repeated authoring behavior before exact V25 managed-API proof;
- panel-by-panel Curtain backing glass and arbitrary/freeform clipping beyond current guarded planners;
- fabrication hooks/laps/anchorage/code-specific reinforcement when explicit engineering input is absent;
- commercial licensing/backend until product requirements and real credentials exist.

These are deliberately fail-closed gaps, not permission to add decorative/mock behavior.

## CI / release rule

No `continue all`, commit, merge, source review or local handoff authorizes GitHub Actions.

Workflows remain manual-only. Do not dispatch build/runtime/release or publish a GitHub Release without a separate explicit owner instruction and the existing release confirmation gates.
