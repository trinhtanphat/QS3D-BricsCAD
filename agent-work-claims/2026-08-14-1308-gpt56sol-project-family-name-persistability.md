# Work claim — ProjectFamily name persistability

- Status: `COMPLETED`
- Agent: `gpt56sol-project-family-name-persistability-20260814-1308`
- Registered: `2026-08-14T13:08:00+07:00`
- Completed: `2026-08-14T13:12:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline: `da647229e9faf84496306428206f813251ebb7d6`
- Claim commit: `008b668766bc4ea27d7b072dacc6d418f3cb131b`
- Claim reconciliation: `210dbad5cf9961b3ad28d10d8ed3abe10f681a0f`
- Pre-write source blob: `66e1ef69119e9df6fd6dfeba7a9050904f5c5a81`
- Source: `044f652c7d26b99482a33f84e4af693c921fc5df`
- Regression: `95cf4bd42b950dce89c81c9756eb4728add8b7f8`

## Confirmed defect

`ProjectFamily.Name` is a persisted mutable domain field. Both the public constructor and public `Name` setter used `RequireName`, which rejected only blank input and trimmed surrounding whitespace. An embedded XML-invalid control character such as `U+0001` was therefore accepted as a family name.

QSDB serializes family names directly to XML attributes and validates XML characters before publication. A supported constructor or rename could therefore create a family state that failed only when persisted. This was a narrow writer-boundary persistability defect independent of the completed ProjectFamily id lane.

## Completed change

- `ProjectFamily.RequireName` now preserves blank rejection and surrounding-whitespace normalization while rejecting control characters in the normalized name.
- The same guard applies to construction and mutable `Name` assignment through the existing shared helper.
- Valid rename semantics, including `PropertyChanged`, remain unchanged.
- ProjectFamily id/category/property maps, family assignment services, Zone/Floor/Project identity/name surfaces and QSDB schema/migration behavior were not changed.

## Regression coverage

Added self-registering `ProjectFamilyNamePersistabilitySmoke` which pins:

1. canonical names remain accepted;
2. padded constructor names normalize to canonical text;
3. valid padded rename still normalizes and emits exactly one `PropertyChanged` event for `Name`;
4. constructor rejects a `U+0001` family name;
5. setter rejects a `U+0001` rename before state mutation;
6. failed rename preserves the prior name and emits no `PropertyChanged` event.

## Validation

Remote GitHub diff for source commit `044f652c7d26b99482a33f84e4af693c921fc5df` confirms exactly one source hunk: the `ProjectFamily.RequireName` helper. Remote regression diff at `95cf4bd42b950dce89c81c9756eb4728add8b7f8` confirms the dedicated smoke uses the real `ElementCategory.ArchitecturalWall` enum and C# `\u0001` literals. GitHub compare reports the regression SHA is ahead of the source SHA with `044f652c7d26b99482a33f84e4af693c921fc5df` as merge base; the unrelated intervening workspace-footer commit does not touch this lane.

Executable .NET/native validation was **not run** in this environment because there is no local checkout/.NET/native runner. No GitHub Actions were dispatched and no BricsCAD/native/runtime PASS is claimed.

## Completion condition

Satisfied: claim-first reservation, live baseline reconciliation, isolated family-name writer fix, focused regression source, remote diff/ancestry verification and explicit validation limitations are present on `main`.
