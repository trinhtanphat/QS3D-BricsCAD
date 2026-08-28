# Issue #4321 — Project snapshot Family/Element property invariants

Status: `SOURCE_FIX_ACTIVE`

Lane-Key: `issue-4321`

Canonical owner: independent QS3D schedule worker `C02`

Runtime: `NOT_APPLICABLE` — deterministic Core state/rollback integrity.

## Problem

`ProjectFamily.Properties` and `ProjectElement.Properties` are mutable dictionaries retained for compatibility. Normal mutation paths validate property identity/text, but snapshot clone/rollback previously bounded only collection size and then raw-copied entries. Caller-injected invalid dictionary state could therefore become detached or rollback truth.

## Hardened contract

- Family property state is prevalidated through the canonical `ProjectFamilyService.SnapshotProperties(...)` contract, including canonical key identity, XML text, 120-character key bound and 1000-character value bound.
- Element property keys must be nonblank, control-free, XML-valid and already canonical under trim normalization; trim/case canonical collisions fail closed.
- Element property values follow the existing `ProjectElement.SetProperty(...)` persistability contract: malformed XML/UTF-16 fails closed while XML-valid TAB/LF/CR and supplementary-plane Unicode remain accepted.
- Validation runs before detached clone/rollback materialization and does not mutate the source object.
- Copy uses only validated canonical Family/Element property entries. It deliberately does not call `ProjectElement.SetProperty(...)` during rollback materialization because that setter can mark generated output stale; snapshot restore must reproduce captured state rather than create new semantic side effects.
- Existing quantity canonicalization from #4311, collection bounds, identity-preserving rollback, dirty flags, timestamps and project revision restoration remain unchanged.

## Regression

`ProjectStateSnapshotElementIdentitySmoke` covers padded/control/malformed keys, malformed values, Family length bounds, source-state non-mutation, valid supplementary-plane Unicode plus XML-valid control-bearing values, and existing quantity/identity/rollback controls.

`preflight-project-snapshot-property-invariants.py` is auto-discovered and pins Family service validation, Element property canonical/XML validation, collision rejection and focused regression controls.

## Landing

Use canonical branch `agent/c02/issue-4321-project-snapshot-property-invariants`. Required endpoint: automatic exact-head branch CI, latest-main reconciliation when needed, one canonical PR with `Lane-Key: issue-4321`, protected current-candidate `preflight` + `core` SUCCESS, expected-head merge and exact resulting `main` verification.

No licensed BricsCAD host, private DWG or `LOCAL_PASS` evidence applies.
