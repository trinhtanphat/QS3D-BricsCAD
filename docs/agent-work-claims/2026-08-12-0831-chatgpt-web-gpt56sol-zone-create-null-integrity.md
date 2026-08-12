# Work claim — Zone Create null-collection integrity

- Status: `SUPERSEDED`
- Agent: `chatgpt-web-gpt56sol-zone-create-null-integrity-20260812-0831`
- Registered: `2026-08-12T08:31:00+07:00`
- Superseded: `2026-08-12T08:32:00+07:00`
- Baseline main SHA: `4a18251c9ae114e3897585bd5533f81719cd5eb9`
- Claim commit: `827997bd0ea582c4e85d0dcf675669e6e59e9d02`
- Priority: evidence-driven Domain mutation integrity during owner-requested `continue all`

## Confirmed defect

`ProjectZoneService.Create(...)` checked `project.Zones.Any(x => x.Id ...)` and then `EnsureUniqueName(...)`, both dereferencing entries before validating the persisted Zone collection. If a malformed project contained a null Zone entry, Create could throw an incidental `NullReferenceException` rather than failing closed with the domain integrity contract before mutation.

## Superseded by earlier concurrent owner

A more specific claim had already landed immediately before this registration:

- `1bb98be2ded9b886a1702693fdffc7e2c149f626` — `chore(agent): claim zone create null preflight`
- owner: `chatgpt-web-gpt56sol-project-zone-create-null-preflight-20260812-0829`

That owner then published the exact source fix at:

- `07585028b4cb46409d830fb98147ed0505379a3b` — `fix(core): fail closed on null zone during create`

My attempted source write was rejected with HTTP 409 because `ProjectZoneService.cs` had advanced to that concurrent fix. The current source was re-fetched and already contains the intended explicit null-zone preflight. I therefore did not duplicate source or create a competing smoke fixture.

## Coordination outcome

This claim is closed as superseded rather than competing with the earlier exact owner. No source/test file was changed by this lane. The earlier owner's claim/regression remains authoritative.

## Validation boundary

Remote GitHub read-back only. No GitHub Actions were dispatched; no executable Core build/smoke PASS and no BricsCAD V25/V26 runtime qualification are claimed.
