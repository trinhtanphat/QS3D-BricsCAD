# Work claim — ProjectZoneService.Create null-zone preflight

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-project-zone-create-null-preflight-20260812-0829`
- Registered: `2026-08-12T08:29:00+07:00`
- Baseline main SHA: `84a48c237072098763cfac564e2399f9e214c08c`
- Priority: P2 — align Zone creation with existing fail-closed project collection integrity.

## Confirmed defect

`ProjectZoneService.Create(...)` performed duplicate-id and duplicate-name checks by dereferencing every existing `project.Zones` entry. If malformed in-memory/persisted state contained a null zone entry, creation leaked a raw `NullReferenceException` before mutation instead of failing closed with a stable project-integrity error.

## Implemented fix

- `Create(...)` now rejects any null existing Zone entry before maximum-count, duplicate-id and duplicate-name traversal.
- The fail-closed contract is `InvalidOperationException("Project zone collection contains a null zone.")`.
- Valid Create behavior, maximum-zone bound, duplicate id/name checks, active-zone initialization, timestamps and revision behavior remain unchanged.
- No Zone Update/Delete/Assign/SetActive, identity policy, QSDB/interchange, UI or BricsCAD runtime behavior was changed.

## Regression coverage

`ProjectZoneCreateNullPreflightSmoke` creates malformed Zone state with one valid Zone plus a null entry, records the zone count, active Zone id, `ChangeVersion` and `UpdatedUtc`, requires the canonical integrity exception from Create, and verifies all recorded state is unchanged.

## Integration evidence

- Claim registration: `1bb98be2ded9b886a1702693fdffc7e2c149f626`.
- Source fix: `07585028b4cb46409d830fb98147ed0505379a3b`.
- Focused regression: `6974a22e58967377943bfc8bde165c564c604c34`.
- Post-integration remote readback at moving `main` `c9f97eec01d55ad78129e4b675c7487769e0b62b` confirms both source and focused smoke remain present after concurrent commits.

## Validation boundary

Remote source/test readback plus committed deterministic smoke coverage only. GitHub Actions were not dispatched. No local .NET build PASS and no BricsCAD runtime PASS are claimed from this remote lane.
