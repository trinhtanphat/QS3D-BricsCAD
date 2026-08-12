# Work claim — Quantity Rule FamilyId canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:56:00+07:00`
- Baseline main SHA: `238118d6cf138d2f34e8cdf87c97930986ef8ad7`
- Priority: P1 Core quantity-state integrity during owner-requested `continue all`
- Task Key: `CORE-QUANTITY-RULE-FAMILY-ID-CANONICALITY`

## Confirmed defect

`QuantityRuleEngine.ApplyMatching(...)` now validates dangling and category-incompatible Family references before stale-output cleanup, but `ResolveFamily(...)` still trims a nonblank persisted `ProjectElement.FamilyId` before lookup. A malformed stored relation such as `" FAM-1 "` is therefore silently normalized to `"FAM-1"` and the referenced Family's numeric properties can participate in managed quantity evaluation.

This conflicts with the repository's fail-closed semantic read-boundary integrity pattern for persisted nonblank Family/Floor/Zone relation IDs. The prior Quantity Rule Family-reference lane covered missing and category-mismatched references, not surrounding-whitespace canonicality.

## Reserved scope

- `src/QS3D.Core/Rules/QuantityRuleEngine.cs`
- one focused auto-registered Core smoke for Quantity Rule FamilyId canonicality
- this claim file for close-out

## Contract

- preserve null/empty/whitespace-only `FamilyId` as the existing valid no-Family state;
- for every nonblank `FamilyId`, require the persisted spelling to equal its trimmed spelling before project lookup;
- preserve case-insensitive project Family identity resolution when no surrounding whitespace is present;
- reject padded nonblank FamilyId before stale managed-output cleanup or generated quantity/provenance mutation;
- preserve existing dangling-Family, category-mismatch, valid Family-property projection, rule ordering and provenance behavior;
- do not broaden into Family assignment mutation, generic relation canonicalization, UI/native BricsCAD behavior, or variable-key policy.

## Validation plan

Add deterministic ModuleInitializer smoke coverage proving padded nonblank FamilyId fails closed before stale cleanup, whitespace-only FamilyId remains the no-Family state, and case-varied canonical FamilyId still projects valid Family numeric properties. Re-fetch source before write and inspect exact pushed diffs. No GitHub Actions dispatch, executable .NET smoke/build PASS, or licensed BricsCAD V25/V26 runtime qualification will be claimed unless actually executed.
