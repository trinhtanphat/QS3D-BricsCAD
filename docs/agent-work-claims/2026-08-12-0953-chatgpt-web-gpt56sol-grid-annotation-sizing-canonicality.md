# Work claim — Generated Grid Annotation sizing canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-grid-annotation-sizing-canonicality`
- Registered: `2026-08-12T09:53:00+07:00`
- Completed: `2026-08-12T09:57:00+07:00`
- Baseline main SHA: `9479e0ea6944e5f018431c7ec0634912a13aef8c`
- Priority: P1 — generated Grid Annotation sizing snapshots must preserve the exact writer-owned round-trip numeric spelling.
- Task Key: `CORE-GRID-ANNOTATION-SIZING-CANONICALITY`

## Confirmed defect

`GridAnnotationBuilder.ReplaceOne(...)` persists both `GridBubbleRadiusM` and `GridTextHeightM` with `double.ToString("R", CultureInfo.InvariantCulture)`. Health previously normalized and parsed the stored strings without checking writer-owned spelling, allowing padded, trailing-zero or scientific aliases to pass.

## Implemented

- Claim: `dae1e356c251edb699f28f3434468385d8a55c81`
- Branch source: `1a9bbbaf28389097206ec44675c5c4b37cf08015`
- Branch smoke / reviewed PR head: `616b110eaa2068ac280ef7274a2e3d17a5ff163b`
- PR: `#725`
- Squash merge on `main`: `eea7eefdb45e7548be7b1abdd06d7a690ac0dbf5`

`ValidateSizing(...)` now establishes positive finite numeric validity first, then compares each raw sizing snapshot with parsed `ToString("R", InvariantCulture)`. Non-canonical radius/text-height spellings emit `GRID_ANNOTATION_BUBBLE_RADIUS_NON_CANONICAL` / `GRID_ANNOTATION_TEXT_HEIGHT_NON_CANONICAL`. Existing invalid and ratio diagnostics remain intact.

## Regression coverage

`GeneratedGridAnnotationSizingCanonicalitySmoke` covers padded radius, trailing-zero text height, scientific notation, invalid precedence, ratio preservation and exact canonical controls.

## Validation

- Read back current provider and focused smoke from merged `main`.
- Compared squash merge `eea7eefdb45e7548be7b1abdd06d7a690ac0dbf5` to later `main` `7f6712bd96641bd0cb6ee6bdcffa57c130997a9f`: status `ahead`, `ahead_by=3`, `behind_by=0`, merge base exactly the squash commit; later changes were unrelated.
- No GitHub Actions workflow was dispatched. No full .NET build or licensed BricsCAD V25/V26 runtime PASS is claimed from this remote lane.
