# Work claim — BCF exchange XML representability current-main recovery

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-bcf-exchange-xml-main-recovery-20260815`
- Registered: `2026-08-15T10:04+07:00`
- Exact main baseline: `49135b378a479fa6c4da78d2d8713ad65b5bba61`
- Latest reconciled main: `5f8350564a554d86f0886e5e3b687f1e21124476`
- Issue: `#1506`
- Replacement current-main PR: `#1559` (`ready for review`, latest readback `mergeable=true`)
- Superseded integration-v2 PR: `#1510` (`closed`, not merged)
- Branch: `agent/chatgpt-gpt56sol/bcf-exchange-xml-main-recovery-20260815`
- Priority: Core P1 interoperability / fail-fast export integrity

## Confirmed current-main defect

Current `BcfIssueExchange.cs` allowed XML-illegal UTF-16 through BCF free text and BCF token fields (`status`, `type`, `qs3dElementId`) until later XML serialization. This violated the model's construction-time validity boundary.

## Recovered implementation

- BCF-local XML representability validation uses `XmlConvert.VerifyXmlChars(...)` at construction time;
- BCF topic `status` / `type` and component QS3D element id use a BCF-local canonical-token + XML wrapper without changing the global IFC token contract;
- existing GUID/IFC GUID, whitespace/control, collection-bound, camera and deterministic-ordering semantics remain unchanged;
- focused registered smoke rejects XML-invalid title, description, creation author, comment author/text, status, type and QS3D component id;
- valid supplementary Unicode round-trips through exact BCF serialize/deserialize.

## Evidence

- claim-only: `ac96760398781240309ad360ce489e5a9c2b1a5c`
- implementation: `859a5e4d16ca12cdc650734fea737e365f3ee5f2`
- first reconciliation onto `ca8b2b38557c169c12526d89c0513c601f70a1db`: `e31b0d158e4a4e9b011fb710367bba46c0d7b918`
- second reconciliation onto `c126bda58d1e226f2199e35f628b20ec9aef946c`: `cbcec384ddc897ff1c337e33e7950c502952d0e3`
- latest reconciliation onto `5f8350564a554d86f0886e5e3b687f1e21124476`: `7770edcdf9cca371f2c8b1caa6eac0848466cdad`
- replacement PR: `#1559`
- final task diff: exactly four files; production source delta `+23/-3`
- exact GitHub source/diff readback: PASS
- prior reviewed v2 source/smoke/registration: `1fd36d180bef77a3a71bbfbd05884e33a107014c` / `44ce0a7d18feb18fea3c85c901069b881cf3f10f` / `88ba6028d65a8f2593c9bd41cdcd17efe7b6f650`
- managed build/smoke in this recovery session: NOT_RUN; no `dotnet` execution available and no PASS claimed
- BricsCAD runtime: not applicable to this Core-only lane
- GitHub Actions: not manually dispatched/rerun

## Coordination / exclusions

- #1510 was closed only after #1559 was verified clean/mergeable; old branch/history remains intact.
- #1512/#1513 remains a distinct `BcfIssueExchangeSerializer.cs` timestamp-canonicality lane and was not touched.
- #1444 remains the separate `BcfZipPackage.cs` structural/package integrity lane.
- No global IFC token, ZIP/package reader, adapter/native, workflow/release, schema or product-boundary changes.
- No direct main merge by this normal-agent session.

## Handoff / release

All recovery source/regression state is represented by ready PR #1559 against `main`. Reservation ownership is released from this session. Keep Issue #1506 open until an authorized coordinator integrates #1559 and remote ancestry/source readback confirms the fix on `main`.

## Integration closeout

- Authorized integration PR `#1559` merged at exact main SHA
  `be4729e8a4f15e7e1dbf3ca5267015a00a58ca85`; issue `#1506` closed through
  that merge.
- Exact-merge validation: smoke registration, Interchange validation, and
  Interchange JSON gates PASS; `QS3D.Core.SmokeTests` Release build completed
  with 0 warnings / 0 errors; full deterministic Core smoke reported
  `ALL PASS`.
- No BricsCAD/native runtime or GitHub Actions operation was performed during
  integration.
