# Work claim — ProjectState persisted-text XML current-main recovery

- Status: `RELEASED` — implementation complete; pending authorized review/integration on current main
- Agent: `chatgpt-gpt56sol-projectstate-xml-main-recovery-20260815`
- Registered: `2026-08-15T10:04+07:00`
- Exact main baseline: `d521a3f95ee0ed80f12335e2f6affa59ce21fa9d`
- Latest reconciled main: `d73420a2dce589fd74e220efdcca3071b828b335`
- Issue: `#1468`
- Replacement current-main PR: `#1543` (`ready for review`, latest readback `mergeable=true`)
- Superseded integration-v2 PR: `#1508` (`closed`, not merged)
- Branch: `agent/chatgpt-gpt56sol/projectstate-xml-main-recovery-20260815`
- Priority: Core P1 persistence integrity

## Confirmed current-main defect

Current `ProjectState.cs` accepted XML-illegal UTF-16 at persisted Zone, Floor, Family, project identity/name, drawing path/fingerprint and active Floor/Zone boundaries. Lone surrogate input could therefore enter live state that canonical QSDB XML cannot represent; mutable ProjectState fields could advance `ChangeVersion` / `UpdatedUtc` before a later persistence failure.

## Recovered implementation

- one `PersistedTextXml.Verify(...)` helper using `XmlConvert.VerifyXmlChars(...)` and `ArgumentException` mapping;
- ZoneDefinition id/name, FloorDefinition id/name, ProjectFamily id/name, ProjectState ProjectId/Name, DrawingPath, DrawingFingerprint, ActiveZoneId and ActiveFloorId route through the helper before accepted mutation;
- all existing null/blank/trim/control semantics are preserved;
- focused smoke rejects both lone high and low surrogates at every listed public boundary;
- rejected mutable ProjectState name/scalar writes preserve old value, `ChangeVersion`, and `UpdatedUtc`;
- valid supplementary Unicode round-trips through exact QSDB SaveNew/Load for project, Zone, Floor, Family, path/fingerprint and resolved active Floor/Zone ids.

## Evidence

- claim-only: `82918bd593c630340050046bff47a3a11d72eb46`
- implementation: `d40ba342faab9447c47d002d024077da275c4d83`
- non-force reconciliation onto `d73420a2dce589fd74e220efdcca3071b828b335`: `10fcde20164f6fb5067c197634c926facd8b08ee`
- replacement PR: `#1543`
- final compare before PR: ahead 3 / behind 0; exactly four task files
- production source delta: +28/-9; smoke registration +1
- exact GitHub diff/readback: PASS
- prior v2 source/smoke/registration: `9fe8de0bb0397ce9b73e15eecb6401e35deb307f` / `43510db8633912422aa086fc7117be1475a35180` / `8e609d94356b5d2ec642cb01d4ede7eddd91c22c`
- managed build/smoke in this recovery session: NOT_RUN; no `dotnet` execution available and no PASS claimed
- BricsCAD runtime: not applicable to this Core-only lane
- GitHub Actions: not manually dispatched/rerun

## Coordination / exclusions

- #1508 was closed only after #1543 was verified clean/mergeable; old branch/history remains intact.
- No ProjectElement, domain service, adapter/native, workflow/release, schema or product-boundary changes.
- No direct main merge by this normal-agent session.

## Handoff / release

All recovery source/regression state is represented by ready PR #1543 against `main`. Reservation ownership is released from this session. Keep Issue #1468 open until an authorized coordinator integrates #1543 and remote ancestry/source readback confirms the complete persisted-text contract on `main`.
