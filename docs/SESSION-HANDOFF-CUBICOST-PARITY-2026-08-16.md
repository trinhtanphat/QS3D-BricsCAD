# Session Handoff — Cubicost / QS3D parity continuation — 2026-08-16

Updated: 2026-08-16 (UTC+7)
Coordination issue: #1821
Previous historical handoff: `docs/SESSION-HANDOFF-CUBICOST-PARITY-2026-08-15.md`

## 1. Purpose and evidence rule

This file is the continuation checkpoint for the long Cubicost/QS3D parity session. The 2026-08-15 file remains historical evidence and is not rewritten because issue/PR numbers and repository heads move quickly under concurrent agents.

The parity effort is clean-room and public-source based. “Deep/internal” Cubicost behavior means publicly verifiable help-center, user-guide, release/training or other lawfully public behavior. It never means private Glodon source, employee-only documentation, private builds or guessed secret functionality.

Live GitHub state always wins over stale issue mappings in an older handoff.

## 2. Repository ownership boundary

### QS3D-Platform

Canonical home for vendor-neutral shared contracts and services:

- BIM/QS semantic contracts;
- MEP recognition and quantity aggregation;
- host-neutral coordination/clash contracts;
- BQ/rate/cost/tender/progress/4D-5D logic;
- TBQ reference analysis and reusable cost-analysis services;
- shared issue/workflow DTOs that do not depend on a CAD SDK.

### QS3D-BricsCAD

BricsCAD-native adapter/product only:

- DWG/document/transaction access;
- `Solid3d` / native exact interference;
- selection, implied selection, highlight and zoom;
- BricsCAD WPF/modeless host integration;
- V25/V26 packaging and BricsCAD-native qualification.

### QS3D-AutoCAD

Autodesk-native adapter/UI consuming shared Platform semantics. Autodesk SDK types must not leak into shared Platform.

### QS3D-CAD

Standalone desktop host consuming shared Platform semantics without pretending to provide native DWG behavior it does not execute.

### FORMAT_SCOPE / SERVICE_SCOPE

OCR/PDF/RVT-heavy import is format-adapter scope. CDE/cloud/RBAC/supplier portal/multi-user organization services are service scope. They should not be implemented as BricsCAD-only product logic.

## 3. BricsCAD live checkpoint

At this continuation checkpoint, `QS3D-BricsCAD/main` is:

`8ddea334f0af1136e97c881d32907e0a0f20249f`

Commit: `fix(repo): restore executable governance preflights (#1815)`.

`main` is reported protected in the live branch response. Repository policy remains explicit: prompts such as `continue all` / `implement all` authorize implementation work but do **not** authorize a merge to `main`.

The prior Cubicost MEP/TBQ native work already established the BricsCAD-side adapter boundary, including MEP takeoff, broad/exact clash, Locate/highlight/zoom/review/profile surfaces and project-bound TBQ workflow. Licensed interactive V25/V26 truth remains LOCAL_ONLY unless a licensed host actually executes the exact SHA.

V26 is not a second duplicated product implementation: its project links the shared V25 adapter source with the V26 target/runtime differences. New shared command source must therefore be reviewed for both targets rather than blindly copied into a second V26 file tree.

## 4. Platform live checkpoint

`QS3D-Platform/main` remains:

`7d20229ac12b6d41c90f75f08cc361ee76372635`

The shared Cubicost implementation is intentionally still a dependency stack, not an inferred main landing.

### PR #15 — shared Cubicost parity baseline

Branch: `agent/chatgpt-gpt56sol/cubicost-shared-parity-20260815`
Head: `a5778f4abcf3b5c308c5d6854040dbc0c3082390`
State: OPEN / mergeable on last readback.

Shared coverage includes MEP recognition/quantities, host-neutral clash, CAD-identification config, coordination issue state, BQ library, cost build-up, historical benchmark, BQ/rate reference, Adjust Cost, Trade/CFA, smart-rate matching, tender evaluation/revisions/rounds, progress claims and 4D/5D cost projection.

Earlier exact implementation validation for this baseline is recorded in its PR/session evidence. It is host-neutral source evidence only.

### PR #17 — shared MEP -> BQ/cost projection

State: OPEN / mergeable on last readback.

Adds deterministic MEP quantity-group to BQ mapping and exact BQ/unit/currency cost projection with fail-closed ambiguity and source provenance. It remains stacked on #15.

### PR #19 — TBQ 360-degree price/reference check

Issue: QS3D-Platform #18
Branch: `agent/chatgpt-gpt56sol/cubicost-tbq-360-price-check-20260816`
Head: `8d9169b7bd4105d50011c10526e4ed539223d6eb`
Base: #15 branch
State: OPEN / mergeable.
CI: QS3D Platform CI #124 / run `31919463022` — SUCCESS.

Delivered:

- immutable rate/reference graph;
- unit/basic rate categories;
- BQ adoption and unit-rate composition references;
- deterministic `BQ`, `UR`, `BQ+UR` or empty reference state;
- Check Linking Rate reverse lookup;
- Check BQ Reversely lookup;
- rates-not-adopted-in-BQ review;
- fail-closed duplicate/dangling/invalid-role validation;
- dedicated smoke/source guard/docs.

### PR #21 — TBQ Analysis by Element Code

