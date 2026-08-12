# Wall Quantity Null Opening Plan

## Goal

Prevent malformed opening collections from silently understating wall deductions while preserving `null` collection = no openings.

## Implementation

1. Re-fetch `WallQuantityCalculator.cs` immediately before editing.
2. In the opening enumeration, replace the silent null skip with a fail-closed argument exception that identifies the openings input.
3. Leave dimension/overflow validation and gross-area clamping unchanged.
4. Add isolated smoke coverage for:
   - null collection retains existing gross/net result;
   - a collection containing a null entry is rejected;
   - valid openings still calculate/clamp exactly as before.

## Safety

No changes to semantic regenerators, physical opening cuts, host links, reporting/export, native BricsCAD code, Actions, or releases.
