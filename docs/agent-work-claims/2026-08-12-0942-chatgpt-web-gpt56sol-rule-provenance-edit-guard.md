# Work claim — Quantity Rule provenance generic-edit guard

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:42:00+07:00`
- Completed: `2026-08-12T09:46:00+07:00`
- Baseline main SHA: `661cc8400397aeb74a2695ffec69bb49bab33f93`
- Claim commit: `688c7c13f086d8035f240e8371efd0249ea94420`
- Source fix commit: `10fc019d20aa9c81f6ee78077a4f86694d307795`
- Regression smoke commit: `36c223e92285dc1f9d22ce6308ed0d58b0b1c8c2`
- Priority: P1 Core quantity-state integrity during owner-requested `continue all`
- Task Key: `CORE-RULE-PROVENANCE-GENERIC-EDIT-GUARD`

## Confirmed defect

`QuantityRuleEngine` reserves `ProjectElement.Properties` keys under the `Rule:` prefix as managed quantity-rule provenance. `GetStaleManagedOutputs(...)` interprets those keys as ownership markers and may remove the corresponding quantity/provenance when the output is no longer active.

The shared `SemanticPropertyEditPolicy` used by both `BulkEditService` and `SemanticSelectionBulkEditService` already blocked semantic identity/reference, CAD-derived and native/generated ownership namespaces, but did not reserve `Rule:`. Generic property editing could therefore create or overwrite `Rule:<output>` metadata that the quantity-rule lifecycle later trusted as internal provenance, allowing user/bulk edits to spoof managed-output ownership and trigger stale cleanup of an unrelated quantity.

## Implemented contract

- `SemanticPropertyEditPolicy` now rejects any canonical trimmed key starting with `Rule:` case-insensitively;
- rejection occurs in the shared preflight before low-level or selection bulk target mutation;
- both `BulkEditService` and `SemanticSelectionBulkEditService` inherit the guard without duplicated policy;
- ordinary user-defined keys outside the reserved namespace, including `RuleFactor`, remain editable;
- Quantity Rule evaluation/provenance read/cleanup behavior and inspector-only relation work are unchanged.

## Regression coverage

`QuantityRuleProvenanceEditGuardSmoke` is auto-registered with a module initializer and covers:

- public editability classification rejects canonical, padded and case-varied `Rule:` keys;
- both low-level and selection bulk editors reject provenance spoofing before element/project persistence mutation;
- a manual quantity survives the rejected spoof attempt and a subsequent no-rule `QuantityRuleEngine.ApplyMatching(...)` pass;
- nearby user property `RuleFactor` remains generically editable.

## Validation performed

- Readback of current `main` after concurrent commits confirmed `SemanticPropertyEditPolicy.cs` still contains the narrow `Rule:` guard with blob SHA `12a297a379f57eacdb96c534e2c3727d49160ccc`.
- Readback confirmed `tests/QS3D.Core.SmokeTests/QuantityRuleProvenanceEditGuardSmoke.cs` is present with blob SHA `16c289e98b3ad7ed22a67b84874881a30b005e17` and targets current public service APIs.
- No GitHub Actions were dispatched. No executable .NET smoke/full build PASS and no licensed BricsCAD V25/V26 runtime qualification are claimed from this connector-only session.

## Coordination

Concurrent Selection Inspector claims explicitly excluded bulk-edit mutation scope. Existing Quantity Rule provenance claims hardened canonical reads/cleanup but did not reserve the generic write namespace. No overlapping `SemanticPropertyEditPolicy.cs` claim or concurrent source edit was observed before completion.

## Completion

`COMPLETED`: generic semantic editing can no longer forge `Rule:` quantity-rule provenance, preventing later lifecycle cleanup from treating arbitrary user metadata as managed-output ownership while preserving nearby user-defined property names.
