# Session handoff — Cubicost parity / QS3D-BricsCAD — 2026-08-15

Status at handoff creation: ACTIVE / CONTINUE ALL
Canonical handoff baseline: `main@52e672e8931961460e65457a11bb13f28db8e77c`
Owner intent: inventory Cubicost deeply, implement useful parity into QS3D, keep looping through implementation/review/CI without treating `continue all` as permission to write directly to `main`.

## 1. Session objective

This session started from the question “Cubicost là gì?” and expanded into a full clean-room parity program:

1. inventory the current public Cubicost feature surface;
2. go beyond marketing pages into official help/user-guide/training/release material to find operational, legacy and edition-specific workflows that are publicly documented;
3. map each capability to QS3D as `EXISTING`, `PARTIAL`, `NEW_CORE`, `ADAPTER_NEXT`, `FORMAT_SCOPE` or `SEPARATE_SERVICE`;
4. implement the missing source-safe Core features;
5. wire BricsCAD-native MEP/takeoff/clash/review behavior;
6. continue with TBQ/cost workspace persistence and user-facing workflows;
7. keep runtime-only BricsCAD claims `PENDING_LOCAL` until exercised in a licensed runtime;
8. never claim access to undocumented Glodon trade secrets or private internal functions. Only lawful/public evidence may be used for clean-room parity.

## 2. Repository coordination rules used in this session

- Refresh exact `main` before every new lane.
- Check ACTIVE issues/PRs/reserved surfaces before writing.
- Claim/register work first.
- Implement on isolated `agent/...` branches.
- Do not infer permission to push or merge `main` from `fix bug`, `update code`, `continue all`, `implement all`, or `commit push git`.
- Do not force-push `main`.
- Reconcile concurrent writes semantically; never overwrite with blind `ours/theirs` selection.
- Re-run/refresh preflight evidence after rebasing or rebuilding a branch.
- Exact-SHA CI is required before calling a remote-capable lane complete.
- Licensed BricsCAD runtime behavior remains `PENDING_LOCAL` unless actually executed.
- Keep Platform/cloud, AutoCAD, CAD standalone and BricsCAD host-specific responsibilities separated.

## 3. Cubicost feature inventory completed

The session produced a broad public feature inventory organized by product/workflow family:

### TAS — Architecture & Structure

Covered: BIM quantity takeoff, DWG/PDF recognition, IFC/RVT import boundary, localized measurement rules, one-click calculation, automatic deductions, real-time recalculation, 3D quantity trace-back, deduction/expression inspection, report templates, classification, project segmentation/zones, revision/change management, structural/architectural/steel/earthwork/finishes/precast workflows and recognition-oriented operating options.

### TRB — Rebar

Covered: 3D rebar modeling/takeoff, DWG/PDF/JPG/tracing workflows, BS/ACI/Eurocode/country-rule concepts, reinforcement relationships, automatic quantity, graphical settings, classification, rebar schedules, reinforcement checking, missing-rebar identification/navigation, beam reinforcement workflows, BQ/cost linkage, zone quantities and 3D traceability.

### TME/TMEC — MEP

Covered: MEP takeoff, equipment/device identification, duct recognition, system/specification/region classification, 3D MEP visualization, clash detection, cross-discipline coordination and cost/report linkage.

### TBQ — BOQ / estimate / cost

Covered: BQ and resource libraries, rate build-up, historical unit rates, smart rate application, 360-degree price/reference checking, reverse lookup, cost analysis, historical benchmark, Adjust Cost, Analysis by Trade, CFA/cost-per-area, BQ Library reuse, tender BOQ, PDF/OCR boundary, addenda/change comparison, tender completeness/reasonableness, contractor bid evaluation/ranking, progress claims and quantity/cost synchronization.

### Cloud / CDE / e-tender / Manager

Covered as separate product boundaries: project common-data environment, multi-user collaboration/comments, shared project/model data, organization RBAC, online supplier/tender portal, cloud viewing/sync, launcher/updater/license/learning workspace.

