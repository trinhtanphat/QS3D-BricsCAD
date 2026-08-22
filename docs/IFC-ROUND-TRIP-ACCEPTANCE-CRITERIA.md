# IFC round-trip acceptance criteria

Status: `IFC-01` implementation gate for the QS3D-BricsCAD repository.

## Purpose

Any future IFC import/export implementation must preserve observable semantic identity and measurement evidence before it can be described as round-trip safe. QS3D semantic and measurement contracts remain canonical; IFC is an exchange boundary, not an alternate source of QS3D business truth.

## Required identity projection

For every supported exchanged object, round-trip comparison must retain a deterministic projection of:

- QS3D semantic identity;
- external IFC object identity;
- supported external class/type;
- supported quantity/property key, value, unit, and source evidence;
- supported classification identity;
- supported QS3D mapping or cost-item relation when one exists;
- any declared unsupported or lossy state.

Blank or duplicate managed identities are invalid. An external object without a trusted QS3D identity relation remains unmapped; display name, layer, description, or geometric resemblance must not silently create authoritative identity.

## Classification

Supported IFC class/type and classification references are retained as external evidence. Unknown or unsupported classifications remain explicit rather than being coerced into an arbitrary QS3D category. Any change to canonical QS3D classification must pass through the normal QS3D mapping/review boundary.

## Quantities and units

Supported imported quantities must retain their key, finite value, unit, external source identity, and enough provenance to explain the evidence.

- Non-finite values are rejected.
- Accepted numeric zero is canonicalized at the QS3D boundary.
- Missing or unsupported units are not guessed.
- Conversion reuses the existing QS3D unit path rather than an IFC-specific parallel formula.
- Exact duplicate evidence must not be counted twice; conflicting duplicate evidence is reported as ambiguous.
- External quantity evidence does not become authoritative QS3D measurement merely because its key resembles a QS3D quantity key.

Supported exported quantities come from the canonical QS3D quantity path. Export must not recalculate quantity through a separate IFC business-rule path. If required provenance or gross/deduction/net meaning cannot be represented, the result is declared lossy rather than lossless.

## Provenance

When available and supported, the round-trip projection retains:

- QS3D semantic identity;
- external object/source identity;
- quantity key and unit;
- evidence/source identity;
- QS3D rule identity/version when present;
- supported adjustment/deduction evidence;
- an explicit lossy indication when required evidence cannot be represented.

External evidence must not be assigned a fabricated QS3D rule identity.

## Mapping and cost relations

Classification, measurement, mapping, and cost relations remain distinct concepts. Imported classification is evidence even when no local mapping exists. Missing cost mapping remains unmapped rather than becoming an invented default or zero-cost relation. Existing canonical mapping/cost relations may be exported only when the implementation explicitly supports them.

## Determinism

With identical canonical QS3D input and identical declared exchange configuration, repeated round trips must yield an identical canonical comparison projection. Tests compare that projection, not raw file bytes. Collection ordering in the projection is deterministic and ambiguous duplicate identities are rejected.

## Required result states

A future IFC adapter must distinguish at least:

- supported for the declared fields;
- supported but lossy;
- unmapped;
- unsupported;
- invalid or ambiguous.

These states must remain visible to the caller rather than being collapsed into generic success.

## Minimum IFC-02 test matrix

A first implementation slice must add automated tests for every applicable item below:

1. identity-preserving export/import resolves to the same QS3D semantic identity;
2. duplicate external identity is reported as ambiguous;
3. unknown external object remains unmapped;
4. supported class/type survives the canonical round-trip projection;
5. unsupported class remains explicitly unsupported/unmapped;
6. supported quantity value and unit survive the declared conversion contract;
7. non-finite quantity is rejected before canonical QS3D quantity state;
8. numeric zero is canonical positive zero at the canonical boundary;
9. unknown/ambiguous unit is not silently converted;
10. duplicate quantity evidence cannot double-count and conflicting duplicates remain visible;
11. supported provenance survives the canonical projection;
12. provenance loss prevents a false lossless result;
13. supported classification identity survives the canonical projection;
14. supported existing mapping/cost relation survives while an absent relation remains unmapped;
15. repeated identical input yields the same canonical comparison projection.

## IFC-02 claim gate

Before broad source implementation, the IFC-02 claim must state:

- supported IFC schema/version and runtime/library boundary;
- supported object subset;
- exact QS3D-to-external identity relation;
- canonical projection used by round-trip tests;
- representation of unsupported/lossy/unmapped/invalid states;
- where existing QS3D unit, measurement, mapping, and cost services are reused;
- which test-matrix rows are covered by that implementation slice.

## Non-goals of IFC-01

This acceptance contract does not add an IFC parser/writer, select an external library or schema version, define full geometry fidelity, implement BCF, perform heuristic recognition, change QSDB persistence, or create new QS3D measurement/mapping/cost business rules.
