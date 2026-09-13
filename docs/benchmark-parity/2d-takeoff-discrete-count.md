# 2D Takeoff discrete count evidence

## Problem

`TakeoffMarkup2D` previously accepted any positive finite `rawValue` for all measurement kinds. That is correct for calibrated length and area geometry, but not for `TakeoffMeasurementKind.Count`: a value such as `1.5` could be emitted unchanged as `1.5 ea` and continue through Takeoff Package quantity, formula, inventory, and estimate evaluation.

## Behavior

Count markup values are now required to be positive finite whole numbers. Fractional count values fail at the markup boundary with `ArgumentOutOfRangeException` before calibrated evidence can be produced. Valid integer counts retain the existing `ea` unit and pass unchanged through `CalibratedTakeoffEngine2D`, package validation, quantity grouping, formulas, rates, inventory, and estimate calculation.

Length and area markups keep their existing positive finite continuous-value contract; fractional geometry remains valid and continues to use drawing calibration.

## Compatibility and migration

This is a validation hardening with no public type, enum, constructor signature, result shape, package API, revision comparison, classification, zone/layer, or estimate schema change.

Existing callers that intentionally encoded fractional discrete items as Count must migrate those inputs to an appropriate continuous measurement/classification or normalize upstream data to a whole item count. Whole-number Count inputs require no migration.

The regression smoke covers fractional-count rejection, fractional-length compatibility, calibrated whole-count evidence, and the end-to-end Drawing → Package → Quantity → Formula → Inventory → Estimate path.