### Deep official user-guide behaviors discovered in this session

Examples discovered beyond marketing summaries include:

- 3D Measure;
- 3-Point Arc;
- Add Drawing;
- Identification Options;
- Identify PDF Text;
- Restore CAD Entity;
- Select by Color;
- import-hatch recognition controls;
- beam Width×Height / Height×Width reading;
- beam host-extension and auto-extension tolerance;
- variable raft and selected reinforcement-detail operations;
- 360° Price Check / BQ↔rate reverse references;
- Adjust Cost;
- Analysis by Element Code / Trade;
- CFA/cost-per-area style analysis;
- Backfill Printing;
- Batch Import from RL;
- Build-up Analysis;
- BQ Library/category-path/project reuse.

The detailed parity inventory is maintained in the existing Cubicost parity documentation, including `docs/CUBICOST-PARITY-MATRIX-2026-08-15.md` and its deep-user-guide addenda.

## 4. Core parity work delivered

Issue #1611 / PR #1615 established the clean-room Core foundation. The work was later integrated by the repository integration flow.

### MEP Core

Added host-neutral MEP concepts:

- semantic kinds such as equipment, fixture, duct, pipe, cable tray, conduit, cable, fitting and accessory;
- system/specification/region/kind classification;
- deterministic count/length/area/volume aggregation;
- duplicate identity guards;
- finite-number/overflow validation.

### Coordination Core

Added:

- host-neutral geometry envelopes;
- hard clash detection;
- configurable clearance clash detection;
- discipline filtering;
- deterministic ordering.

### Advanced cost/TBQ Core

Added:

- resource-based cost rate build-up;
- direct cost, overhead, profit and computed unit rate;
- historical cost catalog;
- benchmark min/max/average/median/deviation statistics;
- tender requirement/bid comparison;
- incomplete-bid detection;
- deterministic complete-bid ranking;
- progress-claim certification, overclaim rejection, retention and net-certified value.

### Deep cost and identification Core

Added clean-room foundations for:

- CAD identification options;
- Select-by-Color/recognition settings;
- beam-size interpretation and extension policy;
- BQ/rate reference graph and reverse lookup;
- Build-up Analysis;
- Adjust Cost arithmetic;
- Trade/CFA analysis;
- BQ Library reuse/catalog behavior.

### Regression coverage

Core smoke coverage was added and registered for first-wave and deep-parity contracts.

## 5. BricsCAD-native MEP adapter/review sequence delivered

The session intentionally moved from low-risk read-only functionality toward richer native review.

### Wave 1 — native takeoff and broad clash

Implemented V25 read-only commands using existing repo primitives rather than duplicating selection/unit logic:

- selection through the existing entity snapshot reader;
- drawing-unit conversion through the existing CAD unit policy;
- true curve length from native curve metrics, not bounding-box diagonal approximation;
- `GeometricExtents` only for clash envelopes;
- no project auto-create and no DWG mutation.

Commands include Cubicost-style MEP takeoff and broad clash entry points.

### Recognition profiles + Locate

Implemented configurable MEP recognition rules with priority and ambiguity fail-closed behavior. Specific rules such as `CABLETRAY` may outrank broad `CABLE` rules; same-priority conflicting outcomes are rejected as ambiguous rather than guessed.

`QS3DMEPCLASHLOCATE` was added for native clash-pair navigation.

A correctness bug was found and fixed during review: the original Locate flow could set PICKFIRST to one live entity and only afterwards discover that the second Handle was stale. The corrected contract resolves both live ObjectIds first and changes implied selection only when the full pair is valid.

### Exact Solid3d narrow-phase

An overlapping agent lane already implemented exact native `Solid3d.CheckInterference`; this session reconciled rather than duplicating it. Blob-level review showed the recognition/adapter contents were compatible even though histories diverged.

### Exact transient highlight review

