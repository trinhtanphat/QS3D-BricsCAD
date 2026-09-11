# Estimating rate build-up workspace

Issue: #6461  
Parent program: #6127  
Commands: `QS3DESTIMATE`, `QS3DRATEBUILDUP`

## Purpose

This workspace adds the professional estimating workflow around the existing QS3D cost arithmetic authority. It does **not** introduce a second pricing engine.

`CostResourceComponent`, `CostRateBuildUp`, `CostDecimalMath` and `RateBook` remain authoritative for decimal-preserving resource extension, direct cost, overhead, profit and final unit rate.

## Supported estimating model

A build-up revision contains:

- build-up ID, revision ID and previous-revision linkage;
- CostCode, bill unit and ISO-style three-letter currency token;
- UTC effective date, author and revision reason;
- Material, Labour, Plant and Subcontract resource lines;
- per-line quantity per bill unit, unit rate and wastage percentage;
- quotation/source ID, supplier, source reference and source effective UTC date;
- overhead and profit percentages;
- immutable Draft → Reviewed → Approved lifecycle metadata.

Positive wastage is converted to an effective resource quantity using the existing Core decimal-preserving percentage/add helpers. The resulting resource lines are then converted to canonical `CostResourceComponent` inputs. `CostRateBuildUp` alone computes direct cost, OH, profit and final unit rate.

## Operator flow

1. Open a drawing in BricsCAD V25.
2. Run `QS3DESTIMATE` (or `QS3DRATEBUILDUP`).
3. Enter build-up identity, revision, CostCode, bill unit, currency, effective date, author and reason.
4. Enter one or more resource rows. Category must be `Material`, `Labour`, `Plant` or `Subcontract`.
5. Record source provenance for every row. Source currency is bound to the build-up currency and source date cannot be later than the revision effective date.
6. Enter OH and profit percentages.
7. Click **Evaluate**. The displayed Direct / Overhead / Profit / Unit rate values are projections of the Core `CostRateBuildUp` result.
8. Enter reviewer identity/note and click **Mark reviewed**.
9. Enter approver identity/note and click **Approve**.
10. To change approved/reviewed content, click **New revision**, update resource/provenance inputs and Evaluate. The successor revision links to the prior revision and starts again as Draft.

## Safety / lifecycle rules

- Reviewed or Approved content cannot be recalculated under the same revision identity in the UI.
- Approval requires a Reviewed revision; review requires a Draft revision.
- Approval time cannot precede review time; review time cannot precede revision effective time.
- Duplicate resource codes fail closed.
- Currency mismatch between source and build-up fails closed.
- Future-dated source provenance relative to the revision fails closed.
- Invalid/oversize/control-character text and non-canonical identifiers fail closed through Core contracts.
- The modeless window is bound to the admitted managed document and native database generation. Actions refuse a stale/background drawing generation.
- UI/adapters do not calculate direct cost, OH, profit or final unit rate.

## Current persistence boundary

This carrier deliberately does not modify `ProjectMetadataDictionary`, `QuantitySummaryWindow` or `CommercialQsWindow` because those paths have active non-overlapping QS reservations. The revision model is persistence-ready and immutable, but durable project binding is a follow-up once the current metadata reservations are reconciled/released. No alternate project store is introduced here.

## Deterministic validation

`EstimatingRateBuildUpSmoke` verifies:

- all four resource categories and quotation provenance;
- 10% material wastage routed through Core helpers;
- deterministic arithmetic oracle: Direct 500, OH 50, Profit 55, Unit rate 605;
- Draft → Reviewed → Approved transitions;
- new-revision previous-ID linkage and approval reset;
- future source date, source/build-up currency mismatch, excessive wastage and duplicate resources fail closed.

`scripts/preflight-estimating-rate-build-up-workspace.py` additionally pins the authority boundary and rejects UI-side monetary arithmetic.

## Qualification boundary

Protected source/Core smoke/V25 compile evidence proves source integration only. It is **not** a licensed BricsCAD runtime PASS. Real modeless interaction, MDI switching, DPI and native host behavior remain subject to the repository's licensed-local qualification policy.
