# QSDB Named Category Token Plan

## Goal

Require persisted `ElementCategory` values to use the symbolic token format produced by QS3D, while retaining case-insensitive compatibility for valid names.

## Evidence

`QsdbProjectStore.Category(...)` parses with `Enum.TryParse(..., true)` and `Enum.IsDefined(...)`; that combination accepts numeric strings for currently defined enum ordinals. The serializer writes symbolic names, so numeric aliases create a second persisted representation of the same semantic category.

## Implementation

1. Extend `QsdbProjectXmlSchemaValidator` with one category-token helper.
2. Require the `category` attribute to be present/canonical text.
3. Parse to a defined `ElementCategory` and require `Enum.GetName(...)` to match the token case-insensitively.
4. Apply the helper to persisted Family, QuantityRule and ProjectElement entries.
5. Add isolated Core smoke coverage:
   - numeric family category rejected;
   - numeric quantity-rule category rejected;
   - numeric element category rejected;
   - lower-case valid symbolic tokens remain loadable.

## Safety

- No schema bump and no enum/value changes.
- No rule calculation or native CAD changes.
- Serializer remains unchanged.
- No Actions/release dispatch.