Added a dedicated exact-clash review/highlight command using native `Entity.Highlight()` / `Unhighlight()` with ownership-aware cleanup and `finally`-style safety. The implementation avoids holding DBObjects/transactions open across user interaction and avoids unhighlighting graphics the command did not successfully own.

### Modeless review workspace / zoom / profile persistence

Issue #1666 / PR #1668 landed source support for:

- `QS3DMEPZOOMSELECTION` using native extents and current-view fit;
- modeless MEP review launcher/workspace;
- centralized MEP recognition profile provider;
- per-host roaming recognition profile persistence;
- bounded/hardened XML parsing with DTD/external resolution disabled;
- atomic temp/replace save semantics;
- no retained `Document`, `DBObject`, `ObjectId` or `Solid3d` in modeless UI fields.

Issue #1666 remains explicit that licensed V25/V26 graphics/modeless behavior is `PENDING_LOCAL` until run in a licensed host.

## 6. Quantity Insight / exact geometry work discovered while continuing

Issue #1669 / PR #1678 landed source support for exact BREP-backed Quantity Insight behavior:

- live ownership revalidation;
- exact geometry explanation rather than treating bounding boxes as exact;
- quantity/detail row locate/select/highlight/zoom;
- deduction/intersection region transient preview;
- stale/deleted/foreign/multi-DWG fail-closed behavior;
- read-only/transient graphics lifecycle with no persistent DWG mutation.

Issue #1669 remains open because exact-main cloud evidence and licensed BricsCAD local acceptance are still required.

## 7. Recognition batch atomicity lane currently active

Issue #1679 / PR #1681 reserves TAS recognition atomicity.

The observed defect: multi-row review/auto/B4D could capture rows one-by-one; a late failure could leave earlier semantic mutations committed.

The PR changes the boundary to:

- preflight/revalidate accepted rows first;
- one outer ProjectState snapshot/transaction boundary for semantic/audit mutation;
- rollback the complete batch if mutation fails;
- refresh live handles/candidates/capture readiness/source ownership before mutation;
- keep AUTO/B4D confidence/margin gates deterministic;
- update UI only after successful semantic commit.

This lane explicitly excludes TBQ #1674, Quantity Insight #1669 and MEP review #1666.

## 8. TBQ project-bound workspace — current implementation lane

Issue #1674 is the active TBQ continuation claim.

Branch discovered for the lane:

`agent/chatgpt-gpt56sol/tbq-project-workspace`

Original claim baseline was `main@6d0bde12266f3839752818ffeeb261852b73ae4e`; the repository has advanced since then and must be refreshed/reconciled before implementation continues.

### Proven architecture facts

- QS3D already has real cost primitives such as `EstimateLine` and `RateBook`; do not create a second cost engine.
- Deep TBQ Core services already exist for rate references, build-up analysis, adjustment, trade/CFA analysis and BQ library behavior.
- `ProjectStateSnapshot` persists project domain state including metadata but does not expose a typed estimate/rate workspace as a first-class snapshot member.
- `ProjectState` already owns metadata-backed project collections and mutation/version semantics.
- the BricsCAD project mutation path is existing-project bind → canonical project-id check → backing-store freshness check → project mutation → coordinator save under the repository file-lock/atomic-save path.
- TBQ must not auto-create a project for mutation and must not write a parallel ad-hoc sidecar.

### Reserved acceptance for #1674

- metadata-backed project TBQ workspace;
- deterministic schema/version serialization;
- malformed/unsupported workspace data fails closed;
- standard metadata mutation increments `ChangeVersion`;
- QSDB snapshot/store roundtrip coverage;
- V25 read-only commands for rate/reference/build-up/trade/BQ workflows;
- Adjust Cost preview;
- Adjust Cost apply only after canonical existing-project bind + freshness + coordinator save under lock;
- no stale/cold-cache mutation;
- focused source guards and smoke tests;
- exact-SHA CI evidence before remote-qualified completion;
- native licensed behavior remains `PENDING_LOCAL` when not runnable remotely.

