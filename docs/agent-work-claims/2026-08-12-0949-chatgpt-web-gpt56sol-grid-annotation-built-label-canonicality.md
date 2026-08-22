# Work claim — Generated Grid Annotation built-label canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-grid-annotation-built-label-canonicality`
- Registered: `2026-08-12T09:49:00+07:00`
- Completed: `2026-08-12T09:52:00+07:00`
- Baseline main SHA: `3f0915076869f92244a0b5b384bf157d2ef097ee`
- Priority: P1 — generated Grid Annotation built-label snapshots must preserve the exact writer-owned normalized label.
- Task Key: `CORE-GRID-ANNOTATION-BUILT-LABEL-CANONICALITY`

## Confirmed defect

`GridAnnotationBuilder.ReplaceOne(...)` reads `GridLabel` through a helper that trims the source text, then persists that normalized value into `GeneratedGridAnnotationLabel`. Health previously trimmed the built-label snapshot before compare, allowing malformed persisted values such as `" G1 "` to appear equal to current canonical `"G1"` without evidence.

## Implemented

- Claim: `fc47c66308f5ffbecd764f40961183ff94cdfe7f`
- Branch source: `8ebfee44df8a323306fad613c4d861f553f4d473`
- Branch smoke / reviewed PR head: `7ff726d4cc0b74c8331ac3f9b3c15d1f0c0abd55`
- PR: `#717`
- Squash merge on `main`: `f6d60e2a3bc3a70d4d0a3ab9bb4a34163b291c1e`

`GeneratedGridAnnotationHealthService` now preserves the raw built-label snapshot long enough to emit `GRID_ANNOTATION_BUILT_LABEL_NON_CANONICAL` for surrounding whitespace. Existing `GRID_ANNOTATION_LABEL_STALE` still compares the normalized snapshot, preserving stale semantics independently of spelling.

## Regression coverage

`GeneratedGridAnnotationBuiltLabelCanonicalitySmoke` covers a padded matching snapshot, a padded stale snapshot, exact canonical matching control, and canonical stale control.

## Validation

- Read back current provider and focused smoke from merged `main`.
- Compared squash merge `f6d60e2a3bc3a70d4d0a3ab9bb4a34163b291c1e` to later `main` `016b51e1c6d8e683c414d73613712ce762d792bd`: status `ahead`, `ahead_by=1`, `behind_by=0`, merge base exactly the squash commit; later change was unrelated.
- No GitHub Actions workflow was dispatched. No full .NET build or licensed BricsCAD V25/V26 runtime PASS is claimed from this remote lane.
