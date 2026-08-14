# Work claim — Floor/Zone assignment smoke canonical relation expectations

- Status: `COMPLETED`
- Agent: `codex-project-floor-zone-assignment-fixture-20260814` (`/root/fix_level_curtain_frame_z`)
- Registered: `2026-08-14T15:49:00+07:00`
- Completed: `2026-08-14T15:52:00+07:00`
- Baseline main SHA: `f6c35385939dae0969206b67707e49adca73623b`
- Priority: next deterministic Core full-smoke blocker after completed ProjectElement relation normalization

## Confirmed fixture drift

`ProjectFloorZoneMutationIntegritySmoke.FloorAssignmentCanonicalIdentityIsNoOp` constructs a `ProjectElement` with padded `FloorId` text and later expects the padding to remain. The authoritative `ProjectElement.FloorId` setter now trims optional relation identity at construction, so the stored value is already the lowercase alias `f-01` before `ProjectFloorService.Assign()` runs. The analogous Zone case has the same stale expectation and will fail next for the same reason.

The assignment contract itself remains current: both services compare relation identity canonically, return `0`, and preserve project `ChangeVersion`, element dirty flags, `UpdatedUtc`, alias case, and owned-target identity. The focused mutation-integrity gate passes on the baseline and does not encode either obsolete padded expectation.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/ProjectFloorZoneMutationIntegritySmoke.cs`
- this claim only

Change only the two post-assignment relation expectations from padded aliases to the already-stored trimmed lowercase aliases `f-01` and `z-01`. Preserve all changed-count, version, dirty-state, timestamp, ownership, active-alias repair, canonical activation and null-target atomicity assertions.

## Explicit exclusions

- no production Floor/Zone, ProjectElement, persistence, schema, audit, UI or native behavior changes;
- no focused-gate edit because `scripts/preflight-project-floor-zone-mutation-integrity.py` is current and passes;
- no edit to the independently stale `scripts/preflight-qsdb-relation-identity.py`;
- no LOCAL probe/runner, BricsCAD/private data, GitHub Actions, release or packaging work;
- report the next independent full-smoke blocker rather than expanding this claim.

## Validation

- Core Release build and full deterministic Core smoke;
- `scripts/preflight-project-floor-zone-mutation-integrity.py` and relevant Floor/Zone focused gates;
- exact diff/readback confirming only the two expectations changed and every no-op/repair/atomicity assertion remains.

## Completion record

- Claim PR `#1238` merged as `c8302de334d08957588ea27c5938cd304d98c5f7`.
- Test commit `5b6dd3e1c505fc4de7a7108fe3fd77e5f404b52f` merged through PR `#1240` as `3ccb9c4a2aa93405da8828b9c6fe919fd01aa011`.
- The one reserved smoke now expects the already-stored trimmed lowercase Floor/Zone aliases `f-01` and `z-01`. Both assignment calls still assert `changed == 0`, unchanged project revision, clean element state, unchanged timestamp and canonical owned-target lookup; all active-alias repair and null-target atomicity cases remain unchanged.
- Core Release build PASS with `0 warnings / 0 errors`. The mutation-integrity, canonical-reference, name-invariant, editor-atomicity, assignment-audit and active-audit Floor/Zone gates all PASS unchanged.
- Full Core smoke advances beyond this fixture and stops at the next independent blocker: `ProjectMaterialCatalogSmoke.RenameStalesInheritedConsumerWithPaddedFamilyId` line 143 reports that the material-rename fixture expected padded stored `FamilyId` text although the authoritative relation setter already trimmed it.
- No production, focused gate, `preflight-qsdb-relation-identity.py`, LOCAL runner/probe, BricsCAD/native/private data, GitHub Actions, release or packaging surface changed.