## 9. Current Git state at creation of this handoff

Exact `main` at handoff creation:

`52e672e8931961460e65457a11bb13f28db8e77c`

The repository is actively changing because multiple agent/integration/release lanes are landing concurrently. Every future implementation step must re-read `main`; no SHA in this document should be blindly reused as a future base.

Known relevant live items at this checkpoint:

- #1666 — MEP review workspace/profile source integrated; local runtime qualification pending.
- #1669 — Quantity Insight exact BREP/highlight/locate source integrated; cloud/local evidence pending.
- #1674 — TBQ project-bound workspace + V25 cost workflows ACTIVE.
- #1679 / PR #1681 — TAS recognition atomic multi-row apply ACTIVE/open.

## 10. Session decisions and rejected designs

- Do not build fake calculator-only TBQ commands disconnected from project state.
- Do not persist TBQ through a random new file/sidecar while project metadata and atomic QSDB persistence already exist.
- Do not treat measurement work-item mapping as a generic metadata store; it is a domain catalog/resolver.
- Do not use bounding-box diagonal as MEP quantity length.
- Do not use broad-phase bounding boxes as proof of exact Solid3d interference.
- Do not change PICKFIRST partially when a clash pair cannot be fully resolved.
- Do not retain native CAD database objects in long-lived modeless WPF state.
- Do not call `Editor.Command("ZOOM", ...)` merely to fake navigation when current-view APIs can provide deterministic behavior.
- Do not duplicate overlapping agent lanes; review/reconcile and reuse canonical work.
- Do not call source-only native graphics behavior PASS.
- Do not claim undisclosed/private Glodon functionality. Public/help/user-guide evidence is acceptable clean-room input; non-public trade secrets are not.

## 11. Product-boundary backlog after TBQ

After #1674 is complete, remaining Cubicost-inspired parity should continue without collapsing product boundaries:

### BricsCAD host

- finish native TBQ workspace UI/palette and persisted project binding;
- richer cost/BQ report integration with existing report/XLSX paths;
- coordination issue persistence after the shared Platform coordination contract is canonical;
- remaining native review polish and licensed V25/V26 acceptance;
- V26 parity for newly-added V25 commands/UI where missing.

### Format/AI lanes

- native RVT/Revit ingestion only through a separately approved interoperability lane;
- PDF/tender-table OCR, Identify PDF Text and auto-inking through a dedicated format/recognition lane;
- expanded DWG/PDF/JPG intelligent recognition where public behavior is reproducible without proprietary implementation details.

### QS3D Platform/cloud

- common data environment;
- organization/project RBAC;
- enterprise shared cost/rate/BQ libraries;
- comments/collaboration;
- E-tender supplier portal and multi-round online bid workflow;
- cloud viewer/synchronization;
- Manager-like launcher/updater/license/learning workspace.

### AutoCAD / standalone

- adopt shared vendor-neutral contracts from Platform/Core;
- implement host-native equivalents independently rather than copying BricsCAD-specific transaction/graphics code.

## 12. Completion truth

This session is not yet eligible for “everything 100% complete”. Large Core and native MEP/Quantity Insight pieces have landed, but the following gates remain:

1. finish #1674 TBQ workspace persistence + V25 user-facing workflows;
2. finish/reconcile #1679 recognition atomicity;
3. obtain exact-SHA cloud/CI evidence for integrated source where required;
4. obtain licensed local V25/V26 runtime evidence for native modeless/graphics/selection/highlight paths;
5. continue remaining format/cloud/V26 lanes under separate claims;
6. only an explicitly-authorized integration coordinator may merge remaining PRs to `main`.

This document is the canonical handoff for the Cubicost-parity conversation as of 2026-08-15 15:02 Asia/Ho_Chi_Minh. Future agents should update this document or create a clearly linked successor when material state changes.