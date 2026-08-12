# Work claim — BOM property key canonicality

- Status: `COMPLETE`
- Agent: `chatgpt-web-gpt56sol-bom-property-key-20260812-0033`
- Registered: `2026-08-12T00:33:00+07:00`
- Baseline main SHA observed before registration: `f546bac4c06f263699158288878d36f7b65066c9`
- Claim commit SHA: `79c778bdbb0bbbeb89792ba971e75f7d741b6f63`
- Source implementation SHA: `65bbaeb0af2072a1b38eee161494cfb90c872574`
- Regression test SHA: `55a3faa3bcda1533d45fdd961a5bb011d4a3221a`
- Priority: P2 source-proven release-integrity regression hardening

## Reserved scope

Fix the Core BOM release guard mismatch where QSDB persistence rejects blank or surrounding-whitespace semantic property keys, while `BomReleaseGuardService` did not inspect property-key canonicality. `ProjectElement.Properties` is publicly mutable, and `ProjectQuantityReportBuilder` reads canonical property names such as `MaterialName`, `Mark`, and dimensional inputs by dictionary lookup, so a malformed key such as `" MaterialName "` could be silently treated as missing/default during BQ construction instead of blocking release.

## Implemented

- `BomReleaseGuardService.Inspect(...)` now emits Error-level `BOM_PROPERTY_KEY_INVALID` for blank or surrounding-whitespace property keys on included semantic elements.
- Existing quantity-key, finite-value, report grouping, provenance, generated-handle liveness, exclusion, and exception-redaction behavior remains unchanged.
- `BomReleaseGuardSmoke` now includes `NonCanonicalPropertyKeyBlocksRelease()`, mutating the public dictionary with `" MaterialName "` and asserting the new Error-level issue belongs to the owning element.

## Validation performed

- Verified claim-first ancestry: the implementation commits descend from standalone claim commit `79c778bdbb0bbbeb89792ba971e75f7d741b6f63`.
- Re-read current `main` source after implementation and confirmed `BOM_PROPERTY_KEY_INVALID` is present in `BomReleaseGuardService.cs`.
- Re-read current `main` smoke source and confirmed `NonCanonicalPropertyKeyBlocksRelease()` is registered from `Run()` and asserts Error severity plus owner element ID.
- Did not dispatch GitHub Actions.
- No local/full .NET smoke execution is claimed in this environment; validation here is repository/source-level plus remote-main readback.

## Explicit exclusions retained

- No report-builder behavior changes.
- No project/QSDB persistence or schema changes.
- No generic property edit-policy changes.
- No BricsCAD/native/runtime changes.
- No GitHub Actions dispatch or workflow edits.

## Handoff

This claim is complete. Future work may independently review other BOM/report invariants, but must not reopen these surfaces without a new non-overlapping claim and a fresh `main` re-read.
