# Work claim — EntitySnapshot identifier control-character canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-entitysnapshot-identifier-control-20260814`
- Registered UTC: `2026-08-14T01:41:00Z`
- Baseline main SHA: `023a2d89e719d8051dbcd0fa91e780de6fe1ebaf`
- Priority: `P1 foundation hardening`

## Verified defect

Existing EntitySnapshot canonicalization explicitly trims surrounding whitespace from both CAD `Handle` and `EntityType`, and regression coverage preserves those canonical values through recognition. The current constructor still accepts internal control characters in either identifier. A malformed handle can therefore cross the canonical CAD identity boundary, while a malformed entity type can enter recognition normalization/ranking inputs even though both fields are already treated as canonical identifiers.

## Reserved scope

- `src/QS3D.Core/Model/EntitySnapshot.cs` — Handle/EntityType canonicality only
- one focused self-registering Core smoke regression
- this claim file

## Bounded implementation

- preserve existing blank rejection and surrounding-whitespace trim behavior for Handle and EntityType;
- reject control characters after canonical trim for both identifiers;
- preserve Layer, Metadata, metric validation, generated-ownership marker and all recognition/capture semantics;
- do not restrict CAD handles to a hexadecimal grammar, change EntityType casing, or modify adapters/host/UI code.

## Validation plan

Focused smoke will cover newline/tab/control rejection for Handle and EntityType and preservation of existing trim/casing behavior for valid values. No GitHub Actions will be dispatched. Managed/native PASS will only be reported if actually executed. No force-push.
