# Material Usage semantic generation fence

Issue: #6023  
Lane: C02 Quantity / Export  
Runtime: REMOTE_SAFE deterministic managed Core/reporting.

## Reproduced defect

On protected `main@edf4d77f17f993feb33bece819aba1a38893538e`, `MaterialUsageScheduleBuilder.Build(ProjectState)` snapshots floor/family/material lookup dictionaries and then enumerates `project.Elements` while reading each live element/family `Properties`, `Quantities`, `SourceHandles` and project identity. Nested semantic mutation during the build can therefore mix catalog generation A with reporting/provenance generation B without requiring a `ProjectState.ChangeVersion` transition.

## Required contract

- Capture one detached semantic Material Usage generation before aggregation: project identity/fingerprint, floor/family/material semantics, element reporting values and provenance.
- Aggregate only detached values; do not resume traversal of live `project.Elements` for quantities/properties/source handles after capture.
- Revalidate the source generation before publication and fail closed on in-place quantity, family semantic/name/property, source-handle, or equivalent-instance drift even when project revision alone is insufficient.
- Preserve room exclusion, family/category validation, deterministic grouping/order, compensated Length/Area/Volume/Mass aggregation, finite/non-negative validation, mass overflow/underflow protections, checked element counts and public row/provenance shape.
- Cover stable deterministic output plus hostile Count/enumerator/content drift, duplicate/case-colliding identities and semantic mutations.

## Qualification

Start RED-first with `scripts/preflight-material-usage-generation-fence.py` and executable `MaterialUsageGenerationFenceSmoke`. Production follows only after intended RED is established with Reservation-v2/generic admission green. Then run focused guard/smoke, broad deterministic Core, self-review, latest-main non-force reconcile and fresh exact-head protected `preflight + core` before expected-head merge.

No licensed BricsCAD runtime PASS is required or claimed.
