# Work claim — Material XLSX cell text limit

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-material-xlsx-text-20260812-0103`
- Registered: `2026-08-12T01:03:00+07:00`
- Completed: `2026-08-12T01:06:00+07:00`
- Baseline main SHA: `198df88b4ee48bb977f1e1dc0f4292cd035624ea`
- Claim commit: `d2140b7085b3863cf132c4bb140cccf5da1e2974`
- Source fix commit: `280b901c260cfd510c5308535bd30b677b63b900`
- Regression commit: `00f52057a7dfb9110c172e8b0e36781db619c140`
- Priority: P2 evidence-driven remote-safe XLSX integrity hardening

## Confirmed defect

`MaterialUsageXlsxExporter` enforced the worksheet row limit but wrote every inline-string cell directly through `XlsxXmlText.Escape(...)` without enforcing Excel's 32,767-character cell-content limit. `MaterialUsageRow` exposes unrestricted string properties, so a caller could provide a valid row whose Floor, MaterialName, UnitHint, Component, Category or FamilyName exceeded the XLSX cell limit. The exporter could then publish a structurally valid ZIP/XML package with an invalid Excel cell value.

## Implemented

- Added `MaxCellTextCharacters = 32767` to `MaterialUsageXlsxExporter`.
- Every row string field exported as an inline cell is validated before `Path.GetFullPath`, directory creation, temp-file creation or package writing.
- Null runtime strings retain existing empty-string behavior; ordinary values and exactly 32,767-character values remain accepted.
- Values longer than 32,767 characters fail with `ArgumentOutOfRangeException` before destination filesystem mutation.
- Existing worksheet row limit, `XlsxXmlText` sanitization, numeric finite checks, package validation and atomic publication remain unchanged.
- Added module-initializer regression `MaterialUsageXlsxCellTextLimitSmoke` proving exactly 32,767 characters can publish and 32,768 characters are rejected without creating the destination directory/file.

## Implemented surfaces

- `src/QS3D.Core/Export/MaterialUsageXlsxExporter.cs`
- `tests/QS3D.Core.SmokeTests/MaterialUsageXlsxCellTextLimitSmoke.cs`
- this claim file

## Excluded scope honored

- No `XlsxXmlText` shared-policy changes.
- No Door/Opening or Curtain XLSX exporter changes.
- No Material Usage schedule grouping/catalog/business-rule changes.
- No UI/native BricsCAD/runtime or GitHub Actions work.

## Validation actually performed

- Standalone claim commit was verified as an ancestor of current `main`; subsequent commits before implementation were disjoint from the reserved Material exporter/test surfaces.
- Source write used the exact current blob SHA and current `main` was re-read afterward, confirming pre-filesystem cell-text validation is present.
- Focused smoke source was re-read on current `main`, confirming both exact-limit acceptance and oversized side-effect-free rejection.
- Regression commit `00f52057a7dfb9110c172e8b0e36781db619c140` was verified as an ancestor of later current `main` `0778ff7619cd36941fcdf050aae298e3400f28ff`; subsequent compare showed only a disjoint semantic-sheet preflight file.
- No force push/reset/revert was used.
- No local .NET smoke execution is claimed in this connector-only lane.
- No BricsCAD V25/V26 runtime or GitHub Actions execution is claimed.

## Coordination

The completed Material Usage group-key collision lane owned `MaterialUsageSchedule.cs`, not this exporter. The active Door XLSX cell-limit claim explicitly excluded other exporters. This claim reserved only Material Usage XLSX serialization and its dedicated smoke.

## Completion condition

Completed. Material Usage XLSX now enforces the 32,767-character cell text limit before filesystem mutation, focused regression source is present on current `main`, exact commits are recorded above, and concurrent history was preserved.
