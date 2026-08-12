# Work claim — Quantity Rule FamilyId canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:56:00+07:00`
- Completed: `2026-08-12T09:59:00+07:00`
- Baseline main SHA: `238118d6cf138d2f34e8cdf87c97930986ef8ad7`
- Claim commit: `02091b5b876be653b870e2c546425d613c27609d`
- Source fix commit: `e76abeb712497e11dbed3cd744fd8f687029d7f8`
- Regression registration commit: `b1323bf2363fdec9454b80dc4c7fe2a2e1a7fe50`
- Effective regression correction commit: `77ff0259a0a85a9ced55518b9faff65e886b4cda`
- Priority: P1 Core quantity-state integrity during owner-requested `continue all`
- Task Key: `CORE-QUANTITY-RULE-FAMILY-ID-CANONICALITY`

## Confirmed defect

`QuantityRuleEngine.ApplyMatching(...)` validated dangling and category-incompatible Family references before stale-output cleanup, but `ResolveFamily(...)` still trimmed a nonblank persisted/runtime `ProjectElement.FamilyId` before lookup. A malformed relation such as `" FAM-1 "` was therefore silently normalized to `"FAM-1"` and the referenced Family's numeric properties could participate in managed quantity evaluation.

This conflicted with the repository's fail-closed semantic read-boundary integrity pattern for persisted nonblank Family/Floor/Zone relation IDs. The prior Quantity Rule Family-reference lane covered missing and category-mismatched references, not surrounding-whitespace canonicality.

## Implemented contract

- `ResolveFamily(...)` now distinguishes the raw stored FamilyId from its trimmed lookup token;
- null/empty/whitespace-only `FamilyId` remains the valid no-Family state;
- every nonblank FamilyId must already equal its trimmed spelling before lookup;
- case-insensitive project Family identity lookup is preserved when no surrounding whitespace is present;
- padded nonblank FamilyId fails before active-rule/stale-output processing, so stale managed quantities/provenance are not removed and new outputs are not created;
- dangling-Family, category-mismatch, valid Family-property projection, rule ordering and provenance behavior remain unchanged.

## Regression coverage

`QuantityRuleFamilyIdCanonicalitySmoke` is auto-registered with a module initializer and covers:

- a runtime-mutated padded nonblank `FamilyId` fails closed before stale quantity/provenance cleanup or new output creation;
- a runtime-mutated whitespace-only `FamilyId` remains the no-Family state and still evaluates the built-in `Count` variable;
- a case-varied but whitespace-canonical `FamilyId` still resolves case-insensitively and projects Family numeric properties.

The first regression commit was immediately corrected after readback showed the `ProjectElement` constructor trims relation IDs; the effective smoke now assigns malformed/whitespace-only values through the public mutable `FamilyId` property so the regression exercises the actual runtime malformed-state surface.

## Validation performed

- Current-main source readback confirmed `QuantityRuleEngine.cs` retains the raw-vs-trimmed FamilyId guard with blob SHA `72793cc78e414ddf360135a4a43ba883956d1c09`.
- Current-main regression readback confirmed `QuantityRuleFamilyIdCanonicalitySmoke.cs` uses public mutable `FamilyId` to exercise padded/blank runtime state and is present with blob SHA `7b4de9ae25a223b78c024b0d58f6656a53aa7066`.
- No GitHub Actions were dispatched. No executable .NET smoke/full build PASS and no licensed BricsCAD V25/V26 runtime qualification are claimed from this connector-only session.

## Coordination

The earlier Quantity Rule Family-reference integrity claim was already completed and covered dangling/category-mismatch behavior only. Recent Quantity Rule claims covered provenance generic editing, duplicate/null rule integrity and variable projection; no newer active claim reserved FamilyId whitespace canonicality before this source write.

## Completion

`COMPLETED`: Quantity Rule evaluation no longer silently normalizes padded nonblank FamilyId state into a valid Family projection, while blank absence and case-insensitive canonical Family lookup remain compatible.
