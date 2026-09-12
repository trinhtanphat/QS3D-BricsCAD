# Takeoff Package duplicate evidence guard

Autodesk-style Takeoff Package admission now rejects duplicate drawing evidence identities within the same logical sheet before quantity aggregation, formula evaluation, inventory publication, or estimating.

## Contract

- Identity is sheet-local: `(SheetId, MarkupId)` is unique within a package revision.
- Reusing the same `MarkupId` on a different sheet remains valid.
- Duplicate evidence emits `PKG.DUPLICATE_EVIDENCE` at `Error` severity and leaves the package `Blocked` with an empty inventory.
- Existing source-reference, revision, orphan-evidence, classification, formula, quantity and rate validation behavior is unchanged.

This closes a raw-package admission gap: `TakeoffSheetResult2D` already rejects duplicate markup IDs, but callers can construct package inputs directly from deserialized evidence. Without the package guard, a repeated row could be counted twice in Drawing/BIM → Package → Classification → Quantity → Formula → Inventory → Estimate.

## Compatibility

No DTO shape changes are required. Valid packages are unaffected. Integrations that previously sent the same drawing evidence more than once must deduplicate by sheet and markup identity before package build.
