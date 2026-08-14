# Work claim — Quantity Rule raw FamilyId canonicality fixture

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-14T16:21:00+07:00`
- Baseline main SHA: `b0d5471895e6c100cd017b8382fcb424a7eed822`
- Priority: next deterministic full Core smoke blocker reported by QSDB #1269/#1272 and Polygon #1275/#1276 validation

## Confirmed fixture drift

`QuantityRuleFamilyIdCanonicalitySmoke.PaddedFamilyIdFailsBeforeStaleCleanup` intends to prove `QuantityRuleEngine.ResolveFamily(...)` rejects a nonblank noncanonical raw FamilyId before stale managed outputs are removed. The smoke currently creates that state through `element.FamilyId = " FAM-1 ";`, but the authoritative public relation setter now canonicalizes it to `FAM-1` before the engine runs.

Production remains correct: `ResolveFamily(...)` compares raw FamilyId against its trimmed value and throws before `GetStaleManagedOutputs(...)` / `CleanupStaleOutputs(...)`.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/QuantityRuleFamilyIdCanonicalitySmoke.cs`
- this claim document only

Keep the padded public-setter assignment, assert its canonical result, then inject private `_familyId` test-locally to represent legacy/corrupt raw state. Assert the injection reaches the public getter, preserve the expected noncanonical exception, and preserve all pre-existing stale quantity/provenance no-mutation assertions. Leave whitespace-empty and case-varied canonical controls unchanged.

## Exclusions

No production QuantityRuleEngine/ProjectElement changes; no persistence/native/LOCAL/workflow/release/Actions changes; no cleanup-order weakening.

## Completion

Pending claim merge, narrow implementation, validation evidence, and closeout.
