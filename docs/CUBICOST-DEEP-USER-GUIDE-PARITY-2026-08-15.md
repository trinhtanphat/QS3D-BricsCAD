# Cubicost deep user-guide parity addendum

Updated: 2026-08-15 (UTC+7)
Issue: #1611
PR: #1615

## Purpose

This addendum expands the Cubicost parity audit beyond product marketing pages into official Glodon help-center and user-guide material. It captures operational functions, settings, checks and legacy/deep workflows that are easy to miss from headline product descriptions.

This remains a clean-room parity exercise. It does not claim access to undisclosed Glodon source code, private assets, trade secrets or non-public file formats. If a behavior cannot be lawfully verified from public or owner-provided material, it is not represented as a known Cubicost feature.

## Deep TAS/TRB identification and modeling functions verified from official help

The official TAS/TRB help center exposes operational functions including:

- 3-point arc drafting.
- 3D measure.
- Add Drawing.
- slab merge troubleshooting/validation.
- typical-floor/elevation handling for finishes.
- CAD/PDF drawing identification troubleshooting.
- optional hatch exclusion during CAD/PDF import.
- Select by Color identification mode.
- per-color classification rules for separating entities such as columns and walls.
- beam auto-extension tolerance during identification.
- CAD Identification Options dialog/configuration.
- beam size reading mode: Width × Height vs Height × Width.
- beam-to-column/wall extension policy: extend into host vs nearest host face.
- PDF text identification.
- restore CAD entity.
- move entities from one zone to another.
- variable raft-foundation end handling.
- RC-wall separate end-link handling.
- wall-mesh lapping length settings.
- wall horizontal bar/link-shape settings.

### QS3D source-safe implementation in #1611

`src/QS3D.Core/Recognition/CadIdentificationOptions.cs` adds a host-neutral contract for:

- import-hatch filtering;
- Select by Color enablement;
- deterministic color-classification rules;
- beam size reading mode;
- beam host-extension policy;
- beam auto-extension tolerance;
- PDF-text-identification capability flag;
- restore-CAD-entity capability flag.

Native DWG/PDF entity reading, OCR and BricsCAD command/UI integration remain separate adapter/format work.

## Deep TBQ cost workflows verified from official help

### 360-Degree Price Check / rate-reference checking

Official TBQ help documents reference marks and reverse lookups across bill items and unit rates:

- show/hide/refresh reference marks;
- `BQ` reference mark for rates used by bill items;
- `UR` reference mark for rates used inside unit rates;
- Check Linking Rate to find unit rates using a selected rate;
- Check BQ Reversely to find bill items using a selected rate;
- detect potentially unused rates and missed adoption.

### Build-up Analysis

Official TBQ help documents a Build-up Analysis view containing adopted rates, allowing rate review/modification and reverse BQ lookup. Changes are intended to remain linked across BQ and build-up views.

### Adjust Cost

Official TBQ help documents cost adjustment at project, bill or element scope with:

- adjustment ratio;
- markup ratio;
- direct adjusted-total entry;
- automatic derivation of related ratios from a target total;
- filtering before targeted adjustment;
- collaborative checked-out-node awareness.

The checked-out multi-user behavior belongs in QS3D Platform/RBAC/collaboration. The source-safe arithmetic is implemented in Core.

### Analysis by Trade

Official TBQ help documents:

- trade codes on bill items;
- analysis grouped by trade;
- CFA input;
- automatic cost-per-m²-CFA calculation;
- Unclassified bucket for items without trade codes;
- refresh behavior;
- export to Excel;
- hide/unhide columns and expand/fold analysis presentation.

Core implements the deterministic trade/CFA analysis; UI column state and Excel presentation reuse existing QS3D reporting/export infrastructure.

### BQ Library

Official TBQ help documents:

- create a BQ library;
- categories/subcategories/headings/bills;
- import bills from previous projects;
- reuse accumulated BQ data.

Core implements deterministic BQ-library entries, category paths and guarded project import/reuse.

### Other guide-index functions discovered

The official TBQ guide index also exposes operational entries such as:

- Analysis by Element Code;
- Backfill Printing;
- Batch Import from RL;

These are recorded for follow-up. Where their complete behavior is not yet available in fetched official text, QS3D must not invent exact semantics. They remain `DEEP_GUIDE_DISCOVERED` until a verified specification is available.

## New Core surfaces

- `src/QS3D.Core/Recognition/CadIdentificationOptions.cs`
- `src/QS3D.Core/Cost/DeepCostWorkflows.cs`
- `tests/QS3D.Core.SmokeTests/CubicostDeepParitySmoke.cs`

Implemented cost functions:

- `RateReferenceGraph`
- BQ/UR reference marks
- reverse reference lookup
- `BuildUpAnalysisService`
- `CostAdjustmentService`
- `TradeCostAnalysisService`
- `BqLibraryCatalog`
- project-to-library import with explicit overwrite policy

## Next product-boundary lane

After this Core PR is stable, the next stacked lane is the BricsCAD V25 adapter:

1. extract MEP candidates from native DWG entities;
2. classify/bind them into `MepElement`;
3. convert native entity extents into coordination envelopes;
4. run Core clash detection;
5. expose commands for MEP takeoff and clash review;
6. provide zoom/highlight/navigation without mutating canonical project state during read-only inspection;
7. record LOCAL_ONLY runtime/UI qualification requirements for licensed BricsCAD.

## Official source families used for this addendum

- Glodon Asia TAS User Guide.
- Glodon Asia TRB User Guide.
- Glodon Asia TBQ User Guide.
- Glodon official product/help pages for TAS/TRB/TME/TBQ.

The repository should continue mining these official guides in future parity passes because their indexed operational articles expose substantially more product behavior than the marketing overview alone.
