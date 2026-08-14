# Work claim — Quantity Rule raw FamilyId canonicality fixture

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-14T16:21:00+07:00`
- Completed: `2026-08-14T16:24:00+07:00`
- Baseline main SHA: `b0d5471895e6c100cd017b8382fcb424a7eed822`
- Priority: next deterministic full Core smoke blocker reported by QSDB #1269/#1272 and Polygon #1275/#1276 validation

## Confirmed fixture drift

`QuantityRuleFamilyIdCanonicalitySmoke.PaddedFamilyIdFailsBeforeStaleCleanup` intends to prove `QuantityRuleEngine.ResolveFamily(...)` rejects a nonblank noncanonical raw FamilyId before stale managed outputs are removed. The smoke created that state through `element.FamilyId = " FAM-1 ";`, but the authoritative public relation setter now canonicalizes it to `FAM-1` before the engine runs.

Production remains correct: `ResolveFamily(...)` compares raw FamilyId against its trimmed value and throws before `GetStaleManagedOutputs(...)` / `CleanupStaleOutputs(...)`.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/QuantityRuleFamilyIdCanonicalitySmoke.cs`
- this claim document only

The implementation kept the padded public-setter assignment, asserted its canonical result, then injected private `_familyId` test-locally to represent legacy/corrupt raw state. It asserts the injection reaches the public getter and preserves the expected noncanonical exception plus all stale quantity/provenance no-mutation assertions. Whitespace-empty and case-varied canonical controls remain unchanged.

## Exclusions

No production QuantityRuleEngine/ProjectElement changes; no persistence/native/LOCAL/workflow/release/Actions changes; no cleanup-order weakening.

## Completion

- Claim-only PR `#1278` merged before implementation as `d0e82ec5757f8ec7e8dee78626dbec031b80f7bb`.
- Implementation commit `cc6ac8ba4f08c0596785c3d03b889ffa048ff8ff` changed exactly one smoke file with `+12/-0`.
- Implementation PR `#1280` merged to `main` at `57448276afeac45127fad0843d498e2ef0b459fc`.
- Intervening commits before merge touched only project files and did not overlap the reserved smoke.
- The merged regression now explicitly proves public setter canonicalization, reconstructs raw padded `_familyId`, and preserves the fail-before-cleanup/no-mutation assertions.
- This web environment did not execute the .NET full Core smoke, so no fresh suite PASS or next-blocker claim is recorded here. A runner on this SHA or descendant must provide that evidence.
