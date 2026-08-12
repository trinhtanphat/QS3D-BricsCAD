# Work claim — Room finish generator structural read-only result

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-room-finish-generator-readonly-result-20260812-0843`
- Registered: `2026-08-12T08:43:00+07:00`
- Completed: `2026-08-12T08:44:00+07:00`
- Baseline main SHA: `0970f1cb7779bcd95d2617c80e66dabb341c1b2a`
- Claim commit: `33244992266470a41dad7e72039e553d769cf2c5`
- Source commit: `1a5be3fac8bfdfde12b42177b2090e903327b318`
- Regression commit: `154397a6fd3c13676648ddaa312839786fa7edcb`
- Priority: evidence-driven public result ownership during owner-requested `continue all`

## Confirmed defect fixed

`RoomFinishGenerator.Generate(...)` declares `IReadOnlyList<ElementInstance>` but previously returned its mutable backing `List<ElementInstance>` directly. Callers could cast the generated preview/result collection to a mutable collection and structurally add, remove or clear generated finish instances after generation had completed.

## Completed change

The generated finish result is now returned through `output.AsReadOnly()`. Room-category validation, numeric finite/non-negative checks, enabled finish categories, IDs/families/floor/material/source-handle propagation and generated metric values are unchanged. No deep-immutability redesign of `ElementInstance` was made.

## Regression evidence

`RoomFinishGeneratorReadOnlyResultSmoke` generates representative Floor Finish + Skirting outputs, verifies order/category/area/length/floor/material/source-handle propagation, requires the returned `ICollection<ElementInstance>` to be read-only, and proves structural `Add` throws `NotSupportedException`.

## Read-back validation

Current `main` source was re-fetched after publication and contains `return output.AsReadOnly();`. The focused smoke was also re-fetched from `main` with the intended semantic and mutation-boundary checks intact.

## Coordination respected

The previous room-finish-generator numeric-safety contract remains unchanged. This lane did not edit Room Finish synchronization/health, Auto Room lifecycle, native generation, UI or existing numeric smoke files.

## Validation boundary

Remote source/smoke read-back only. No GitHub Actions were dispatched; no executable Core build/smoke PASS and no BricsCAD V25/V26 runtime qualification are claimed.
