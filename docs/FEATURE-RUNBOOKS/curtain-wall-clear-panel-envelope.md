# Curtain wall schedule clear-panel envelope integrity

Lane-Key: `issue-5190`
Reservation-Protocol: `v2`
Canonical carrier: `agent/longnguyentuan2107-maker-c02-20260901-10/issue-5190-curtain-envelope`
Ownership-Key: `core.reporting.curtain-clear-panel-envelope-integrity-v1`

## User-visible invariant

Each glass-wall element contributes a clear-panel width interval and height interval to the grouped curtain-wall schedule. The semantic interval must be ordered on each axis:

- `CurtainMinClearPanelWidthM <= CurtainMaxClearPanelWidthM`
- `CurtainMinClearPanelHeightM <= CurtainMaxClearPanelHeightM`

All four quantities retain the historical finite/non-negative validation and missing-key fallback to zero. The new contract only rejects an inverted interval; it does not infer missing dimensions or impose a positive minimum.

## Failure ordering

`CurtainWallScheduleBuilder` reads the four quantities once and validates both intervals before creating or mutating the grouped schedule row. Therefore a malformed element cannot increment wall/panel/frame counts, enter compensated aggregates, alter row min/max state, or append element/source-handle provenance before its envelope is rejected.

After validation, the exact captured values are used for the grouped `Math.Min`/`Math.Max` aggregation. The quantities are not re-read later in the same element pass.

## Deterministic regression

`CurtainWallScheduleSmoke` covers both failure axes independently:

- width minimum 2.0 m / maximum 1.0 m must fail with an element-scoped width-envelope diagnostic;
- height minimum 2.5 m / maximum 1.5 m must fail with an element-scoped height-envelope diagnostic.

The existing two-wall grouped control remains and proves the stable aggregate envelope (`1.30..1.45 m` width and `1.35..1.45 m` height), integer aggregation, compensated quantities, grouping and provenance.

`scripts/preflight-curtain-wall-clear-panel-envelope.py` pins source/test presence and the ordering `element -> envelope validation -> grouped row creation/mutation -> envelope aggregation`.

## Acceptance boundary

Runtime: `NOT_APPLICABLE`.

This carrier changes deterministic Core reporting/quantity integrity only. Acceptance requires fresh exact-head protected `preflight` and `core` SUCCESS. No licensed BricsCAD `LOCAL_PASS` is required or claimed.