Issue: QS3D-Platform #20
Branch: `agent/chatgpt-gpt56sol/cubicost-tbq-element-analysis-20260816`
Head: `20ae90285ebb10b8ecb513f8ce78c94a521ada15`
Base: #19 branch
State: OPEN.
CI: QS3D Platform CI #125 / run `31919681711` — SUCCESS; validate job `95097388476` succeeded.

Delivered:

- element-code cost lines and deterministic aggregation;
- blank element codes -> `Unclassified`;
- checked decimal totals and share percentage;
- optional analysis area / cost per square metre;
- project/bill/element-style generic hierarchy scoping with sibling-boundary protection;
- deterministic source-line counts and ordering;
- fail-closed negative/duplicate/null input validation;
- dedicated smoke/source guard/docs.

### PR #23 — TBQ Build-up Analysis

Issue: QS3D-Platform #22
Branch: `agent/chatgpt-gpt56sol/cubicost-tbq-build-up-analysis-20260816`
Head: `841db29bafd7b65dc34f3c79beb342f2ad7e398f`
Base: #21 branch
State: OPEN.
CI: QS3D Platform CI #126 / run `31919881915` — SUCCESS.

Delivered:

- analysis workspace exposes only BQ-adopted build-up rates;
- deterministic Check BQ Reversely;
- existing adopted-rate replacement only;
- no add path for new/unadopted rates;
- immutable-style replacement result with previous/current rate, affected BQ items and next workspace;
- caller/host retains the real atomic persistence transaction boundary;
- duplicate/dangling reference validation plus smoke/source guard/docs.

Dependency order is therefore:

`#15 -> (#17 independent sibling where applicable) -> #19 -> #21 -> #23`

Do not flatten or merge these out of order without a deliberate integration review.

## 5. Public TBQ behavior covered / still open

Public Glodon TBQ guide material used in this session includes:

- 360-Degree Price Check in Build-up unit rate;
- Adjust Cost;
- Analysis by Element Code;
- Analysis by Trade;
- BQ Library;
- Backfill Printing;
- Batch Import from RL;
- Build-up Analysis.

Already represented substantially in shared source: Adjust Cost, Trade/CFA, BQ Library, 360-degree reference checking, Element Code analysis, Build-up Analysis, broader rate/tender/progress/cost contracts.

Still requiring evidence-first dedicated follow-up before implementation/claim:

- Backfill Printing;
- Batch Import from RL;
- deeper report/export behavior and report-template semantics;
- any edition/version-specific TBQ behavior not yet mapped.

Do not implement from a feature title alone when exact semantics matter. Retrieve public documentation first, then classify shared vs host vs format/service ownership.

## 6. TAS / TRB / TME continuation

Previously verified public TAS/TRB help surfaces include CAD/PDF drawing import, 3D measurement, arc drawing, identification options, PDF-text identification, Restore CAD Entity, variable raft, zone reassociation, wall mesh/link configuration and related edge cases.

TME remains split between shared semantics and native CAD extraction. Public product material supports standardized MEP calculation rules, MEP device identification, duct classification by system/specification, region-based quantification and clash detection. BricsCAD-native extraction/solid truth stays in the adapter; recognition/quantity/cost semantics should converge in Platform.

## 7. Validation truth

A green Platform CI means the host-neutral tree compiles and its deterministic guards/smokes pass. It does not prove BricsCAD/AutoCAD UI, licensed native selection, graphics, document affinity or exact modeler behavior.

A cloud V25 compile/package also does not substitute for licensed interactive runtime qualification where the acceptance matrix requires it.

Never report `PASS` for native/local rows that were not actually executed.

## 8. Merge / concurrency rule

This session has not been granted new integration authority by `continue all`.

Therefore:

- no direct `main` writes from these feature lanes;
- no merge of #15/#19/#21/#23 merely because source CI is green;
- refresh live heads before each new lane;
- use issue/claim -> agent branch -> plan -> implementation -> exact CI -> PR -> fix loop;
- never force-push over another agent's concurrent changes;
- integration coordinator must preserve dependency order and obtain required exact merged-tree/main evidence.

## 9. Current next safe work

1. keep #15/#17/#19/#21/#23 green and reconcile if their bases move;
2. continue public-document evidence collection for remaining TBQ features, beginning with Backfill Printing and Batch Import from RL;
3. map each new behavior to Platform, host adapter, FORMAT_SCOPE or SERVICE_SCOPE before coding;
4. continue TAS/TRB/TME parity only where a public behavior contract is known;
5. run licensed V25/V26 LOCAL_ONLY matrices for native BricsCAD behavior on exact integrated SHAs;
6. after explicit owner integration authorization, merge in dependency order and require exact-main CI before calling the product stack complete.

## 10. Session verdict at this checkpoint

`PROMPT/LANE STATUS`: remote/source-safe continuation is progressing and the newly implemented Platform TBQ lanes are green; the overall Cubicost/QS3D product-family parity program is **NOT 100% COMPLETE**.

`MERGED TO MAIN`: this continuation docs lane and Platform feature stack are **NO** unless a separate authorized integration session lands them.

`SESSION CAN BE CLOSED/DELETED`: **NO** if the goal remains full implementation + integration + native qualification of the public parity backlog.
