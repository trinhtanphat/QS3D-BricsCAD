# QS3D Semantic Change Review — source contract

Updated: 2026-08-11

Status: `LANDED_SOURCE` only after integration to `main`. BricsCAD V25 runtime qualification remains `LOCAL_ONLY`.

## Purpose

WS-27 Revision / Audit / Change Review needs a presentation model that can explain semantic revision changes without creating a second diff engine and without treating native CAD handles as portable authority.

`QS3D.Core.Revisions.SemanticChangeReviewBuilder` is that presentation layer. It consumes the authoritative `RevisionService.Compare(before, after)` result and groups the deltas by stable semantic element ID.

## Review model

Each grouped semantic element records:

- stable `ElementId`;
- semantic category resolved from the after snapshot when present, otherwise the before snapshot;
- element change state (`Added`, `Removed`, `Changed`);
- deterministic visible field rows;
- count of source-reference/native-property changes deliberately omitted from portable review content.

Visible field rows are classified as:

- `Identity`: `Category`, `FamilyId`, `FloorId`, `ZoneId`;
- `Property`: portable fields with the `Property:` prefix;
- `Quantity`: fields with the `Quantity:` prefix;
- `Other`: any future `RevisionService` field not yet given a dedicated presentation category.

The review summary exposes added/removed/changed element counts plus identity/property/quantity/other field counts.

## Authority and portability boundaries

This class does **not** duplicate `RevisionService.Compare` and does not reimplement quantity arithmetic. `QuantityRevisionReport` remains the dedicated quantity revision report.

`RevisionService` intentionally retains local revision authority, including `SourceHandles` and all element properties, because drawing-local provenance can matter to lifecycle diagnostics. Portable review presentation has a narrower contract: it must not surface raw native references as if they were stable cross-machine/project identity.

`SemanticChangeReviewBuilder` therefore:

- omits the explicit `SourceHandles` delta;
- for `Property:*` deltas, reuses `ProjectInterchangeElementPropertyPolicy.IsPortable(...)` rather than maintaining a second native-property heuristic;
- consequently omits generated owner-slot/handle properties, `Generated*` and `QS3D.Generated*` metadata, `PhysicalOpeningCut*` drawing-local state, Room `BoundarySourceHandles`, and other handle-bearing property keys rejected by the shared portability policy;
- increments `OmittedSourceReferenceChangeCount` for every omitted delta so a reviewer still sees that drawing-local provenance changed without receiving the raw value.

This reuse is intentional: Interchange export/import and semantic revision review should agree on which `ProjectElement.Properties` are portable semantic data. The review layer does not alter or redact the underlying `RevisionSnapshot`; only its presentation rows are filtered.

Added or removed elements remain visible even when they have no field rows. A changed element whose only revision deltas are drawing-local references also remains visible with an omitted-reference count.

## Determinism and validation

The builder:

- validates semantic element IDs as nonblank, unpadded and unique case-insensitively;
- delegates actual diff calculation to `RevisionService.Compare`;
- orders visible fields by field kind and name;
- orders grouped elements by change state, category and stable semantic ID;
- performs no mutation of either revision snapshot or live project state;
- has no BricsCAD/Teigha/Autodesk dependency.

## Regression coverage

`SemanticChangeReviewSmoke` covers:

- added/removed/changed grouping;
- identity/property/quantity classification;
- explicit `SourceHandles` omission;
- generated handle, Room boundary-source handle, generated stale-snapshot and physical-opening native-reference property omission through the shared Interchange portability policy;
- preservation of the changed semantic element and omitted-reference count when native references change;
- deterministic ordering under different input ordering;
- padded/duplicate semantic ID fail-closed behavior.

`scripts/preflight-semantic-change-review.py` protects the presentation/authority boundary, verifies reuse of `ProjectInterchangeElementPropertyPolicy`, and is automatically discovered by `scripts/preflight-all.py`.

## Not claimed by this source batch

This does not qualify:

- modeless Revision UI freshness in a licensed BricsCAD V25 session;
- native entity/handle persistence or reconcile behavior;
- private customer DWG comparison correctness;
- exact-SHA NETLOAD, multi-document, Save/SaveAs/reopen behavior;
- signed installer/release readiness.

Those remain under the repository's existing local exact-SHA qualification gates. The source privacy fix itself is fully exercised at Core/presentation level and does not create a new native mutation scenario.
