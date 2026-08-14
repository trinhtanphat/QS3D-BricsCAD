# Work claim — Quantity Rule raw FamilyId fixture boundary

- Status: `COMPLETED`
- Agent: `codex-quantity-rule-familyid-reflection-fixture-20260814` (`/root/fix_level_curtain_frame_z`)
- Registered: `2026-08-14T16:22:00+07:00`
- Completed: `2026-08-14T16:26:00+07:00`
- Baseline main SHA: `f77ab1d3e6b89891efe5f18defdc0160414c57ce`
- Priority: next deterministic full Core smoke blocker after ProjectElement relation writers became canonical

## Confirmed two-layer contract

The supported `ProjectElement.FamilyId` setter now trims optional relation identity, maps whitespace-only input to empty, and rejects controls. `QuantityRuleFamilyIdCanonicalitySmoke.PaddedFamilyIdFailsBeforeStaleCleanup()` still uses that setter to create padded nonblank state, so Quantity Rule receives canonical `FAM-1` and no longer reaches its defensive raw-state rejection.

`QuantityRuleEngine.ResolveFamily()` intentionally remains a fail-closed consumer boundary: a corrupt/legacy nonblank raw FamilyId whose spelling differs from its trimmed lookup token must throw before rule enumeration, stale managed quantity/provenance cleanup, or new output creation. Canonical success expectations would remove this ordering regression.

No open PR or ACTIVE/BLOCKED claim owns the exact smoke. The original Quantity Rule FamilyId canonicality source claim is `COMPLETED`.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/QuantityRuleFamilyIdCanonicalitySmoke.cs`
- this claim only

Construct the padded-case element through the valid canonical public boundary and assert it, then use test-local reflection only on private `_familyId` to inject padded nonblank raw state and assert the public getter sees it. Retain the noncanonical exception contract, old managed quantity/provenance preservation, and absence of new output quantity/provenance. Keep whitespace public-setter-to-empty behavior and case-varied canonical Family property projection unchanged.

## Explicit exclusions

- no changes to `QuantityRuleEngine`, `ProjectElement`, persistence, relation normalization or any other production source;
- no gate change because no focused gate references this smoke or `ResolveFamily` raw-state guard;
- no other Quantity Rule smoke, preview, reporting or persistence surface;
- no LOCAL probe/runner, BricsCAD/native/private data, GitHub Actions, release or packaging work;
- report the next independent full-smoke blocker rather than expanding scope.

## Validation

- Core Release build and full deterministic Core smoke;
- relevant Quantity Rule focused gates unchanged;
- exact diff/readback proving padded reflection injection plus fail-before-cleanup assertions, public whitespace canonicalization and case-varied projection remain.

## Completion record

- Claim PR `#1277` merged as `838385ddf285b46059305edd4b9e7581528d2463`.
- This lane prepared equivalent test commit `24d0577bb0d3d6d0e9010265a1c25efea6781d22` and PR `#1279`, but closed that PR unmerged after the later duplicate lane's reviewed-equivalent implementation landed first. No force update or duplicate code reapplication occurred.
- Reused implementation commit `cc6ac8ba4f08c0596785c3d03b889ffa048ff8ff` merged through PR `#1280` as `57448276afeac45127fad0843d498e2ef0b459fc`.
- The merged smoke proves the public padded setter stores canonical `FAM-1`, injects only private `_familyId` back to padded raw state, asserts the getter sees it, and retains the noncanonical exception plus old managed quantity/provenance preservation and absence of new outputs. Whitespace-to-empty and case-varied canonical controls remain unchanged.
- Current-main Core Release build PASS with `0 warnings / 0 errors`. All seven Quantity Rule create, UI, freshness, preview, apply-tracking and provenance focused gates PASS under Python UTF-8 mode.
- Full Core smoke advances beyond this fixture and stops at the next independent blocker: `ReportingReferenceIdCanonicalitySmoke.PaddedFloorReferenceFailsClosed` does not receive the expected `InvalidOperationException` because its public FloorId writer now canonicalizes padded input.
- No production, gate, LOCAL runner/probe, BricsCAD/native/private data, GitHub Actions, release or packaging surface was changed or run by this lane.
