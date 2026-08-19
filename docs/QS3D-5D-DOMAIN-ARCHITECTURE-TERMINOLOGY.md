# QS3D 4D/5D terminology alignment for the domain architecture

**Issue:** #3104  
**Companion to:** `QS3D-5D-DOMAIN-ARCHITECTURE.md`  
**Current-main terminology source:** `BIM5D-QUANTITY-SCHEDULE-COST-MODEL.md` from issue #3101  
**Status:** normative terminology clarification; design-only, no implementation code

## 1. Why this clarification exists

`QS3D-5D-DOMAIN-ARCHITECTURE.md` describes a dependency topology in which quantity facts can feed both commercial estimating and schedule/activity mapping as independently versioned downstream contexts. That topology is intentional: QS3D can support useful model-based estimating even when no schedule has been configured.

Current `main`, however, now also defines the canonical dimensional progression used when QS3D labels an integrated BIM workflow:

```text
3D model / stable element identity
        -> quantity facts + measurement provenance
        -> 4D activity / schedule / sequence linkage
        -> 5D cost / rate / budget linkage
        -> progress / change / variance / forecast / reporting
```

These statements are compatible. The first is a **domain dependency graph**; the second is the **canonical 3D/4D/5D product terminology**.

## 2. Normative interpretation

For all implementation and documentation derived from issue #3104:

1. **Quantity truth remains the common upstream fact layer.** Geometry and deterministic measurements are not mutated by schedule or cost data.
2. **4D means explicit schedule/time semantics.** It requires activity identity, schedule revision/provenance and explicit quantity/model-to-activity linkage; a date pasted onto an estimate row is not enough.
3. **5D means explicit cost semantics.** It requires cost codes/items, rates/resources, currency/effective-date/version semantics and traceable calculation back to quantity scope.
4. **Integrated 4D/5D wording follows the canonical progression `quantity -> 4D schedule -> 5D cost`.** Time-phased cost/progress must traverse explicit quantity/activity/cost links.
5. **Quantity -> cost without schedule remains valid domain behavior**, but it is described as `model-based estimating`, `quantity-cost linkage` or `cost planning`, not as the complete integrated 4D/5D chain.
6. Cost-code mappings and rate books may be prepared before schedule linkage as independent business data. Their preparation order does not redefine the BIM dimension terminology.
7. Schedule/activity allocation remains a sidecar/versioned association and never changes the measured quantity fact.
8. A later schedule or cost revision creates a new downstream projection/snapshot and staleness/variance metadata; it does not rewrite a frozen measurement, estimate, progress snapshot or certified claim.

## 3. Architectural consequence

The canonical integrated traversal is therefore:

```text
SourceRevision
  -> MeasurementSnapshot / QuantityFact
  -> ActivityAllocationSet + ScheduleVersion          # 4D
  -> CostMapping + Rate/ResourceVersion               # 5D
  -> Estimate / time-phased commercial projection
  -> ProgressSnapshot
  -> ClaimRevision
  -> Variance / forecast / reporting
```

The architecture may still expose a schedule-independent estimating projection:

```text
MeasurementSnapshot / QuantityFact
  -> CostMapping + Rate/ResourceVersion
  -> EstimateSnapshot
```

That second path is deliberately narrower and must not be marketed or reported as proof of integrated 4D/5D capability.

## 4. Invariants added by this alignment

1. A feature cannot claim `4D-enabled` without a first-class schedule/activity mapping and schedule revision provenance.
2. A feature cannot claim `integrated 4D/5D` when cost and schedule exist only as unrelated records or UI panels.
3. A schedule-independent estimate remains reproducible from its frozen quantity/mapping/rate manifest.
4. A time-phased 5D projection must identify the exact schedule version, activity allocation version, quantity snapshot and commercial rate/cost basis it consumed.
5. Changes to the canonical 3D/4D/5D terminology must be made consistently across the BIM5D model, this companion clarification and any future implementation claims.

## 5. Relationship to the main architecture note

This file does not replace the bounded contexts, identifiers, provenance, unit/currency/time semantics, versioning, interfaces or claim invariants in `QS3D-5D-DOMAIN-ARCHITECTURE.md`. It only removes ambiguity about **sequence labels** after issue #3101 landed on `main` while #3104 was still in protected review.

If a diagram in the main architecture note appears to show schedule and cost as parallel downstream branches, read that as **independent domain preparation/dependency**, not as a different definition of 4D/5D. For integrated product terminology, this clarification is authoritative: **quantity -> 4D schedule -> 5D cost -> progress/change/claim/reporting**.
