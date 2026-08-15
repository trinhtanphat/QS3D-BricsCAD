# Work claim — Semantic Documentation catalog XML current-main recovery

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-documentation-catalog-xml-main-recovery-20260815`
- Registered: `2026-08-15T10:04+07:00`
- Exact main baseline: `5ddd99af06bd209c562cd2c60cb22563ef565528`
- Latest reconciled main: `d521a3f95ee0ed80f12335e2f6affa59ce21fa9d`
- Issue: `#1500`
- Replacement current-main PR: `#1539` (`ready for review`, latest readback `mergeable=true`)
- Superseded integration-v2 PR: `#1502` (`closed`, not merged)
- Branch: `agent/chatgpt-gpt56sol/documentation-catalog-xml-main-recovery-20260815`
- Priority: Core P1 persisted documentation integrity

## Confirmed current-main defect

`SemanticDocumentationCatalogStore.CanonicalRequiredText(...)` and `CanonicalOptionalText(...)` accepted XML-illegal UTF-16 on current main and only failed later during XML materialization. The completed integration-v2 lane #1500/#1502 had already established the narrow correct contract: XML representability must be checked at the centralized persisted-text boundary before project mutation/persistence publication.

## Recovered implementation

- `src/QS3D.Core/Documentation/SemanticDocumentationCatalogStore.cs`: required/optional canonical persisted text now routes through `XmlConvert.VerifyXmlChars(...)`; invalid XML text becomes `InvalidOperationException` at the store boundary.
- `tests/QS3D.Core.SmokeTests/SemanticDocumentationCatalogXmlPersistabilitySmoke.cs`: invalid view/sheet/title-block text is rejected without changing project revision/timestamp/metadata; valid supplementary Unicode round-trips through Documentation Store and QSDB.
- `tests/QS3D.Core.SmokeTests/SemanticDocumentationCatalogXmlPersistabilityRegistration.cs`: module-registers the focused smoke.
- Current-main catalog-save freshness protection is preserved unchanged.

## Evidence

- claim-only: `8eecac11ea14eb94999842600dfc7bec6d33e5cb`
- recovery implementation: `e06753b2d3826bb98268a960f5cda17664a5d750`
- non-force reconciliation onto `d521a3f95ee0ed80f12335e2f6affa59ce21fa9d`: `79ca9935b177f1d89753c3cf4bea95db1c5d71b1`
- original reviewed source: `6814c81b4067595f34523446662b6899aabebb85`
- original reviewed smoke: `6693460abb4036741894de33e6e1f08b1bcd2959`
- original reviewed registration: `60c20f46d0817325ccaadd3a02bfd07689506ef7`
- final compare before PR: ahead 3 / behind 0; exactly four task files
- production source delta: +15/-2 only in the centralized XML-text helpers
- exact GitHub source/diff readback: PASS
- managed build/smoke in this recovery session: NOT_RUN; no `dotnet` execution available and no PASS claimed
- BricsCAD runtime: NOT_RUN, outside this Core-only lane
- GitHub Actions: not manually dispatched/rerun

## Coordination / exclusions

- #1502 was closed only after #1539 was verified clean/mergeable; its branch/history remains intact.
- No `SemanticViewPlanner`, `SemanticSheetPlanner`, Semantic Schedule, ProjectState, BCF/IFC, native documentation runtime, schema, workflow/release or product-boundary changes.
- No direct main merge by this normal-agent session.

## Handoff / release

All recovery source/regression state is represented by ready PR #1539 against `main`. Reservation ownership is released from this session. Keep Issue #1500 open until an authorized coordinator integrates #1539; only then may the issue/claim be closed as completed after remote ancestry/readback verification.

## Integration closeout

- Authorized integration PR `#1539` merged at exact main SHA
  `88f83db19ed5dfd85606d5a5e00adfc28f4fd99c`; issue `#1500` closed through
  that merge.
- Exact-merge validation: documentation catalog schema, bounded-save,
  structural-freshness, and smoke-registration gates PASS;
  `QS3D.Core.SmokeTests` Release build completed with 0 warnings / 0 errors;
  full deterministic Core smoke reported `ALL PASS`.
- No BricsCAD/native runtime or GitHub Actions operation was performed during
  integration.
