# Source Review + Dependency Impact — 2026-08-11

This source-only batch continues the product flow documented in `docs/SOURCE-PRODUCT-PLAN-2026-08-10.md` without adding a second regeneration/rule engine and without moving any BricsCAD V25/native-runtime qualification into remote scope.

## Implemented in this batch

### Dependency Impact Plan

`QS3D.Core.Services.DependencyImpactPlanner` turns the existing `DependencyGraph` into a review-oriented impact model.

- accepts one or more canonical semantic root element IDs;
- rejects blank, padded, duplicate, or missing roots;
- reuses `DependencyGraph.GetDirectDependents()` rather than maintaining a second graph;
- performs deterministic breadth-first traversal;
- records shortest-path `Depth`, immediate `CauseElementId`, and originating `RootElementId` for every impacted element;
- excludes selected roots from the impacted list even when dependency cycles lead back to them;
- binds the result to `ProjectId` + `ProjectState.ChangeVersion`;
- fails if the project changes while the plan is being computed;
- does not mutate live semantic state.

This is the Core contract intended for later dependency-impact visualization/filtering in the review UI.

### Preview Review Snapshot

`QS3D.Core.Review.PreviewReviewSnapshotService` and `PreviewReviewSnapshotStore` provide a portable review artifact for the existing Quantity Rule Preview and Regeneration Preview pipelines.

- format: `QS3D.PreviewReviewSnapshot` v1;
- named snapshot with project ID, source `ChangeVersion`, operation kind, scope, targets, review rows, and summary counts;
- supports both `QuantityRuleProjectPreview` and `RegenerationPreview`;
- subset regeneration targets remain explicit and canonical;
- snapshot content is normalized and protected with SHA-256 fingerprinting;
- load verifies fingerprint and semantic invariants and fails closed on tampering;
- XML loading prohibits DTD resolution and caps input at 16 MiB;
- save uses the existing `AtomicFileCommit` replacement path with backup;
- CAD-handle fields are filtered from regeneration review content and forbidden on load, so the team-review artifact does not become a raw CAD-handle export;
- creating/saving a review snapshot does not apply the preview or mutate the project.

## Source regression contracts

Added source smoke coverage for:

- deterministic/read-only dependency impact planning;
- multi-root shortest-cause behavior;
- malformed/missing root fail-closed behavior;
- quantity review snapshot round-trip;
- regeneration subset-scope preservation;
- fingerprint tamper rejection;
- CAD-handle field injection rejection.

Added Core-only preflight gates:

- `scripts/preflight-dependency-impact-plan.py`
- `scripts/preflight-preview-review-snapshot.py`

`preflight-all.py` already discovers `preflight-*.py` automatically, so no aggregate runner edit is required.

## Product-flow effect

The source workflow is now:

`semantic change candidate -> dependency impact plan -> rule/regen preview -> named review snapshot -> user/team review -> existing guarded Apply -> Model Health regression gate -> native ownership-safe output`

The new contracts are review/read-only infrastructure. They do not automatically apply quantity rules, regeneration, or native CAD mutations.

## Still LOCAL_ONLY / not qualified by this batch

This batch does not claim or replace local qualification for BricsCAD V25 compile/NETLOAD, private DWG behavior, native geometry/boolean ownership, multi-document runtime behavior, HiDPI/runtime performance, engineering-standard rebar approval, Authenticode signing, installer, or clean-machine release qualification.

No GitHub Actions run is required to land this source-only contract; runtime truth remains governed by the existing local qualification handoff.
