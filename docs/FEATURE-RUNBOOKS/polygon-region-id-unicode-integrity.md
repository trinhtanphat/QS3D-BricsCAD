# Polygon region ID Unicode integrity

## Scope

This runbook qualifies the deterministic Core identity boundary for `PolygonRegionSetTopology.NormalizeAndValidate`. It does not change polygon geometry, mesh planning, native BricsCAD ownership, or the LOCAL_ONLY runtime matrix under parent #83.

## Defect

Before Issue #5192, `NormalizeRegionId` trimmed RegionId text, bounded its length, rejected blank/control text, and enforced case-insensitive uniqueness, but it did not reject malformed UTF-16 or XML-invalid non-control text. A lone surrogate or XML-invalid noncharacter could therefore become the stable RegionId retained by canonical topology islands and tagged scan segments even though strict UTF-8/XML persistence surfaces cannot round-trip that identity safely.

## Contract

A published polygon RegionId must:

- remain non-blank after trimming and stay within the existing 160-character limit;
- contain no control characters;
- be well-formed Unicode under strict UTF-8 encoding, so lone high/low surrogates fail closed;
- be valid XML character data before topology publication;
- retain existing case-insensitive duplicate rejection and deterministic canonical ordering;
- preserve valid supplementary-plane Unicode exactly after trimming, including propagation into tagged scan segments.

The validation is admission-only. It does not normalize case, replace invalid text, silently drop characters, or alter valid geometry/topology behavior.

## Deterministic evidence

`PolygonRegionSetTopologySmoke` covers lone high/low surrogates, an XML-invalid U+FFFE RegionId, and a valid supplementary-plane RegionId that survives both canonical island publication and scan-segment tagging. `scripts/preflight-polygon-region-id-unicode-integrity.py` pins the production validation and regression tokens.

Runtime classification: `NOT_APPLICABLE`. Core smoke/static/protected CI is authoritative for this bounded contract; no licensed BricsCAD `LOCAL_PASS` is implied.
