# Work claim — QSDB raw relation canonicality smoke reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-14T16:14:00+07:00`
- Baseline main SHA: `552aa7433ab6fe438076337bc9ba7c86cb9c1cbe`
- Priority: next deterministic full Core smoke blocker reported independently by PRs #1262 and #1266

## Confirmed fixture drift

`QsdbRelationIdentityCanonicalSmoke` is intended to prove that QSDB Save rejects raw noncanonical optional relation ids without silently normalizing the in-memory project. Current public `ProjectState.ActiveFloorId` / `ActiveZoneId` and `ProjectElement.FamilyId` / `FloorId` / `ZoneId` setters canonicalize those values on assignment, so the current padded setup no longer reaches the persistence boundary in a noncanonical state.

Production `QsdbProjectStore.ValidateProject(...)` still fail-closes through `ValidateOptionalCanonicalValue(...)` for the two project active-context ids and the three element relation ids. The source gate also locks those validators. Therefore this lane must restore the legacy/corrupt raw-state regression rather than weaken production validation or change public canonical setters.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/QsdbRelationIdentityCanonicalSmoke.cs`
- this claim document only

Keep the existing padded public-setter literals so the smoke proves current canonicalization first, then inject only the relevant private backing field through test-local reflection to represent raw legacy/corrupt persisted state before Save. Assert the injected raw value is visible, Save throws `InvalidDataException`, the raw relation remains byte-for-byte unchanged in memory, and timestamps remain unchanged. Preserve the empty optional-relation round-trip case.

## Explicit exclusions

- no changes to `QsdbProjectStore`, `ProjectState`, `ProjectElement`, QSDB schema/migration, `preflight-qsdb-relation-identity.py`, other persistence gates, native BricsCAD, LOCAL runners/probes, workflows, release, private data, or GitHub Actions;
- do not replace the rejection contract with successful canonical Save;
- do not weaken unknown/orphan/duplicate relation validation.

## Validation

- exact one-smoke diff/readback;
- preserve `preflight-qsdb-relation-identity.py` source literals and validator locks;
- use available owner full-smoke validation after merge to identify the next independent blocker; do not claim a local full-suite PASS from this web environment.

## Completion record

Pending implementation after claim merge.
